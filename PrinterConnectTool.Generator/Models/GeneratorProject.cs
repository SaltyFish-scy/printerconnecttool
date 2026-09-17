using System.Collections.ObjectModel;

namespace PrinterConnectTool.Generator.Models;

public class GeneratorProject : BindableBase
{
    /// <summary>当前生成器写出的项目文件格式版本。</summary>
    public const string CurrentVersion = "1.4";

    public string Version { get; set; } = CurrentVersion;
    public string ShellTitle { get; set; } = "打印机自助连接工具";
    public int PingTimeoutMs { get; set; } = 2000;
    public int OverallTimeoutMs { get; set; } = 3000;
    public ObservableCollection<DriverPackage> Drivers { get; } = new();
    public ObservableCollection<OfficeDefinition> Offices { get; } = new();
    public ObservableCollection<PrinterDefinition> Printers { get; } = new();

    /// <summary>
    ///     解析打印机关联的驱动包：优先按 DriverId 活引用查找，
    ///     找不到时回退到 1.3 及更早版本的 DriverBrand 快照匹配。
    /// </summary>
    public DriverPackage? FindDriver(PrinterDefinition printer)
    {
        if (!string.IsNullOrEmpty(printer.DriverId))
        {
            var byId = Drivers.FirstOrDefault(d => d.Id == printer.DriverId);
            if (byId != null) return byId;
        }

        if (!string.IsNullOrEmpty(printer.DriverBrand))
            return Drivers.FirstOrDefault(d => d.Brand == printer.DriverBrand);

        return null;
    }

    /// <summary>打印机的实际驱动名：未单独指定时跟随驱动包的默认驱动名。</summary>
    public string EffectiveDriverName(PrinterDefinition printer)
    {
        if (!string.IsNullOrWhiteSpace(printer.DriverName))
            return printer.DriverName;
        return FindDriver(printer)?.DefaultDriverName ?? "";
    }
}
