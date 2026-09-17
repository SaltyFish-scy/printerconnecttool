namespace PrinterConnectTool.Generator.Models;

public class PrinterDefinition : BindableBase
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "";
    private string _ip = "";
    private int _portNumber = 9100;
    private string _officeId = "";
    private string _driverId = "";
    private string _driverBrand = "";
    private string _driverName = "";

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Ip { get => _ip; set => SetProperty(ref _ip, value); }
    public int PortNumber { get => _portNumber; set => SetProperty(ref _portNumber, value); }
    public string OfficeId { get => _officeId; set => SetProperty(ref _officeId, value); }

    /// <summary>关联驱动包的 Id（活引用，驱动改名不断链）。</summary>
    public string DriverId { get => _driverId; set => SetProperty(ref _driverId, value); }

    /// <summary>冗余快照，仅用于兼容 1.3 及更早版本的项目文件，逻辑上以 <see cref="DriverId"/> 为准。</summary>
    public string DriverBrand { get => _driverBrand; set => SetProperty(ref _driverBrand, value); }

    /// <summary>留空则跟随驱动包的默认驱动名。</summary>
    public string DriverName { get => _driverName; set => SetProperty(ref _driverName, value); }
}
