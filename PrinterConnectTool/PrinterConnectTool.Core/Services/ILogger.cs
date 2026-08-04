namespace PrinterConnectTool.Services;

/// <summary>
///     通用日志接口：Console / GUI 各自实现，Core 只负责输出日志内容
/// </summary>
public interface ILogger
{
    void Info(string message);
    void Success(string message);
    void Warning(string message);
    void Error(string message);
}
