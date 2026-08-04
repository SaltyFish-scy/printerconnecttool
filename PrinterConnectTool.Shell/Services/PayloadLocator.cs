namespace PrinterConnectTool.Shell.Services;

/// <summary>
///     保存当前 EXE 对应的 Payload 解压路径，供 DriverExtractor / PrinterInstaller 使用。
///     程序退出时调用 Cleanup 删除解压目录，避免长期占用磁盘。
/// </summary>
public static class PayloadLocator
{
    public static string? PayloadRoot { get; private set; }
    public static bool HasPayloadRoot => !string.IsNullOrEmpty(PayloadRoot);

    public static void SetPayloadRoot(string root)
    {
        PayloadRoot = root;
    }

    public static void Cleanup()
    {
        if (string.IsNullOrEmpty(PayloadRoot)) return;
        if (Directory.Exists(PayloadRoot))
        {
            Directory.Delete(PayloadRoot, true);
        }
        PayloadRoot = null;
    }
}
