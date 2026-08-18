using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using PrinterConnectTool.Generator.Models;

namespace PrinterConnectTool.Generator.Services;

public static class ValidationService
{
    private static readonly Regex AllowedNameRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public static bool IsValidBrand(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedNameRegex.IsMatch(value);
    }

    public static bool IsValidZipFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var name = Path.GetFileName(fileName);
        if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return false;

        var withoutExt = name[..^4];
        return !string.IsNullOrWhiteSpace(withoutExt) && AllowedNameRegex.IsMatch(withoutExt);
    }

    public static bool IsValidZipStructure(string zipPath, string brand)
    {
        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            return false;
        if (string.IsNullOrWhiteSpace(brand))
            return false;

        try
        {
            using var stream = File.OpenRead(zipPath);
            return IsValidZipStructure(stream, brand);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidZipStructure(byte[] zipData, string brand)
    {
        if (zipData == null || zipData.Length == 0)
            return false;
        if (string.IsNullOrWhiteSpace(brand))
            return false;

        try
        {
            using var stream = new MemoryStream(zipData);
            return IsValidZipStructure(stream, brand);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidZipStructure(Stream zipStream, string brand)
    {
        if (zipStream == null || string.IsNullOrWhiteSpace(brand))
            return false;

        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith(brand + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

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
            else if (!IsValidBrand(driver.Brand))
                result.AddError($"驱动包 Brand '{driver.Brand}' 只能包含英文、数字、短横线或下划线。");
            if (!brands.Add(driver.Brand))
                result.AddError($"驱动包 Brand '{driver.Brand}' 重复。");

            if (driver.ZipData is { Length: > 0 } zipData)
            {
                if (!IsValidZipStructure(zipData, driver.Brand))
                    result.AddError($"驱动包 '{driver.Brand}' 的 ZIP 顶层缺少与 Brand 同名的文件夹。");
            }
            else
            {
                if (!IsValidZipFileName(driver.ZipFilePath))
                    result.AddError($"驱动包 '{driver.Brand}' 的 ZIP 文件名不合法，只能包含英文、数字、短横线或下划线，且以 .zip 结尾。");
                else if (!File.Exists(driver.ZipFilePath))
                    result.AddError($"驱动包 '{driver.Brand}' 的 ZIP 文件不存在：{driver.ZipFilePath}");
                else if (!IsValidZipStructure(driver.ZipFilePath, driver.Brand))
                    result.AddError($"驱动包 '{driver.Brand}' 的 ZIP 顶层缺少与 Brand 同名的文件夹。");
            }

            if (string.IsNullOrWhiteSpace(driver.DefaultDriverName))
                result.AddError($"驱动包 '{driver.Brand}' 的默认驱动名不能为空。");
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
