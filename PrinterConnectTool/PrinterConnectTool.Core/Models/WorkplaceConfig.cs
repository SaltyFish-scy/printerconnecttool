using System.Text.Json.Serialization;

namespace PrinterConnectTool.Models;

/// <summary>
///     职场配置模型
/// </summary>
public class WorkplaceConfig
{
    /// <summary>职场名称（如"上海大区"）</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>探测网关 IP（用于判断是否在该职场内网）</summary>
    [JsonPropertyName("gatewayIp")]
    public string GatewayIp { get; set; } = string.Empty;

    /// <summary>该职场下的打印机列表</summary>
    [JsonPropertyName("printers")]
    public List<PrinterConfig> Printers { get; set; } = new();
}