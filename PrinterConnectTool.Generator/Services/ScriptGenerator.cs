using System.Reflection;
using System.Text;
using PrinterConnectTool.Generator.Models;

namespace PrinterConnectTool.Generator.Services;

/// <summary>
///     基于 UTF-8 with BOM 模板生成 PS1 安装脚本，仅替换顶部配置区占位符。
/// </summary>
public static class ScriptGenerator
{
    private const string TemplateResourceName = "PrinterConnectTool.Generator.Resources.ScriptTemplate.ps1";

    public static void GenerateToStream(PrinterDefinition printer, GeneratorProject project, Stream output)
    {
        var driver = project.Drivers.FirstOrDefault(d => d.Brand == printer.DriverBrand)
                     ?? throw new InvalidOperationException($"找不到打印机 '{printer.Name}' 关联的驱动包 '{printer.DriverBrand}'。");

        var template = ReadTemplate();
        var driverFolder = @$"C:\Drivers\{driver.Brand}";
        var portName = $"IP_{printer.Ip}";

        var content = template
            .Replace("{PRINTER_IP}", EscapeSingleQuotes(printer.Ip))
            .Replace("{PRINTER_NAME}", EscapeSingleQuotes(printer.Name))
            .Replace("{DRIVER_NAME}", EscapeSingleQuotes(printer.DriverName))
            .Replace("{DRIVER_FOLDER}", EscapeSingleQuotes(driverFolder))
            .Replace("{PORT_NAME}", EscapeSingleQuotes(portName));

        using var writer = new StreamWriter(output, new UTF8Encoding(true), leaveOpen: true);
        writer.Write(content);
        writer.Flush();
    }

    public static void GenerateToFile(PrinterDefinition printer, GeneratorProject project, string outputPath)
    {
        using var fs = File.Create(outputPath);
        GenerateToStream(printer, project, fs);
    }

    private static string ReadTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(TemplateResourceName)
                           ?? throw new InvalidOperationException($"找不到 PS1 模板资源: {TemplateResourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string EscapeSingleQuotes(string value)
    {
        return value.Replace("'", "''");
    }
}
