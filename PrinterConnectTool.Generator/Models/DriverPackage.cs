namespace PrinterConnectTool.Generator.Models;

public class DriverPackage : BindableBase
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _brand = "";
    private string _displayName = "";
    private string _zipFilePath = "";
    private string _defaultDriverName = "";
    private byte[] _zipData = Array.Empty<byte>();

    /// <summary>驱动包唯一标识。旧版项目文件无此字段，加载时自动生成。</summary>
    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Brand { get => _brand; set => SetProperty(ref _brand, value); }
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
    public string ZipFilePath { get => _zipFilePath; set => SetProperty(ref _zipFilePath, value); }
    public string DefaultDriverName { get => _defaultDriverName; set => SetProperty(ref _defaultDriverName, value); }
    public byte[] ZipData { get => _zipData; set => SetProperty(ref _zipData, value); }
}
