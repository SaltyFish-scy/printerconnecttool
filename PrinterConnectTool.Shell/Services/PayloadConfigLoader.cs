using System.Text.Json;
using PrinterConnectTool.Models;

namespace PrinterConnectTool.Shell.Services;

/// <summary>
///     从 Payload 解压目录加载 workplaces.json
/// </summary>
public static class PayloadConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static AppConfig Load(string payloadRoot)
    {
        var path = Path.Combine(payloadRoot, "Config", "workplaces.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, Options)
               ?? throw new InvalidOperationException("配置文件解析失败");
    }
}
