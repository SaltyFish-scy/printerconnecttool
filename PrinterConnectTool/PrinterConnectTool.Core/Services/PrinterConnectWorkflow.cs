using System.Runtime.Versioning;
using PrinterConnectTool.Models;

namespace PrinterConnectTool.Services;

/// <summary>
///     打印机连接流程编排：探测职场、提取驱动、执行安装
///     UI / Console 都调用这里，各自只负责交互方式不同
/// </summary>
[SupportedOSPlatform("windows")]
public static class PrinterConnectWorkflow
{
    /// <summary>
    ///     加载配置并探测职场
    /// </summary>
    public static async Task<(AppConfig Config, WorkplaceConfig? Workplace)> DetectWorkplaceAsync(ILogger logger)
    {
        logger.Info("正在加载配置...");
        var config = ConfigLoader.Load(logger);

        logger.Info("正在探测职场网络...");
        var detector = new WorkplaceDetector(config);
        var workplace = await detector.DetectAsync();

        return (config, workplace);
    }

    /// <summary>
    ///     安装指定打印机
    /// </summary>
    /// <param name="printer">打印机配置</param>
    /// <param name="logger">日志输出</param>
    /// <param name="captureOutput">true=隐藏 PowerShell 并捕获输出；false=弹出独立 PowerShell 窗口</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<bool> InstallAsync(
        PrinterConfig printer,
        ILogger logger,
        bool captureOutput = false,
        CancellationToken cancellationToken = default)
    {
        logger.Info($"已选择打印机: {printer.Name}");
        logger.Info("正在准备驱动文件...");

        DriverExtractor.Extract(printer.Brand, logger);

        logger.Info("开始安装打印机...");
        var installer = new PrinterInstaller();

        if (captureOutput)
            return await installer.InstallAsync(printer, logger, cancellationToken);

        return installer.Install(printer, logger);
    }
}
