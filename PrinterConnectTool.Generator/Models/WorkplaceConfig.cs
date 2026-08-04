using System.Text.Json.Serialization;

namespace PrinterConnectTool.Models;

public class WorkplaceConfig
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("gatewayIp")] public string GatewayIp { get; set; } = string.Empty;
    [JsonPropertyName("printers")] public List<PrinterConfig> Printers { get; set; } = new();
}
