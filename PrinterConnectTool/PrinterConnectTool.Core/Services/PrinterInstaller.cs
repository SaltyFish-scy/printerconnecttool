using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using PrinterConnectTool.Models;

namespace PrinterConnectTool.Services;

/// <summary>
///     打印机安装：
///     1. 提取驱动到 C:\Drivers\{brand}\
///     2. 从 Payload 目录或嵌入资源原样复制 PS1 到 C:\Drivers\{brand}\install.ps1（保留 BOM）
///     3. 运行 PS1：支持"弹出独立窗口"或"隐藏并捕获输出"两种模式
/// </summary>
[SupportedOSPlatform("windows")]
public class PrinterInstaller
{
    /// <summary>
    ///     执行打印机安装（弹出独立 PowerShell 窗口，兼容原控制台体验）
    /// </summary>
    public bool Install(PrinterConfig printer, ILogger logger)
    {
        var scriptPath = ReleaseScript(printer, logger);
        return RunInWindow(scriptPath, logger);
    }

    /// <summary>
    ///     执行打印机安装（隐藏 PowerShell 窗口，把标准输出/错误实时捕获到日志）
    /// </summary>
    public async Task<bool> InstallAsync(PrinterConfig printer, ILogger logger, CancellationToken cancellationToken = default)
    {
        var scriptPath = ReleaseScript(printer, logger);
        return await RunHiddenAndCaptureAsync(scriptPath, logger, cancellationToken);
    }

    /// <summary>
    ///     从 Payload 目录或嵌入资源原样释放 PS1 脚本，保留字节级内容（包括 BOM）
    /// </summary>
    private static string ReleaseScript(PrinterConfig printer, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(printer.Script))
            throw new InvalidOperationException(
                $"打印机 [{printer.Name}] 未配置 script 字段，无法找到对应的安装脚本。");

        var scriptPath = Path.Combine(@"C:\Drivers", printer.Brand, "install.ps1");
        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);

        var payloadRoot = GetPayloadRoot();
        if (!string.IsNullOrEmpty(payloadRoot))
        {
            var payloadScriptPath = Path.Combine(payloadRoot, "Scripts", printer.Script);
            if (File.Exists(payloadScriptPath))
            {
                File.Copy(payloadScriptPath, scriptPath, overwrite: true);
                logger.Info($"脚本已从 Payload 复制: {scriptPath}");
                return scriptPath;
            }
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"PrinterConnectTool.Scripts.{printer.Script}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException(
                $"找不到嵌入的安装脚本: {printer.Script}\n" +
                $"资源名: {resourceName}\n" +
                $"可用资源: {string.Join(", ", assembly.GetManifestResourceNames())}");

        // 原样读取字节（保留源文件的 BOM），直接写入文件，不做任何重新编码
        var scriptBytes = new byte[stream.Length];
        stream.ReadExactly(scriptBytes);

        File.WriteAllBytes(scriptPath, scriptBytes);

        logger.Info($"脚本已释放: {scriptPath}");
        return scriptPath;
    }

    /// <summary>
    ///     弹出独立 PowerShell 窗口运行 PS1（UseShellExecute=true）
    ///     这是经过验证的稳定方案，printui / rundll32 在这种独立窗口下行为正常。
    /// </summary>
    private static bool RunInWindow(string scriptPath, ILogger logger)
    {
        logger.Info("正在弹出 PowerShell 窗口执行安装...");
        logger.Info("（请在弹出的 PowerShell 窗口中查看进度）");

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        process.WaitForExit();

        return process.ExitCode == 0;
    }

    /// <summary>
    ///     隐藏运行 PowerShell 并实时捕获输出到日志（B 方案）
    ///     注意：此方案改变了 PS1 的运行上下文，若出现"安装成功但打印异常"需回退到 RunInWindow。
    /// </summary>
    private static async Task<bool> RunHiddenAndCaptureAsync(string scriptPath, ILogger logger, CancellationToken cancellationToken)
    {
        logger.Info("正在后台运行 PowerShell 安装脚本，输出将显示在下方...");

        // 注册 Windows 代码页提供程序，确保 Encoding.GetEncoding(936) 在自包含发布中可用
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 中文 Windows 默认控制台输出编码通常为 GBK(936)，先按此读取；
        // 若出现乱码可改为 Encoding.UTF8 或根据系统调整。
        var outputEncoding = Encoding.GetEncoding(936);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = outputEncoding,
            StandardErrorEncoding = outputEncoding,
        };

        using var process = new Process { StartInfo = psi };

        process.Start();

        // 使用事件异步方式读取输出，避免 WaitForExitAsync 与 ReadLineAsync 互相等待导致死锁
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) logger.Info(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) logger.Error(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        // 等待异步事件读取完成
        process.CancelOutputRead();
        process.CancelErrorRead();

        if (process.ExitCode == 0)
            logger.Success("PowerShell 安装脚本执行完成。");
        else
            logger.Error($"PowerShell 安装脚本退出码: {process.ExitCode}");

        return process.ExitCode == 0;
    }

    private static string? GetPayloadRoot()
    {
        var shellAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "PrinterConnectTool.Desktop");
        if (shellAssembly == null) return null;

        var locatorType = shellAssembly.GetType("PrinterConnectTool.Shell.Services.PayloadLocator");
        if (locatorType == null) return null;

        var hasProperty = locatorType.GetProperty("HasPayloadRoot", BindingFlags.Public | BindingFlags.Static);
        if (hasProperty?.GetValue(null) is not true) return null;

        var rootProperty = locatorType.GetProperty("PayloadRoot", BindingFlags.Public | BindingFlags.Static);
        return rootProperty?.GetValue(null) as string;
    }
}
