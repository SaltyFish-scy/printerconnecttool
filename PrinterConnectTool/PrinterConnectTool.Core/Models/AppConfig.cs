using System.Text.Json.Serialization;

namespace PrinterConnectTool.Models;

/// <summary>
///     全局配置根模型（对应 workplaces.json）
/// </summary>
public class AppConfig
{
    [JsonPropertyName("workplaces")] public List<WorkplaceConfig> Workplaces { get; set; } = new();

    [JsonPropertyName("settings")] public AppSettings Settings { get; set; } = new();
}

public class AppSettings
{
    [JsonPropertyName("pingTimeoutMs")] public int PingTimeoutMs { get; set; } = 2000;

    [JsonPropertyName("overallTimeoutMs")] public int OverallTimeoutMs { get; set; } = 3000;
}