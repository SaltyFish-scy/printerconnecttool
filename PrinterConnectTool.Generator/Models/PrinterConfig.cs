using System.Text.Json.Serialization;

namespace PrinterConnectTool.Models;

public class PrinterConfig
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("ip")] public string Ip { get; set; } = string.Empty;
    [JsonPropertyName("driverName")] public string DriverName { get; set; } = string.Empty;
    [JsonPropertyName("brand")] public string Brand { get; set; } = string.Empty;
    [JsonPropertyName("script")] public string Script { get; set; } = string.Empty;
    [JsonPropertyName("portNumber")] public int PortNumber { get; set; } = 9100;
}
