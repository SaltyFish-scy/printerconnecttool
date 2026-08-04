namespace PrinterConnectTool.ConsoleApp;

/// <summary>
///     欢迎界面渲染
/// </summary>
public static class WelcomeScreen
{
    private const string Version = "V2.3";
    private const string Developer = "cysong4";

    /// <summary>
    ///     显示欢迎界面
    /// </summary>
    public static void Show()
    {
        Console.Clear();

        // 顶部边框
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║                                                          ║");
        Console.WriteLine("  ║         欢迎使用分子公司打印机自助连接工具                  ║");
        Console.WriteLine("  ║                                                          ║");
        Console.WriteLine("  ║                                                          ║");

        // 版本号
        Console.ForegroundColor = ConsoleColor.Yellow;
        var versionLine = $"        当前版本号：{Version}";
        Console.WriteLine("  ║" + PadCenter(versionLine, 58) + "║");

        // 开发人员
        var devLine = $"        开发/维护人员：{Developer}";
        Console.WriteLine("  ║" + PadCenter(devLine, 58) + "║");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ║                                                          ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        // 状态行
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  正在检测网络环境...");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    ///     显示探测结果
    /// </summary>
    public static void ShowWorkplaceDetected(string workplaceName)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  已探测到所在职场：{workplaceName}");
        Console.WriteLine("  正在为您检测内部所有打印机...");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    ///     显示网络错误提示
    /// </summary>
    public static void ShowNetworkError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine($"  {message}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("  按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    ///     中文混合字符串居中填充（按显示宽度计算）
    /// </summary>
    private static string PadCenter(string text, int totalWidth)
    {
        var displayWidth = GetDisplayWidth(text);
        if (displayWidth >= totalWidth) return text;

        var padding = totalWidth - displayWidth;
        var leftPad = padding / 2;
        var rightPad = padding - leftPad;

        return new string(' ', leftPad) + text + new string(' ', rightPad);
    }

    /// <summary>
    ///     计算字符串显示宽度（中文=2，英文=1）
    /// </summary>
    private static int GetDisplayWidth(string text)
    {
        var width = 0;
        foreach (var c in text) width += c > 127 ? 2 : 1;
        return width;
    }
}