using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

/// <summary>
/// 豆包流式 ASR WebSocket 二进制协议工具类。
///
/// 帧结构（每帧 = Header 4字节 + PayloadSize 4字节大端 uint32 + Payload）：
///   Byte 0: [version:4bit][headerSize:4bit]  固定 0x11（version=1, size=1×4=4字节）
///   Byte 1: [msgType:4bit][flags:4bit]
///   Byte 2: [serialization:4bit][compression:4bit]
///   Byte 3: 0x00（reserved）
/// </summary>
public static class ASRProtocol
{
    /// <summary>日志开关，与 ASRManager.EnableLog 联动赋值即可（由 ASRManager 在启动时同步）。</summary>
    public static bool EnableLog = true;

    private static void Log(string msg)      { if (EnableLog) Debug.Log($"[ASRProtocol] {msg}"); }
    private static void LogError(string msg) { Debug.LogError($"[ASRProtocol] {msg}"); }

    // Byte 0 固定值：version=1, headerSize=1（实际字节数 = 1×4 = 4）
    private const byte HeaderByte0 = 0x11;
    private const byte Reserved    = 0x00;

    // Byte 1 高 4 位：消息类型
    private const byte MsgTypeFullClientRequest  = 0x1; // 客户端首包，携带 JSON 参数
    private const byte MsgTypeAudioOnly          = 0x2; // 客户端音频包，携带 PCM 数据
    private const byte MsgTypeFullServerResponse = 0x9; // 服务端识别结果（JSON）
    private const byte MsgTypeError              = 0xF; // 服务端错误帧

    // Byte 1 低 4 位：标志位
    private const byte FlagsNone      = 0x0; // 普通包
    private const byte FlagsLastAudio = 0x2; // 最后一包音频，服务端收到后返回最终结果

    // Byte 2 高 4 位：序列化方式
    private const byte SerializationNone = 0x0; // 无序列化（原始字节）
    private const byte SerializationJSON = 0x1; // JSON 格式

    // Byte 2 低 4 位：压缩方式
    private const byte CompressionNone = 0x0; // 不压缩
    private const byte CompressionGzip = 0x1; // Gzip 压缩

    // -------------------------------------------------------
    // 公共方法
    // -------------------------------------------------------

    /// <summary>
    /// 构建首包（Full Client Request）。
    /// WebSocket 建连后发送的第一帧，携带 appid/token/cluster 及音频格式参数。
    /// Payload = Gzip(JSON)。
    /// </summary>
    public static byte[] BuildFullClientRequest(ASRConfigAsset cfg, string reqId, string uid)
    {
        // 根据配置决定开启哪些后处理流程
        string workflow = "audio_in,resample,partition,vad,fe,decode";
        if (cfg.EnableITN)        workflow += ",itn";
        if (cfg.EnablePunctuation) workflow += ",nlu_punctuate";

        var json = $@"{{
  ""app"":{{""appid"":""{cfg.AppId}"",""token"":""{cfg.Token}"",""cluster"":""{cfg.Cluster}""}},
  ""user"":{{""uid"":""{uid}""}},
  ""audio"":{{""format"":""raw"",""codec"":""raw"",""rate"":16000,""bits"":16,""channel"":1}},
  ""request"":{{
    ""reqid"":""{reqId}"",
    ""sequence"":1,
    ""workflow"":""{workflow}"",
    ""nbest"":1,
    ""show_utterances"":true,
    ""result_type"":""single"",
    ""vad_signal"":true,
    ""vad_silence_time"":""{cfg.VadSilenceTime}""
  }}
}}";
        byte[] payload = GzipCompress(Encoding.UTF8.GetBytes(json));
        byte header1 = (byte)((MsgTypeFullClientRequest << 4) | FlagsNone);
        byte header2 = (byte)((SerializationJSON << 4) | CompressionGzip);
        return BuildFrame(header1, header2, payload);
    }

    /// <summary>
    /// 构建音频包（Audio Only Request）。
    /// Payload = Gzip(PCM16 原始字节)。
    /// </summary>
    /// <param name="pcm16">16-bit little-endian PCM 数据</param>
    /// <param name="isLast">true = 最后一包，服务端收到后开始输出最终识别结果</param>
    public static byte[] BuildAudioOnlyRequest(byte[] pcm16, bool isLast)
    {
        byte[] payload = GzipCompress(pcm16);
        byte flags   = isLast ? FlagsLastAudio : FlagsNone;
        byte header1 = (byte)((MsgTypeAudioOnly << 4) | flags);
        // Java SDK 音频帧 header[2] 同样使用 JSON+Gzip（0x11），与参数帧保持一致
        byte header2 = (byte)((SerializationJSON << 4) | CompressionGzip);
        return BuildFrame(header1, header2, payload);
    }

    /// <summary>
    /// 解析服务端下发的二进制帧，返回结构化响应。
    /// 支持 Full Server Response（JSON）和 Error 两种帧类型。
    /// </summary>
    public static ASRServerResponse ParseServerResponse(byte[] data)
    {
        if (data == null || data.Length < 8)
            return new ASRServerResponse { code = -1, message = "invalid response length" };

        // 从 Header 中解析各字段（nibble 操作）
        byte msgType       = (byte)((data[1] >> 4) & 0xF);
        byte flags         = (byte)(data[1] & 0xF);
        byte serialization = (byte)((data[2] >> 4) & 0xF);
        byte compression   = (byte)(data[2] & 0xF);

        Log($"header: msgType=0x{msgType:X} flags=0x{flags:X} serial=0x{serialization:X} compress=0x{compression:X} dataLen={data.Length}");

        if (msgType == MsgTypeError)
        {
            // Error 帧布局：Header(4) + ErrorCode(4, big-endian int32) + MsgSize(4, big-endian int32) + Msg(压缩 UTF8)
            int errCode = ReadInt32BigEndian(data, 4);
            int msgLen  = ReadInt32BigEndian(data, 8);
            string msg  = "server error";
            if (data.Length >= 12 + msgLen)
            {
                byte[] msgBytes = new byte[msgLen];
                Array.Copy(data, 12, msgBytes, 0, msgLen);
                // 错误消息体同样受 Header 中 compression 字段约束
                if (compression == CompressionGzip)
                    msgBytes = GzipDecompress(msgBytes);
                msg = Encoding.UTF8.GetString(msgBytes);
            }
            Log($"error frame: code={errCode} msg={msg}");
            return new ASRServerResponse { code = errCode, message = msg };
        }

        // Full Server Response 布局：Header(4) + PayloadSize(4, big-endian uint32) + Payload(压缩 JSON)
        uint payloadSize = ReadUInt32BigEndian(data, 4);
        if (data.Length < 8 + payloadSize)
            return new ASRServerResponse { code = -1, message = "truncated payload" };

        byte[] payloadBytes = new byte[payloadSize];
        Array.Copy(data, 8, payloadBytes, 0, (int)payloadSize);

        if (compression == CompressionGzip)
            payloadBytes = GzipDecompress(payloadBytes);

        string jsonStr = Encoding.UTF8.GetString(payloadBytes);
        Log($"server response JSON: {jsonStr}");
        try
        {
            return JsonUtility.FromJson<ASRServerResponse>(jsonStr);
        }
        catch (Exception e)
        {
            LogError($"JSON parse error: {e.Message}\n{jsonStr}");
            return new ASRServerResponse { code = -1, message = "json parse error" };
        }
    }

    /// <summary>
    /// 将 Unity AudioClip 的 float 采样（范围 -1~1）转换为 16-bit little-endian PCM 字节数组。
    /// </summary>
    public static byte[] FloatsToPCM16(float[] samples)
    {
        byte[] pcm = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
            pcm[i * 2]     = (byte)(s & 0xFF);         // 低字节
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);  // 高字节
        }
        return pcm;
    }

    // -------------------------------------------------------
    // 内部辅助方法
    // -------------------------------------------------------

    /// <summary>
    /// 拼装完整帧：Header(4) + PayloadSize(4, big-endian) + Payload。
    /// </summary>
    private static byte[] BuildFrame(byte header1, byte header2, byte[] payload)
    {
        byte[] frame = new byte[4 + 4 + payload.Length];
        frame[0] = HeaderByte0;
        frame[1] = header1;
        frame[2] = header2;
        frame[3] = Reserved;
        WriteUInt32BigEndian(frame, 4, (uint)payload.Length);
        Array.Copy(payload, 0, frame, 8, payload.Length);
        return frame;
    }

    private static byte[] GzipCompress(byte[] data)
    {
        using (var ms = new MemoryStream())
        {
            using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                gz.Write(data, 0, data.Length);
            return ms.ToArray();
        }
    }

    private static byte[] GzipDecompress(byte[] data)
    {
        using (var input  = new MemoryStream(data))
        using (var gz     = new GZipStream(input, CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            gz.CopyTo(output);
            return output.ToArray();
        }
    }

    // 协议规定整数字段使用大端表示
    private static void WriteUInt32BigEndian(byte[] buf, int offset, uint value)
    {
        buf[offset]     = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)(value);
    }

    private static uint ReadUInt32BigEndian(byte[] buf, int offset)
    {
        return ((uint)buf[offset]     << 24)
             | ((uint)buf[offset + 1] << 16)
             | ((uint)buf[offset + 2] << 8)
             |        buf[offset + 3];
    }

    private static int ReadInt32BigEndian(byte[] buf, int offset)
    {
        return (buf[offset]     << 24)
             | (buf[offset + 1] << 16)
             | (buf[offset + 2] << 8)
             |  buf[offset + 3];
    }
}

// -------------------------------------------------------
// 服务端响应数据模型（与 JsonUtility 兼容，字段名须与 JSON key 一致）
// -------------------------------------------------------

[Serializable]
public class ASRServerResponse
{
    public string reqid;     // 对应请求的 reqid
    public int    code;      // 状态码，1000 = 成功，其他见错误码表
    public string message;   // 状态描述
    public int    sequence;  // 对应请求包序号，负数表示最终响应
    public ASRResultItem[] result;
}

[Serializable]
public class ASRResultItem
{
    public string            text;        // 整段音频的识别文本
    public int               confidence;  // 置信度
    public ASRUtteranceRaw[] utterances;  // 分句列表
}

[Serializable]
public class ASRUtteranceRaw
{
    public string text;        // 分句文本
    public int    start_time;  // 分句起始时间（ms）
    public int    end_time;    // 分句结束时间（ms）
    public bool   definite;    // true = 分句已确定，false = 中间结果仍可能变化
}
