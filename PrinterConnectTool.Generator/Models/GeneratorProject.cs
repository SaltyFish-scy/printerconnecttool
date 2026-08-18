using System.Collections.ObjectModel;

namespace PrinterConnectTool.Generator.Models;

public class GeneratorProject : BindableBase
{
    public string Version { get; set; } = "1.0";
    public string ShellTitle { get; set; } = "打印机自助连接工具";
    public int PingTimeoutMs { get; set; } = 2000;
    public int OverallTimeoutMs { get; set; } = 3000;
    public ObservableCollection<DriverPackage> Drivers { get; } = new();
    public ObservableCollection<OfficeDefinition> Offices { get; } = new();
    public ObservableCollection<PrinterDefinition> Printers { get; } = new();
}
