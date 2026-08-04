namespace PrinterConnectTool.Generator.Models;

public class DriverPackage : BindableBase
{
    private string _brand = "";
    private string _displayName = "";
    private string _zipFilePath = "";
    private string _defaultDriverName = "";

    public string Brand { get => _brand; set => SetProperty(ref _brand, value); }
    public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
    public string ZipFilePath { get => _zipFilePath; set => SetProperty(ref _zipFilePath, value); }
    public string DefaultDriverName { get => _defaultDriverName; set => SetProperty(ref _defaultDriverName, value); }
}
