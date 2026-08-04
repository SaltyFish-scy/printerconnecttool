namespace PrinterConnectTool.Generator.Models;

public class OfficeDefinition : BindableBase
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "";
    private string _gatewayIp = "";

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string GatewayIp { get => _gatewayIp; set => SetProperty(ref _gatewayIp, value); }
}
