using System.Runtime.Versioning;

namespace PrinterConnectTool.Services;

/// <summary>
///     安装后清理临时驱动文件
/// </summary>
[SupportedOSPlatform("windows")]
public static class Cleaner
{
    /// <summary>
    ///     清理指定品牌的临时驱动目录
    /// </summary>
    public static void CleanDriverTemp(string driverFolder, ILogger logger)
    {
        try
        {
            if (Directory.Exists(driverFolder))
            {
                Directory.Delete(driverFolder, true);
                logger.Info("临时驱动文件已清理");
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"临时文件清理失败（不影响使用）: {ex.Message}");
        }
    }

    /// <summary>
    ///     清理整个 PrinterDrivers 临时目录
    /// </summary>
    public static void CleanAllTemp()
    {
        try
        {
            var tempBase = Path.Combine(Path.GetTempPath(), "PrinterDrivers");
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
        }
        catch
        {
            // 静默忽略
        }
    }
}