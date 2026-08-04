using System.Text.Json.Serialization;

namespace PrinterConnectTool.Models;

/// <summary>
///     打印机配置模型
/// </summary>
public class PrinterConfig
{
    /// <summary>连接后的打印机名称（如"上海大区-4F销委会打印机"）</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>打印机 IP 地址</summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>驱动名称（需与 INF 文件中完全一致）</summary>
    [JsonPropertyName("driverName")]
    public string DriverName { get; set; } = string.Empty;

    /// <summary>驱动文件夹标识（对应 Drivers/ 下的子目录名）</summary>
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    /// <summary>对应的安装脚本文件名（如 install_shanghai_4f_xiaoweihui.ps1）</summary>
    [JsonPropertyName("script")]
    public string Script { get; set; } = string.Empty;

    /// <summary>端口号（默认 9100）</summary>
    [JsonPropertyName("portNumber")]
    public int PortNumber { get; set; } = 9100;
}