using System.Runtime.Versioning;
using PrinterConnectTool.ConsoleApp;
using PrinterConnectTool.Models;
using PrinterConnectTool.Services;

// .NET 10 顶层语句 — 程序入口
[SupportedOSPlatform("windows")]
public class Program
{
    public static async Task Main(string[] args)
    {
        var logger = new ConsoleLogger();

        // ==================== 0. 管理员权限检查 & 自动提权 ====================
        AdminChecker.EnsureAdmin();

        // ==================== 1. 欢迎界面 ====================
        WelcomeScreen.Show();

        // ==================== 2. 加载配置 & 探测职场 ====================
        AppConfig config;
        WorkplaceConfig? workplace;
        try
        {
            (config, workplace) = await PrinterConnectWorkflow.DetectWorkplaceAsync(logger);
        }
        catch (Exception ex)
        {
            WelcomeScreen.ShowNetworkError($"配置加载失败: {ex.Message}");
            return;
        }

        if (workplace == null)
        {
            var detector = new WorkplaceDetector(config);
            var internetOk = await detector.CheckInternetAsync();
            if (internetOk)
                WelcomeScreen.ShowNetworkError(
                    "无法探测到所在区域，请关闭代理软件，或者检查是否连接所在公司的网络内。");
            else
                WelcomeScreen.ShowNetworkError("无法确认网络是否正常连接，请检查网络。");
            return;
        }

        // ==================== 3. 显示职场 & 打印机列表 ====================
        WelcomeScreen.ShowWorkplaceDetected(workplace.Name);

        if (workplace.Printers.Count == 0)
        {
            WelcomeScreen.ShowNetworkError($"职场 [{workplace.Name}] 暂无可用打印机配置。");
            return;
        }

        // 列出打印机
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  可用打印机列表：");
        Console.ResetColor();
        Console.WriteLine();

        for (var i = 0; i < workplace.Printers.Count; i++)
        {
            var p = workplace.Printers[i];
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  [{i + 1}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(p.Name);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"      IP: {p.Ip}  驱动: {p.DriverName}");
            Console.ResetColor();
        }

        Console.WriteLine();

        // ==================== 4. 用户选择打印机 ====================
        int choice;
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  请输入要连接的打印机编号: ");
            Console.ResetColor();

            var input = Console.ReadLine();
            if (int.TryParse(input, out choice) && choice >= 1 && choice <= workplace.Printers.Count) break;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  输入无效，请输入 1 到 {workplace.Printers.Count} 之间的数字。");
            Console.ResetColor();
            Console.WriteLine();
        }

        var selectedPrinter = workplace.Printers[choice - 1];

        // ==================== 5. 提取驱动 & 执行安装 ====================
        try
        {
            await PrinterConnectWorkflow.InstallAsync(selectedPrinter, logger, captureOutput: false);
        }
        catch (Exception ex)
        {
            logger.Error($"安装过程异常: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("  按任意键退出...");
            Console.ReadKey();
        }
    }
}
