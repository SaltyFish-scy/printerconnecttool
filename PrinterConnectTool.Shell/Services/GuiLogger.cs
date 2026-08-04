using PrinterConnectTool.Services;

namespace PrinterConnectTool.Shell.Services;

/// <summary>
///     GUI 日志实现：把日志转发到 UI 回调
/// </summary>
public class GuiLogger : ILogger
{
    private readonly Action<string, LogLevel> _onLog;

    public GuiLogger(Action<string, LogLevel> onLog)
    {
        _onLog = onLog;
    }

    public void Info(string message) => _onLog(message, LogLevel.Info);
    public void Success(string message) => _onLog(message, LogLevel.Success);
    public void Warning(string message) => _onLog(message, LogLevel.Warning);
    public void Error(string message) => _onLog(message, LogLevel.Error);
}
