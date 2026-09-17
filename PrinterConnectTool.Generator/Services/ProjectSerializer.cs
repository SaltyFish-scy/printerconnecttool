using System.Text.Json;
using System.Text.Json.Serialization;
using PrinterConnectTool.Generator.Models;

namespace PrinterConnectTool.Generator.Services;

public static class ProjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void Save(GeneratorProject project, string filePath)
    {
        // 保存前同步冗余快照字段，让 1.3 及更早版本的生成器打开新文件时仍能看到正确的驱动关联
        foreach (var p in project.Printers)
        {
            var driver = project.FindDriver(p);
            if (driver != null)
            {
                p.DriverId = driver.Id;
                p.DriverBrand = driver.Brand;
            }
        }

        var dto = new GeneratorProjectDto
        {
            Version = GeneratorProject.CurrentVersion,
            ShellTitle = project.ShellTitle,
            PingTimeoutMs = project.PingTimeoutMs,
            OverallTimeoutMs = project.OverallTimeoutMs,
            Drivers = project.Drivers.ToList(),
            Offices = project.Offices.ToList(),
            Printers = project.Printers.ToList()
        };

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, JsonSerializer.Serialize(dto, Options));
    }

    public static GeneratorProject Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var dto = JsonSerializer.Deserialize<GeneratorProjectDto>(json, Options)
                  ?? throw new InvalidOperationException("项目文件解析失败");

        var project = new GeneratorProject
        {
            Version = dto.Version,
            ShellTitle = dto.ShellTitle,
            PingTimeoutMs = dto.PingTimeoutMs,
            OverallTimeoutMs = dto.OverallTimeoutMs
        };

        foreach (var d in dto.Drivers) project.Drivers.Add(d);
        foreach (var o in dto.Offices) project.Offices.Add(o);
        foreach (var p in dto.Printers)
        {
            MigratePrinter(project, p);
            project.Printers.Add(p);
        }

        return project;
    }

    /// <summary>
    ///     兼容 1.3 及更早版本：旧文件中打印机只有 DriverBrand 快照、没有 DriverId，
    ///     且驱动包没有 Id。加载时按 Brand 找回驱动并补上活引用。
    ///     Brand 已匹配不到驱动的打印机保持原样，由生成前校验拦截提示。
    /// </summary>
    private static void MigratePrinter(GeneratorProject project, PrinterDefinition printer)
    {
        if (string.IsNullOrEmpty(printer.DriverId) && !string.IsNullOrEmpty(printer.DriverBrand))
        {
            var match = project.Drivers.FirstOrDefault(d => d.Brand == printer.DriverBrand);
            if (match != null)
                printer.DriverId = match.Id;
        }

        var resolved = project.FindDriver(printer);
        if (resolved != null)
            printer.DriverBrand = resolved.Brand;
    }

    private class GeneratorProjectDto
    {
        public string Version { get; set; } = "1.0";
        public string ShellTitle { get; set; } = "打印机自助连接工具";
        public int PingTimeoutMs { get; set; }
        public int OverallTimeoutMs { get; set; }
        public List<DriverPackage> Drivers { get; set; } = new();
        public List<OfficeDefinition> Offices { get; set; } = new();
        public List<PrinterDefinition> Printers { get; set; } = new();
    }
}
