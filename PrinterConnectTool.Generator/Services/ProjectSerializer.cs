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
        var dto = new GeneratorProjectDto
        {
            Version = project.Version,
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
        foreach (var p in dto.Printers) project.Printers.Add(p);

        return project;
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
