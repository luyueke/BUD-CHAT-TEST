using System;
using System.IO;
using System.Text;
using Game.BLE;
/// <summary>
/// 将 QRPayload 二进制序列化为 Base64url 字符串，减少二维码密度。
/// 原始 JSON ~270 字符 → 编码后 ~122 字符，缩减约 55%。
/// </summary>
public static class QRPayloadEncoder
{
    // transport 枚举，追加新类型时在末尾添加
    private static readonly string[] Transports = { "ble", "wifi", "nfc" };

    public static string Encode(QrEasyCodeData payload)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((byte)payload.a);

        int transportIdx = Array.IndexOf(Transports, payload.b);
        writer.Write((byte)(transportIdx < 0 ? 0 : transportIdx));

        WriteInt64BigEndian(writer, payload.g);
        writer.Write(HexToBytes(payload.e));
        writer.Write(HexToBytes(payload.f));
        writer.Write(HexToBytes(payload.d.Replace("-", "")));
        writer.Write(HexToBytes(payload.h));

        byte[] deviceIdBytes = Encoding.UTF8.GetBytes(payload.c);
        writer.Write((byte)deviceIdBytes.Length);
        writer.Write(deviceIdBytes);

        return Base64UrlEncode(ms.ToArray());
    }

    public static QrCodeData Decode(string encoded)
    {
        byte[] data = Base64UrlDecode(encoded);
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var payload = new QrCodeData();

        payload.version = reader.ReadByte();
        int transportIdx = reader.ReadByte();
        payload.transport = transportIdx < Transports.Length ? Transports[transportIdx] : Transports[0];
        payload.expireAt = ReadInt64BigEndian(reader);
        payload.advHint = BytesToHex(reader.ReadBytes(4));
        payload.bindToken = BytesToHex(reader.ReadBytes(16));

        string uuidHex = BytesToHex(reader.ReadBytes(16)).ToLower();
        payload.serviceUuid = $"{uuidHex.Substring(0, 8)}-{uuidHex.Substring(8, 4)}-{uuidHex.Substring(12, 4)}-{uuidHex.Substring(16, 4)}-{uuidHex.Substring(20)}";

        payload.signature = BytesToHex(reader.ReadBytes(32)).ToLower();

        int deviceIdLen = reader.ReadByte();
        payload.deviceId = Encoding.UTF8.GetString(reader.ReadBytes(deviceIdLen));

        return payload;
    }

    private static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    private static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace("-", "");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    private static void WriteInt64BigEndian(BinaryWriter writer, long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        writer.Write(bytes);
    }

    private static long ReadInt64BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(8);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt64(bytes, 0);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] Base64UrlDecode(string encoded)
    {
        string s = encoded.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
