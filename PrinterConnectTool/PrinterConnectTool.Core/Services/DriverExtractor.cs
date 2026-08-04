using System.IO.Compression;
using System.Reflection;
using System.Runtime.Versioning;

namespace PrinterConnectTool.Services;

/// <summary>
///     从嵌入资源或 Payload 目录提取 {brand}.zip 并解压到 C:\Drivers\
///     ZIP 内应包含原始品牌文件夹结构，如 TOSHIBA.zip/TOSHIBA/eSf6u.inf
///     解压后会得到 C:\Drivers\TOSHIBA\eSf6u.inf
/// </summary>
[SupportedOSPlatform("windows")]
public static class DriverExtractor
{
    /// <summary>
    ///     提取指定 brand 的驱动 ZIP 到 C:\Drivers\{brand}\
    /// </summary>
    /// <param name="brand">驱动文件夹标识（如 TOSHIBA、ADC225）</param>
    /// <param name="logger">日志输出</param>
    /// <returns>提取后的驱动文件夹完整路径</returns>
    public static string Extract(string brand, ILogger logger)
    {
        var driverBase = Path.Combine(@"C:\Drivers", brand);

        // 优先从 Shell 的 PayloadLocator 读取生成器解压的 Payload 目录
        var payloadRoot = GetPayloadRoot();
        if (!string.IsNullOrEmpty(payloadRoot))
        {
            var payloadZip = Path.Combine(payloadRoot, "Drivers", $"{brand}.zip");
            if (File.Exists(payloadZip))
            {
                logger.Info($"从 Payload 读取驱动包: {payloadZip}");
                return ExtractFromZipFile(payloadZip, driverBase, brand, logger);
            }
        }

        // 否则回退到嵌入资源（原流程）
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"PrinterConnectTool.Drivers.{brand}.zip";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException(
                $"未找到品牌 [{brand}] 的驱动 ZIP。\n" +
                $"资源名: {resourceName}\n" +
                $"可用资源: {string.Join(", ", assembly.GetManifestResourceNames())}");

        var tempZip = Path.Combine(Path.GetTempPath(), $"driver_{brand}_{Guid.NewGuid():N}.zip");
        try
        {
            using (var fs = File.Create(tempZip))
            {
                stream.CopyTo(fs);
                fs.Flush(true);
            }

            return ExtractFromZipFile(tempZip, driverBase, brand, logger);
        }
        finally
        {
            try { File.Delete(tempZip); } catch { }
        }
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

    private static string ExtractFromZipFile(string zipPath, string driverBase, string brand, ILogger logger)
    {
        // 清理旧目录
        if (Directory.Exists(driverBase)) Directory.Delete(driverBase, true);
        Directory.CreateDirectory(driverBase);

        // 直接解压到 C:\Drivers\，保留 ZIP 内的 {brand}\ 文件夹结构
        ZipFile.ExtractToDirectory(zipPath, @"C:\Drivers", overwriteFiles: true);

        // 统计解压结果
        if (!Directory.Exists(driverBase))
            throw new InvalidOperationException($"解压后未找到目录: {driverBase}");

        var extractedFiles = Directory.GetFiles(driverBase, "*", SearchOption.AllDirectories);
        var totalSize = extractedFiles.Sum(f => new FileInfo(f).Length);

        logger.Success($"驱动已解压到 {driverBase}");
        logger.Info($"共 {extractedFiles.Length} 个文件，总大小 {totalSize / 1024 / 1024:F1} MB");

        // 列出关键文件
        var infFile = extractedFiles.FirstOrDefault(f => f.EndsWith(".inf", StringComparison.OrdinalIgnoreCase));
        if (infFile != null)
        {
            logger.Info($"INF: {Path.GetFileName(infFile)} ({new FileInfo(infFile).Length} bytes)");
        }
        else
        {
            logger.Warning("未找到 INF 文件！");
        }

        return driverBase;
    }
}
