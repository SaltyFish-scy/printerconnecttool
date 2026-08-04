using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using PrinterConnectTool.Models;

namespace PrinterConnectTool.Services;

/// <summary>
///     配置加载器：优先读取同目录外部 JSON，不存在则用嵌入资源
/// </summary>
[SupportedOSPlatform("windows")]
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    ///     加载配置：外部文件优先 > 嵌入资源
    /// </summary>
    public static AppConfig Load(ILogger logger)
    {
        // 1. 尝试读取 EXE 同目录下的 workplaces.json
        var exeDir = AppContext.BaseDirectory;
        var externalPath = Path.Combine(exeDir, "workplaces.json");

        if (File.Exists(externalPath))
            try
            {
                var json = File.ReadAllText(externalPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (config != null)
                {
                    logger.Info("配置来源：外部 workplaces.json");
                    return config;
                }
            }
            catch
            {
                // 外部文件解析失败，回退到嵌入资源
            }

        // 2. 读取嵌入资源中的 workplaces.json
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PrinterConnectTool.Config.workplaces.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new InvalidOperationException("找不到嵌入的配置文件 workplaces.json");

        using var reader = new StreamReader(stream);
        var embeddedJson = reader.ReadToEnd();
        return JsonSerializer.Deserialize<AppConfig>(embeddedJson, JsonOptions)
               ?? throw new InvalidOperationException("配置文件解析失败");
    }
}