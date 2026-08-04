using System.Net;
using System.Net.Sockets;
using PrinterConnectTool.Generator.Models;

namespace PrinterConnectTool.Generator.Services;

public static class ValidationService
{
    public static ValidationResult Validate(GeneratorProject project)
    {
        var result = new ValidationResult();

        if (project.Drivers.Count == 0)
            result.AddError("请至少添加一个驱动包。");
        if (project.Offices.Count == 0)
            result.AddError("请至少添加一个职场。");
        if (project.Printers.Count == 0)
            result.AddError("请至少添加一台打印机。");

        var brands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var driver in project.Drivers)
        {
            if (string.IsNullOrWhiteSpace(driver.Brand))
                result.AddError("驱动包 Brand 不能为空。");
            if (!brands.Add(driver.Brand))
                result.AddError($"驱动包 Brand '{driver.Brand}' 重复。");
            if (!File.Exists(driver.ZipFilePath))
                result.AddError($"驱动包 '{driver.Brand}' 的 ZIP 文件不存在：{driver.ZipFilePath}");
            if (string.IsNullOrWhiteSpace(driver.DefaultDriverName))
                result.AddError($"驱动包 '{driver.Brand}' 的默认驱动名不能为空。");
            if (driver.Brand.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
                result.AddError($"驱动包 Brand '{driver.Brand}' 只能包含字母、数字、下划线或连字符。");
        }

        var officeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var office in project.Offices)
        {
            if (string.IsNullOrWhiteSpace(office.Name))
                result.AddError("职场名称不能为空。");
            if (!officeNames.Add(office.Name))
                result.AddError($"职场名称 '{office.Name}' 重复。");
            if (!IsValidIp(office.GatewayIp))
                result.AddError($"职场 '{office.Name}' 的网关 IP 格式不正确。");
        }

        var printerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var printer in project.Printers)
        {
            if (string.IsNullOrWhiteSpace(printer.Name))
                result.AddError("打印机名称不能为空。");
            if (!printerNames.Add(printer.Name))
                result.AddError($"打印机名称 '{printer.Name}' 重复。");
            if (!IsValidIp(printer.Ip))
                result.AddError($"打印机 '{printer.Name}' 的 IP 格式不正确。");
            if (project.Offices.All(o => o.Id != printer.OfficeId))
                result.AddError($"打印机 '{printer.Name}' 未关联有效职场。");
            if (project.Drivers.All(d => d.Brand != printer.DriverBrand))
                result.AddError($"打印机 '{printer.Name}' 未关联有效驱动包。");
            if (string.IsNullOrWhiteSpace(printer.DriverName))
                result.AddError($"打印机 '{printer.Name}' 的驱动名不能为空。");
            if (printer is { PortNumber: < 1 or > 65535 })
                result.AddError($"打印机 '{printer.Name}' 的端口号必须在 1-65535 之间。");
        }

        return result;
    }

    private static bool IsValidIp(string ip)
    {
        return IPAddress.TryParse(ip, out var addr) && addr.AddressFamily == AddressFamily.InterNetwork;
    }
}
