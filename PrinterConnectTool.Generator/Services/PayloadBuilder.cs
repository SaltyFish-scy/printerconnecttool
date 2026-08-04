using System.IO.Compression;
using System.Text.Json;
using PrinterConnectTool.Generator.Models;
using PrinterConnectTool.Models;

namespace PrinterConnectTool.Generator.Services;

/// <summary>
///     根据生成器项目构建 Payload ZIP（包含 Config、Drivers、Scripts）。
/// </summary>
public static class PayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] Build(GeneratorProject project)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var config = BuildAppConfig(project);
            var configEntry = zip.CreateEntry("Config/workplaces.json");
            using (var s = configEntry.Open())
                JsonSerializer.Serialize(s, config, JsonOptions);

            foreach (var driver in project.Drivers)
            {
                var entry = zip.CreateEntry($"Drivers/{driver.Brand}.zip");
                using var src = File.OpenRead(driver.ZipFilePath);
                using var dst = entry.Open();
                src.CopyTo(dst);
            }

            for (int i = 0; i < project.Printers.Count; i++)
            {
                var p = project.Printers[i];
                var entry = zip.CreateEntry($"Scripts/install_printer_{i + 1}.ps1");
                using var msScript = new MemoryStream();
                ScriptGenerator.GenerateToStream(p, project, msScript);
                msScript.Position = 0;
                using var dst = entry.Open();
                msScript.CopyTo(dst);
            }
        }

        return ms.ToArray();
    }

    public static AppConfig BuildAppConfig(GeneratorProject project)
    {
        var config = new AppConfig
        {
            Settings = new AppSettings
            {
                PingTimeoutMs = project.PingTimeoutMs,
                OverallTimeoutMs = project.OverallTimeoutMs
            }
        };

        foreach (var office in project.Offices)
        {
            var workplace = new WorkplaceConfig
            {
                Name = office.Name,
                GatewayIp = office.GatewayIp,
                Printers = new List<PrinterConfig>()
            };

            var officePrinters = project.Printers.Where(p => p.OfficeId == office.Id).ToList();
            foreach (var p in officePrinters)
            {
                workplace.Printers.Add(new PrinterConfig
                {
                    Name = p.Name,
                    Ip = p.Ip,
                    DriverName = p.DriverName,
                    Brand = p.DriverBrand,
                    Script = $"install_printer_{project.Printers.IndexOf(p) + 1}.ps1",
                    PortNumber = p.PortNumber
                });
            }

            config.Workplaces.Add(workplace);
        }

        return config;
    }
}
