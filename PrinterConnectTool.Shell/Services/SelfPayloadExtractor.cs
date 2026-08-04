using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace PrinterConnectTool.Shell.Services;

/// <summary>
///     从 EXE 自身尾部读取追加的 Payload ZIP。
///     尾部格式（小端序）：
///     [Marker: 16 bytes "PCTDATA\0" 补齐]
///     [PayloadLength: uint64]
///     [PayloadSha256: 32 bytes]
///     [Magic: 8 bytes "PCTV1\0\0\0"]
/// </summary>
public static class SelfPayloadExtractor
{
    private const string Magic = "PCTV1\0\0\0";
    private const int MagicLength = 8;
    private const int Sha256Length = 32;
    private const int LengthFieldLength = sizeof(ulong);
    private const int MarkerLength = 16;
    private const string MarkerText = "PCTDATA";

    private static readonly int FooterLength = MagicLength + Sha256Length + LengthFieldLength + MarkerLength;

    public static string? ExtractPayload(string exePath, string extractBase)
    {
        using var fs = File.OpenRead(exePath);
        if (fs.Length < FooterLength) return null;

        fs.Seek(-FooterLength, SeekOrigin.End);

        var marker = ReadExact(fs, MarkerLength);
        if (Encoding.ASCII.GetString(marker).TrimEnd('\0') != MarkerText)
            return null;

        var lengthBytes = ReadExact(fs, LengthFieldLength);
        var payloadLength = BitConverter.ToUInt64(lengthBytes);

        var shaBytes = ReadExact(fs, Sha256Length);

        var magic = ReadExact(fs, MagicLength);
        if (Encoding.ASCII.GetString(magic) != Magic)
            return null;

        var totalFooterLength = (long)FooterLength;
        var totalPayloadLength = (long)payloadLength;
        if (fs.Length < totalFooterLength + totalPayloadLength)
            return null;

        fs.Seek(-(totalFooterLength + totalPayloadLength), SeekOrigin.End);
        var payloadBytes = ReadExact(fs, totalPayloadLength);

        var computed = SHA256.HashData(payloadBytes);
        if (!computed.SequenceEqual(shaBytes))
            throw new InvalidOperationException("Payload 校验失败，EXE 可能已被破坏。");

        if (Directory.Exists(extractBase)) Directory.Delete(extractBase, true);
        Directory.CreateDirectory(extractBase);

        var zipPath = Path.Combine(extractBase, "payload.zip");
        File.WriteAllBytes(zipPath, payloadBytes);
        ZipFile.ExtractToDirectory(zipPath, extractBase, overwriteFiles: true);
        File.Delete(zipPath);

        return extractBase;
    }

    private static byte[] ReadExact(Stream stream, long count)
    {
        var buffer = new byte[count];
        stream.ReadExactly(buffer);
        return buffer;
    }
}
