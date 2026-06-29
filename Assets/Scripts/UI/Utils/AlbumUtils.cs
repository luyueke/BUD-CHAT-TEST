using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Basic.Utils;
using Game.COSXML;
using GameData;
using GameData.Base;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.Networking;
public class AuditImageData
{
    public int auditResult;
}

public class AlbumUtils : GlobalInstance<AlbumUtils>
{

    public int DayUploadIndex
    {

        get
        {
            var value = PlayerPrefs.GetString("Album_DayUploadIndex", "");
            if (string.IsNullOrEmpty(value))
            {
                PlayerPrefs.SetString("Album_DayUploadIndex", $"{GameUtils.GetTimeDay()}_1");
                return 1;
            }
            var split = value.Split('_');
            var day = split[0];
            var index = int.Parse(split[1]);
            if (day != GameUtils.GetTimeDay())
            {
                PlayerPrefs.SetString("Album_DayUploadIndex", $"{GameUtils.GetTimeDay()}_1");
                return 1;
            }
            else
            {
                index++;
                PlayerPrefs.SetString("Album_DayUploadIndex", $"{day}_{index}");
                return index;
            }
        }
    }

    public void UploadAlbum(Action<string> onSuccess, Action<string> onFail = null)
    {
        string remotePath = GlobalConfig.UPLOAD_PATH_ALBUM;
        OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
        {
            albumType = 1,
            isCrop = 0,
        };
        AlbumProcess process = new AlbumProcess();
        process.UploadAlbum(remotePath, albumParams, onSuccess, onFail);
    }

    public void UploadHead(Action<string> onSuccess, Action<string> onFail = null)
    {
        string remotePath = GlobalConfig.UPLOAD_PATH_CHARACTER;
        OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
        {
            albumType = 1,
            isCrop = 1, //裁剪
            cropAspectRatio = 1, //宽高比
        };
        AlbumProcess process = new AlbumProcess();
        process.UploadAlbum(remotePath, albumParams, onSuccess, onFail);
    }

    public void UploadMusic(int musicLen, Action<string> onSuccess, Action<string> onFail = null, Action<float> onGetMusicLen = null, Action<int> onGetMusicLoudness = null)
    {
        string remotePath = GlobalConfig.UPLOAD_PATH_MUSIC;
        OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
        {
            albumType = 0,
            length = musicLen,
        };
        AlbumProcess process = new AlbumProcess();
        process.UploadAlbum(remotePath, albumParams, onSuccess, onFail, onGetMusicLen, onGetMusicLoudness);
    }

}

public class AlbumResData
{
    /// <summary>
    ///  本地图片绝对路径
    /// </summary>
    public string localUrl;
    /// <summary>
    /// 0 视频  1 图片
    /// </summary>
    public int mediaType;
}


public class AlbumProcess
{
    public Action<string> OnSuccess
    {
        get; set;
    }
    public Action<string> OnFail
    {
        get; set;
    }

    public Action<float> _onGetMusicLen
    {
        get; set;
    }

    public Action<int> _onGetMusicLoudness
    {
        get; set;
    }

    private OpenSystemAlbumParams _albumParams;
    private string _remotePath;

    public void UploadAlbum(string remotePath, OpenSystemAlbumParams albumParams, Action<string> onSuccess, Action<string> onFail = null, Action<float> onGetMusicLen = null, Action<int> onGetMusicLoudness = null)
    {
        _remotePath = remotePath;
        OnSuccess = onSuccess;
        OnFail = onFail;
        _onGetMusicLen = onGetMusicLen;
        _onGetMusicLoudness = onGetMusicLoudness;
        _albumParams = albumParams;


        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.openSystemAlbum, OnNativeFail);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUri);
        MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
#if UNITY_EDITOR
        AlbumResData authData = new AlbumResData();
        if (albumParams.albumType == 0)
        {
            authData.localUrl = Path.Combine(Application.streamingAssetsPath, "U3D/Music/simple_music_02.mp3");
        }
        else
        {
            authData.localUrl = Path.Combine(Application.streamingAssetsPath, "U3D/Head/glass-975494_640.jpg");
        }
        OnNativeUri(JsonConvert.SerializeObject(authData));
#endif
    }

    private void OnNativeFail(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.openSystemAlbum);
        OnFail?.Invoke($"上传照片失败，请重试 ");
    }

    private void OnNativeUri(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.openSystemAlbum);
        if (string.IsNullOrEmpty(msg))
        {
            OnFail?.Invoke("上传照片失败，请重试");
            return;
        }
        AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
        if (string.IsNullOrEmpty(authData.localUrl))
        {
            OnFail?.Invoke("上传照片失败，请重试");
            return;
        }
        UploadImg(authData);
    }


    /// <summary>
    /// 上传本地音频/图片。音乐类型（albumType==0）会在上传前将音频转为单声道 WAV，
    /// 其他类型直接上传原始文件。
    /// </summary>
    void UploadImg(AlbumResData authData)
    {
        CoroutineManager.Inst.StartCoroutine(UploadImgCoroutine(authData));
    }

    /// <summary>
    /// 上传主协程：音乐类型先加载 AudioClip 获取元信息并转换为单声道 WAV，再执行上传；
    /// 图片类型跳过音频处理直接上传。
    /// 视频文件（.mp4 / .mov / .m4v）使用 AudioType.UNKNOWN，让平台原生解码器
    /// （iOS AVFoundation / Android MediaPlayer）自动提取 MP4 容器的音频轨道。
    /// Unity Editor（Windows）不支持对视频文件解码，编辑器测试路径固定使用 .mp3 文件。
    /// </summary>
    /// <param name="authData">包含本地文件路径的上传数据</param>
    private IEnumerator UploadImgCoroutine(AlbumResData authData)
    {
        // 上传路径默认使用原始文件名
        var uri = $"{_remotePath}{AccountDataManager.Inst.Uid}/{Path.GetFileName(authData.localUrl)}";

        // 音频文件需要先加载 AudioClip，获取时长/响度，并转换为单声道 WAV
        string tempWavPath = null;

        if (_albumParams.albumType == 0)
        {
            string fileExt = Path.GetExtension(authData.localUrl).ToLowerInvariant();

            // 视频文件使用 UNKNOWN，让平台原生解码器（iOS AVFoundation / Android MediaPlayer）
            // 自动提取 MP4 容器内的音频轨道；音频文件（.mp3）使用 MPEG 解码器。
            // 注意：Unity Editor（Windows/FMOD）不支持 UNKNOWN 对视频文件解码，
            // 因此编辑器测试路径固定使用 .mp3 文件，不会走到 isVideoFile == true 的分支。
            bool isVideoFile = fileExt == ".mp4" || fileExt == ".mov" || fileExt == ".m4v";
            AudioType audioType = isVideoFile ? AudioType.UNKNOWN : AudioType.MPEG;

            // Windows 下 Path.Combine 会产生反斜杠，统一转为正斜杠供 file:// URL 使用
            string localUrl = authData.localUrl.Replace('\\', '/');

            // ① 异步加载本地音频/视频为 AudioClip
            AudioClip clip = null;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + localUrl, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError
                    || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("加载本地文件失败: " + www.error);
                }
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(www);

                    if (clip == null)
                    {
                        Debug.LogError("无法解码文件音频，回退为原始文件上传");
                    }
                }
            }

            if (clip != null)
            {
                // ② 回调音频时长和响度
                float length = clip.length;
                Debug.Log("音频时长: " + length + " 秒");
                _onGetMusicLen?.Invoke(length);

                int loudness = clip.Loudness();
                Debug.Log("音频响度: " + loudness);
                _onGetMusicLoudness?.Invoke(loudness);

                // ③ 若为多声道，降混为单声道
                if (clip.channels > 1)
                {
                    Debug.Log($"检测到 {clip.channels} 声道，开始降混为单声道");
                    clip = ConvertToMono(clip);
                }

                // ④ 将 AudioClip 保存为 WAV 临时文件
                string tempDir = Path.Combine(Application.persistentDataPath, "AudioTemp");

                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                tempWavPath = Path.Combine(tempDir, "upload_mono.wav");

                if (SaveWavPcm(clip, tempWavPath))
                {
                    authData.localUrl = tempWavPath;
                    Debug.Log("单声道 WAV 已保存至: " + tempWavPath);
                }
                else
                {
                    Debug.LogError("单声道 WAV 保存失败，回退为原始文件上传");
                    tempWavPath = null;
                }
            }

            // ⑤ 重新构造远端 URI（固定使用 .wav 扩展名）
            var localFileName = $"提取音乐{System.DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{AlbumUtils.Inst.DayUploadIndex.ToString().PadLeft(2, '0')}";
            uri = $"{_remotePath}{AccountDataManager.Inst.Uid}/{localFileName}.wav";
        }

        // ⑥ 执行上传
        LoggerUtils.Log("开始上传:" + authData.localUrl + "  to:" + uri);
        CosXmlUploadManager.UploadFile(uri, authData.localUrl, (url, err) =>
        {
            LoggerUtils.Log("上传结束:" + url + "  err:" + err);

            // ⑦ 删除临时 WAV 文件（无论成功失败）
            if (!string.IsNullOrEmpty(tempWavPath) && File.Exists(tempWavPath))
            {
                try
                {
                    File.Delete(tempWavPath);
                    Debug.Log("临时 WAV 文件已删除: " + tempWavPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("删除临时 WAV 文件失败: " + e.Message);
                }
            }

            UploadImgCallback(url, err);
        });
    }

    /// <summary>
    /// 将多声道 AudioClip 降混为单声道。若已是单声道则直接返回原始对象。
    /// 降混算法：对每帧的所有声道取算术平均值。
    /// </summary>
    /// <param name="source">原始 AudioClip（任意声道数）</param>
    /// <returns>单声道 AudioClip</returns>
    private static AudioClip ConvertToMono(AudioClip source)
    {
        if (source.channels == 1)
            return source;

        int channels = source.channels;
        // sampleCount 是每声道的帧数；GetData 返回的数组长度为 sampleCount * channels
        int sampleCount = source.samples;
        float[] sourceData = new float[sampleCount * channels];
        source.GetData(sourceData, 0);

        // 对每帧求所有声道的平均值，得到单声道数据
        float[] monoData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float sum = 0f;

            for (int c = 0; c < channels; c++)
            {
                sum += sourceData[i * channels + c];
            }

            monoData[i] = sum / channels;
        }

        AudioClip mono = AudioClip.Create("MonoUpload", sampleCount, 1, source.frequency, false);
        mono.SetData(monoData, 0);
        return mono;
    }

    /// <summary>
    /// 将 AudioClip 以 PCM 16-bit 小端序 WAV 格式写入指定文件路径。
    /// </summary>
    /// <param name="clip">要保存的 AudioClip</param>
    /// <param name="filePath">目标文件完整路径</param>
    /// <returns>保存成功返回 true，否则返回 false</returns>
    private static bool SaveWavPcm(AudioClip clip, string filePath)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                WriteWavHeader(fs, clip.channels, clip.frequency, clip.samples);

                // 将 float 样本转换为 PCM 16-bit 小端序字节数组
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                byte[] bytes = new byte[samples.Length * 2];

                for (int i = 0; i < samples.Length; i++)
                {
                    // 限幅到 [-1, 1]，再缩放到 int16 范围
                    short s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                    bytes[i * 2] = (byte)(s & 0xff);
                    bytes[i * 2 + 1] = (byte)((s >> 8) & 0xff);
                }

                fs.Write(bytes, 0, bytes.Length);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("SaveWavPcm 写入失败: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// 向 Stream 写入标准 RIFF/WAV 文件头（fmt chunk + data chunk 头）。
    /// 固定采用 PCM 格式（AudioFormat=1）、16-bit 位深。
    /// </summary>
    /// <param name="stream">目标输出流（不关闭）</param>
    /// <param name="channels">声道数</param>
    /// <param name="sampleRate">采样率（Hz）</param>
    /// <param name="sampleCount">每声道的采样帧数</param>
    private static void WriteWavHeader(Stream stream, int channels, int sampleRate, int sampleCount)
    {
        // PCM 16-bit：每采样帧占 channels * 2 字节
        int dataSize = sampleCount * channels * 2;
        int fileSize = 36 + dataSize;

        using (var bw = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            // RIFF chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(fileSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk（16 字节）
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                                    // chunk 数据长度
            bw.Write((ushort)1);                             // AudioFormat: 1 = PCM
            bw.Write((ushort)channels);                      // 声道数
            bw.Write(sampleRate);                            // 采样率
            bw.Write(sampleRate * channels * 2);             // ByteRate = 采样率 × 声道数 × 字节深度
            bw.Write((ushort)(channels * 2));                // BlockAlign = 声道数 × 字节深度
            bw.Write((ushort)16);                            // BitsPerSample

            // data chunk 头
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);
        }
    }

    /// <summary>
    /// 上传已有本地文件（如录制好的 WAV），直接按原始路径上传，不做声道转换。
    /// 供 CabinToneNetManager.UploadVoice 等外部调用方使用。
    /// </summary>
    /// <param name="authData">包含本地文件路径的上传数据</param>
    /// <param name="onSuccess">上传成功回调，参数为远端 URL</param>
    /// <param name="onFail">上传失败回调，参数为错误信息</param>
    public void UploadCustomFile(AlbumResData authData, Action<string> onSuccess, Action<string> onFail)
    {
        OnSuccess = onSuccess;
        OnFail = onFail;

        // 使用原始文件名构造远端 URI
        var uri = $"{_remotePath}{AccountDataManager.Inst.Uid}/{Path.GetFileName(authData.localUrl)}";

        LoggerUtils.Log("开始上传:" + authData.localUrl + "  to:" + uri);
        CosXmlUploadManager.UploadFile(uri, authData.localUrl, (url, err) =>
        {
            LoggerUtils.Log("上传结束:" + url + "  err:" + err);
            UploadImgCallback(url, err);
        });
    }





    private void UploadImgCallback(string url, string err)
    {
        if (!string.IsNullOrEmpty(err))
        {
            LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
            OnFail.Invoke(err);
        }
        else
        {
            if (_albumParams.albumType == 1)
            {
                var req = new Dictionary<string, string>() {
                    { "url", url }
                };
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp =>
                    {
                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                        {
                            OnSuccess?.Invoke(url);
                        }
                        else
                        {
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, (errRsp) =>
                    {
                        //兼容地图发布501审核不通过提示
                        if (errRsp != null && errRsp.result == 501)
                        {
                            LoggerUtils.LogError($"AuditImage Fail!!! Err : {errRsp.rmsg}");
                            OnFail.Invoke(errRsp.rmsg);
                        }
                        else
                        {
                            LoggerUtils.LogError($"AuditImage Fail!!! Err : {err}");
                            OnFail.Invoke("上传照片失败，请重试");
                        }
                    });
            }
            else
            {
                OnSuccess?.Invoke(url);
            }
        }
    }

}


