namespace PrinterConnectTool.Generator.Models;

public class PrinterDefinition : BindableBase
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "";
    private string _ip = "";
    private int _portNumber = 9100;
    private string _officeId = "";
    private string _driverBrand = "";
    private string _driverName = "";

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Ip { get => _ip; set => SetProperty(ref _ip, value); }
    public int PortNumber { get => _portNumber; set => SetProperty(ref _portNumber, value); }
    public string OfficeId { get => _officeId; set => SetProperty(ref _officeId, value); }
    public string DriverBrand { get => _driverBrand; set => SetProperty(ref _driverBrand, value); }
    public string DriverName { get => _driverName; set => SetProperty(ref _driverName, value); }
}
