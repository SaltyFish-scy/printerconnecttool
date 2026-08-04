using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace PrinterConnectTool.Services;

/// <summary>
///     管理员权限检查 & 自动提权
/// </summary>
[SupportedOSPlatform("windows")]
public static class AdminChecker
{
    /// <summary>
    ///     检查当前是否以管理员身份运行
    /// </summary>
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    ///     自动提权：用 runas 动词重新启动自身，当前进程退出
    /// </summary>
    public static void ElevateAndRestart()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("无法获取当前程序路径，自动提权失败。");
            Console.WriteLine("请右键点击此程序，选择\"以管理员身份运行\"。");
            Console.ResetColor();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            Environment.Exit(1);
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception)
        {
            // 用户在 UAC 弹窗点了"否"
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("管理员权限被拒绝，程序无法继续运行。");
            Console.WriteLine("请右键点击此程序，选择\"以管理员身份运行\"。");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    /// <summary>
    ///     入口检查：非管理员则自动提权重启
    /// </summary>
    public static void EnsureAdmin()
    {
        if (!IsAdministrator()) ElevateAndRestart();
    }
}