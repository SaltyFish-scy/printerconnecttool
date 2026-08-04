using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PrinterConnectTool.Generator.Models;

namespace PrinterConnectTool.Generator.Services;

/// <summary>
///     将 Payload 追加到壳 EXE 尾部，生成最终的单文件 PrinterConnectTool.Desktop.exe。
///     尾部格式：Shell | Payload ZIP | Marker(16B) | Length(u64) | SHA256(32B) | Magic(8B)
/// </summary>
public static class ShellPublisher
{
    private const string Magic = "PCTV1\0\0\0";
    private const string MarkerText = "PCTDATA";
    private const string ShellResourceName = "PrinterConnectTool.Generator.Resources.Shell.exe";

    public static void Publish(GeneratorProject project, string outputPath)
    {
        var shellBytes = ReadShell();
        var payloadBytes = PayloadBuilder.Build(project);
        var sha256 = SHA256.HashData(payloadBytes);
        var marker = Encoding.ASCII.GetBytes(MarkerText.PadRight(16, '\0'));
        var length = BitConverter.GetBytes((ulong)payloadBytes.Length);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var fs = File.Create(outputPath);
        fs.Write(shellBytes);
        fs.Write(payloadBytes);
        fs.Write(marker);
        fs.Write(length);
        fs.Write(sha256);
        fs.Write(Encoding.ASCII.GetBytes(Magic));
        fs.Flush();
    }

    private static byte[] ReadShell()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ShellResourceName)
                           ?? throw new InvalidOperationException($"找不到壳资源 {ShellResourceName}。请先编译壳项目并复制 Shell.exe 到 Resources 目录。");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
