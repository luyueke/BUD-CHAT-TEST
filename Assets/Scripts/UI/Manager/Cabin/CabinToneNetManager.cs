using Game.CommunityGame;
using Game.Store;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 养成舱音色网络管理器
/// </summary>
public class CabinToneNetManager : GlobalInstance<CabinToneNetManager>
{
    CabinTonePublishData _cabinTonePublishData; //角色音色发布数据(包含角色信息和音色信息)

    Dictionary<string, CabinToneInfo> _cabinToneInfoDict = new(); //角色音色信息字典(包含角色信息和音色信息)


    #region 缓存管理

    /// <summary>
    /// 将音色信息写入本地缓存字典（供 CabinNetManager.LoadPgcData 调用）
    /// </summary>
    public void CacheToneInfo(CabinToneInfo toneInfo)
    {
        if (toneInfo == null)
            return;

        if (!_cabinToneInfoDict.ContainsKey(toneInfo.id))
        {
            _cabinToneInfoDict.Add(toneInfo.id, toneInfo);
        }
    }

    // 按 toneId 从本地缓存字典快速取音色信息，不存在时返回 null
    public CabinToneInfo GetToneInfo(string toneId)
    {
        if (string.IsNullOrEmpty(toneId))
        {
            return null;
        }
        _cabinToneInfoDict.TryGetValue(toneId, out var toneInfo);
        return toneInfo;
    }

    // 从缓存字典过滤出所有 PGC 音色（isPgc == 1）并以列表形式返回
    public List<CabinToneInfo> GetPgcToneInfo()
    {
        List<CabinToneInfo> list = new List<CabinToneInfo>();
        foreach (var item in _cabinToneInfoDict.Values)
        {
            if (item.IsPgc())
            {
                list.Add(item);
            }
        }
        return list;
    }

    // 返回音色发布列表（由 GetCabinCharacterTonePublishList 请求后填充）
    public List<CabinCharacterTonePublishSubData> GetCabinCharacterTonePublishData()
    {
        return _cabinTonePublishData?.list;
    }

    // isEnd == 1 表示已无更多分页数据，供调用方判断是否继续加载
    public bool GetCabinCharacterTonePublishIsEnd()
    {
        return _cabinTonePublishData?.isEnd == 1;
    }

    // 清空分页缓存数据，下次调用 GetCabinCharacterTonePublishList 将从第一页重新加载
    public void ResetCabinCharacterTonePublishData()
    {
        _cabinTonePublishData = null;
    }

    #endregion

    #region 角色音色相关

    /// <summary>
    /// 设置角色音色信息
    /// </summary>
    public void SetCabinToneInfo(int nSetType, CabinToneInfo toneInfo, Action<bool> callback = null)
    {
        var req = new CabinCharacterToneInfo()
        {
            setType = nSetType, //1 创建草稿2 编辑草稿3 复制草稿4 发布草稿5 更新发布 7 删除
            characterToneInfo = toneInfo
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterToneSet, HttpMethod.POST,
          JsonConvert.SerializeObject(req),
          rspStr =>
          {
              LoggerUtils.LogError("设置角色音色成功:");
              callback?.Invoke(true);
              if (nSetType == (int)SetType.Publish)
              {
                  MessageHelper.Broadcast<string>(MessageName.OnCreatToneInfo, string.Empty);
              }
          }, errRspStr =>
          {
              LoggerUtils.LogError("设置角色音色失败:" + errRspStr);
              callback?.Invoke(false);
          });
    }

    /// <summary>
    /// 角色音色详情
    /// </summary>
    public void GetCabinToneInfo(string toneId, Action<bool> callback = null)
    {
        var req = new JObject()
        {
            ["id"] = toneId,
        };
        NetworkManager.Inst.SendHttpRequest<CabinCharacterToneDetailData>(HttpUrlDefine.CabinCharacterToneInfo, HttpMethod.GET,
        JsonConvert.SerializeObject(req),
        rsp =>
        {
            if (rsp == null)
            {
                callback?.Invoke(false);
                return;
            }
            CabinToneInfo tone = rsp.characterToneInfo;
            if (tone == null)
            {
                callback?.Invoke(false);
                return;
            }

            _cabinToneInfoDict[tone.id] = tone;
            LoggerUtils.LogError("1获取角色音色详情成功:" + tone.id);
            LoggerUtils.LogError("2获取角色音色详情成功:" + JsonConvert.SerializeObject(tone));
            callback?.Invoke(true);
        }, errRsp =>
        {
            LoggerUtils.LogError("获取角色音色详情失败:" + errRsp.rmsg);
            callback?.Invoke(false);
        });
    }

    /// <summary>
    /// 获取角色音色发布列表
    /// </summary>
    public void GetCabinTonePublishList(Action<bool> callback = null)
    {
        var req = new JObject()
        {
            ["uid"] = AccountDataManager.Inst.Uid,
            // 传入上次响应的 cookie 实现分页加载，首次或重置后传空字符串
            ["cookie"] = _cabinTonePublishData?.cookie ?? "",
        };
        NetworkManager.Inst.SendHttpRequest<CabinTonePublishData>(HttpUrlDefine.CabinCharacterTonePublishList, HttpMethod.GET,
        JsonConvert.SerializeObject(req),
        rsp =>
        {
            if (rsp == null)
            {
                callback?.Invoke(false);
                return;
            }
            _cabinTonePublishData = rsp;
            LoggerUtils.Log("2获取角色音色发布列表成功:" + JsonConvert.SerializeObject(_cabinTonePublishData));
            callback?.Invoke(true);
        }, errRsp =>
        {
            LoggerUtils.LogError("获取角色音色发布列表失败:" + errRsp.rmsg);
            callback?.Invoke(false);
        });
    }

    #endregion
    #region 语音相关

    private string currentDeviceName; // 当前使用的麦克风设备名称
    private AudioClip recordingClip;   // 录制中的 AudioClip
    private Action<RecordVoiceResult> _recordVoiceOnResult;
    private const int RecordSampleRate = 16000;
    private const int RecordMaxSeconds = 210; // 最长 210 秒
    public string recordVoiceFilePath = "";

    /// <summary>
    /// 开始录制语音，录制结果在 StopRecordVoice 后通过 onSuccess 回调返回本地文件路径，供 UploadVoice 使用。
    /// </summary>
    public void BeginRecordVoice(Action<RecordVoiceResult> onResult)
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("没有找到可用的麦克风设备");
            onResult?.Invoke(RecordVoiceResult.Fail);
            return;
        }
        if (recordingClip != null)
        {
            Debug.LogError("请先停止当前录制");
            onResult?.Invoke(RecordVoiceResult.Fail);
            return;
        }
        _recordVoiceOnResult = onResult;
        recordVoiceFilePath = "";
        currentDeviceName = Microphone.devices[0];
        recordingClip = Microphone.Start(currentDeviceName, false, RecordMaxSeconds, RecordSampleRate);
    }

    /// <summary>
    /// 停止录制并将语音保存到本地，成功后通过 BeginRecordVoice 的 onSuccess 回调返回本地文件路径。
    /// </summary>
    public void StopRecordVoice()
    {
        if (recordingClip == null || string.IsNullOrEmpty(currentDeviceName))
        {
            Debug.LogError("未在录制中");
            _recordVoiceOnResult?.Invoke(RecordVoiceResult.Fail);
            _recordVoiceOnResult = null;
            return;
        }
        int lastSample = Microphone.GetPosition(currentDeviceName);
        Microphone.End(currentDeviceName);

        if (lastSample <= 0)
        {
            recordingClip = null;
            Debug.LogError("录制时长为空");
            _recordVoiceOnResult?.Invoke(RecordVoiceResult.Fail);
            _recordVoiceOnResult = null;
            return;
        }

        AudioClip trimmed = TrimAudioClip(recordingClip, lastSample);
        recordingClip = null;
        if (trimmed == null)
        {
            Debug.LogError("录制时长需在 10~210 秒之间");
            _recordVoiceOnResult?.Invoke(RecordVoiceResult.TimeOut);
            return;
        }
        string dir = Path.Combine(Application.persistentDataPath, "VoiceRecord");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        string fileName = $"voice_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        string filePath = Path.Combine(dir, fileName);
        bool saved = SaveWav(trimmed, filePath);
        UnityEngine.Object.Destroy(trimmed);
        if (saved)
        {
            recordVoiceFilePath = filePath;
            _recordVoiceOnResult?.Invoke(RecordVoiceResult.Success);
        }
        else
        {
            Debug.LogError("保存录制文件失败");
            _recordVoiceOnResult?.Invoke(RecordVoiceResult.Fail);
        }
        _recordVoiceOnResult = null;
    }

    /// <summary>
    /// 取消当前录制并丢弃结果，面板退出时调用以释放麦克风资源、清空 recordingClip。
    /// 若当前未在录制则静默返回。
    /// </summary>
    public void CancelRecordVoice()
    {
        if (recordingClip == null)
            return;

        if (!string.IsNullOrEmpty(currentDeviceName))
        {
            Microphone.End(currentDeviceName);
        }

        recordingClip = null;
        currentDeviceName = null;
        _recordVoiceOnResult = null;
    }

    // 裁剪录制的 AudioClip 到实际录制长度，录制时长不在 10~210 秒范围内时返回 null
    private static AudioClip TrimAudioClip(AudioClip source, int positionSamples)
    {
        float durationSec = positionSamples / (float)source.frequency;
        if (durationSec < 10f || durationSec > 210f)
            return null;
        int channels = source.channels;
        // positionSamples 是单声道帧数，乘以声道数才是实际 float 数据量
        int totalSamples = positionSamples * channels;
        if (totalSamples <= 0 || totalSamples > source.samples)
            totalSamples = source.samples;
        float[] data = new float[totalSamples];
        source.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create("TrimmedVoice", totalSamples, channels, source.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    // 将 AudioClip 以 PCM 16-bit 小端序 WAV 格式保存到指定路径
    private static bool SaveWav(AudioClip clip, string filePath)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                WriteWavHeader(fs, clip.channels, clip.frequency, clip.samples);
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                // PCM 16-bit 小端序：每个 float 样本转成 2 字节有符号整数
                byte[] bytes = new byte[samples.Length * 2];
                for (int i = 0; i < samples.Length; i++)
                {
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
            Debug.LogError("SaveWav failed: " + e.Message);
            return false;
        }
    }

    // 按 WAV 规范写入 RIFF 文件头（含 fmt 和 data chunk 头）
    private static void WriteWavHeader(Stream stream, int channels, int sampleRate, int sampleCount)
    {
        int dataSize = sampleCount * channels * 2;
        int fileSize = 36 + dataSize;
        using (var bw = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(fileSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((ushort)1);
            bw.Write((ushort)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((ushort)(channels * 2));
            bw.Write((ushort)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);
        }
    }

    /// <summary>
    /// 加载当前录制语音（recordVoiceFilePath），通过回调返回 AudioClip。
    /// 录制结果由 StopRecordVoice 成功时写入 recordVoiceFilePath。
    /// </summary>
    public void LoadRecordVoiceClip(Action<AudioClip> onLoaded, Action<string> onFail = null)
    {
        LoadRecordVoiceClip(recordVoiceFilePath, onLoaded, onFail);
    }

    /// <summary>
    /// 加载指定路径的录制语音文件，通过回调返回 AudioClip（本地 WAV 为异步加载）。
    /// </summary>
    /// <param name="filePath">本地 WAV 文件路径，通常为 recordVoiceFilePath</param>
    /// <param name="onLoaded">加载成功，返回 AudioClip</param>
    /// <param name="onFail">加载失败时的错误信息</param>
    public void LoadRecordVoiceClip(string filePath, Action<AudioClip> onLoaded, Action<string> onFail = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            onFail?.Invoke(string.IsNullOrEmpty(filePath) ? "请先录制语音" : "文件不存在");
            return;
        }
        CoroutineManager.Inst.StartCoroutine(LoadRecordVoiceClipCoroutine(filePath, onLoaded, onFail));
    }

    // 协程：通过 UnityWebRequest 异步加载本地 WAV 文件并解析为 AudioClip
    private static IEnumerator LoadRecordVoiceClipCoroutine(string filePath, Action<AudioClip> onLoaded, Action<string> onFail)
    {
        string uri = "file://" + filePath;
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                onFail?.Invoke(www.error ?? "加载音频失败");
                yield break;
            }
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip != null)
                onLoaded?.Invoke(clip);
            else
                onFail?.Invoke("无法解析音频");
        }
    }

    /// <summary>
    /// 上传语音(提供外部录制完的音频上传)
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFail"></param>
    public void UploadVoice(string filePath, Action<string> onSuccess, Action<string> onFail = null)
    {
        AlbumResData authData = new AlbumResData()
        {
            localUrl = filePath,
            mediaType = 0,
        };

        (new AlbumProcess()).UploadCustomFile(authData, onSuccess, onFail);
    }
    #endregion
    #region 新接口

    /// <summary>
    /// 精选section详情V2 GET /recommend/ugc/sectionInfoV2
    /// </summary>
    /// <param name="sectionId">分区 ID</param>
    /// <param name="cookie">分页游标，首次传空字符串</param>
    /// <param name="count">每页数量</param>
    /// <param name="tagIds">标签 ID 筛选字符串，多个 id 以英文逗号分隔，为 null 或空时不附加 tagIds 参数</param>
    /// <param name="callback">回调：(成功标志, 响应数据)</param>
    public void GetSectionInfoV2(string sectionId, string cookie, int count,
        string tagIds = null,
        Action<bool, UgcAvatarPageUseData> callback = null)
    {
        var req = new JObject()
        {
            ["cookie"] = cookie ?? "",
            ["sectionId"] = sectionId,
            ["currencyType"] = 0,
        };

        if (!string.IsNullOrEmpty(tagIds))
        {
            req["tagIds"] = tagIds;
        }

        NetworkManager.Inst.SendHttpRequest<UgcAvatarPageUseData>(HttpUrlDefine.sectionInfoV2, HttpMethod.GET,
        JsonConvert.SerializeObject(req),
        rsp =>
        {
            if (rsp == null)
            {
                callback?.Invoke(false, null);
                return;
            }
            callback?.Invoke(true, rsp);
        }, errRsp =>
        {
            LoggerUtils.LogError("获取精选sectionInfoV2失败:" + errRsp.rmsg);
            callback?.Invoke(false, null);
        });
    }

    /// <summary>
    /// 获取AI伴侣音色标签列表 GET /recommend/getCharacterToneTags
    /// </summary>
    public void GetCabinToneTags(Action<bool, CabinToneTagsRsp> callback = null)
    {
        NetworkManager.Inst.SendHttpRequest<CabinToneTagsRsp>(HttpUrlDefine.GetCharacterToneTags, HttpMethod.GET,
        string.Empty,
        rsp =>
        {
            if (rsp == null)
            {
                callback?.Invoke(false, null);
                return;
            }
            callback?.Invoke(true, rsp);
        }, errRsp =>
        {
            LoggerUtils.LogError("获取AI伴侣音色标签失败:" + errRsp.rmsg);
            callback?.Invoke(false, null);
        });
    }

    /// <summary>
    /// 克隆音色
    /// </summary>
    /// <param name="audioUrl">音频文件URL</param>
    /// <param name="languageType">语言类型：0=中文，1=英文，2=日文</param>
    /// <param name="usedVoiceIds">已克隆的其他语言 voiceId，英文逗号分隔</param>
    /// <param name="callback"></param>
    public void CloneCabinCharacterTone(string audioUrl, int languageType = 0, string usedVoiceIds = "", Action<bool, string> callback = null)
    {
        var req = new JObject()
        {
            ["audioUrl"] = audioUrl,
            ["languageType"] = languageType,
        };

        if (!string.IsNullOrEmpty(usedVoiceIds))
        {
            req["usedVoiceIds"] = usedVoiceIds;
        }

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterToneClone, HttpMethod.GET,
          JsonConvert.SerializeObject(req),
          rspStr =>
          {
              var rsp = JsonConvert.DeserializeObject<JObject>(rspStr);
              var voiceId = rsp["voiceId"].ToObject<string>(); //音色id
              LoggerUtils.LogError("克隆音色成功:" + voiceId);
              callback?.Invoke(true, voiceId);
          }, errRspStr =>
          {
              LoggerUtils.LogError("克隆音色失败:" + errRspStr);
              callback?.Invoke(false, null);
          }, timeOut: 60f);
    }

    /// <summary>
    /// 通过 voiceId 直接获取音色试听音频（用于音色创建流程中、tone 尚未发布时的本地预览）。
    /// </summary>
    /// <param name="voiceId">克隆后的音色 ID</param>
    /// <param name="texts">待生成语音的文本列表</param>
    /// <param name="languageType">语言类型：0=中文，1=英文，2=日文</param>
    /// <param name="callback">回调：(是否成功, 预览数据)</param>
    public void GetBatchPreviewByVoiceId(string voiceId, List<string> texts, int languageType, Action<bool, CabinDoubaoBatchPreviewData> callback = null)
    {
        if (string.IsNullOrEmpty(voiceId))
        {
            callback?.Invoke(false, null);
            return;
        }

        var req = new ReqToneBatchPreview()
        {
            voiceId = voiceId,
            texts = texts,
            languageType = languageType,
        };

        NetworkManager.Inst.SendHttpRequest<CabinDoubaoBatchPreviewData>(HttpUrlDefine.CabinCharacterToneBatchPreview, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rsp =>
            {
                if (rsp == null)
                {
                    callback?.Invoke(false, null);
                    return;
                }
                callback?.Invoke(true, rsp);
            }, errRsp =>
            {
                LoggerUtils.LogError("获取音色预览失败:" + errRsp);
                callback?.Invoke(false, null);
            }, timeOut: 60f);
    }

    /// <summary>
    /// 搜索AI伴侣音色 GET /search/characterTone
    /// </summary>
    public void SearchCabinTone(string keyword, string tags, string cookie, int count,
        Action<bool, SearchCabinToneRsp> callback = null)
    {
        var req = new JObject()
        {
            ["searchWord"] = keyword ?? "",
            ["cookie"] = cookie ?? "",
            ["count"] = count,
        };
        if (!string.IsNullOrEmpty(tags))
        {
            req["tagIds"] = tags;
        }

        // ── 测试假数据（服务端未实现时使用）──────────────────────
        // {
        //     var mockRsp = new SearchCabinToneRsp
        //     {
        //         cookie = "",
        //         IsEnd = 1,
        //         list = new List<CabinCharacterTonePublishSubData>
        //         {
        //             MockTone("mock_pgc_001", "清亮少女",  price: 0,   consumed: 1),
        //             MockTone("mock_pgc_002", "温柔御姐",  price: 100, consumed: 0),
        //             MockTone("mock_pgc_003", "活泼萝莉",  price: 0,   consumed: 1),
        //             MockTone("mock_pgc_004", "冷静学姐",  price: 200, consumed: 0),
        //             MockTone("mock_pgc_005", "元气邻家",  price: 0,   consumed: 1),
        //             MockTone("mock_pgc_006", "知性白领",  price: 150, consumed: 0),
        //             MockTone("mock_pgc_007", "软萌新人",  price: 0,   consumed: 1),
        //             MockTone("mock_pgc_008", "傲娇公主",  price: 80,  consumed: 0),
        //             MockTone("mock_pgc_009", "温婉仙子",  price: 0,   consumed: 1),
        //             MockTone("mock_pgc_010", "犀利女王",  price: 120, consumed: 0),
        //             MockTone("mock_pgc_011", "甜美邻居",  price: 0,   consumed: 1),
        //             MockTone("mock_pgc_012", "睿智导师",  price: 60,  consumed: 0),
        //         },
        //     };
        //     callback?.Invoke(true, mockRsp);
        //     return;
        // }
        // ── END 测试假数据 ─────────────────────────────────────

        NetworkManager.Inst.SendHttpRequest<SearchCabinToneRsp>(HttpUrlDefine.SearchCharacterTone, HttpMethod.GET,
        JsonConvert.SerializeObject(req),
        rsp =>
        {
            if (rsp == null)
            {
                callback?.Invoke(false, null);
                return;
            }
            callback?.Invoke(true, rsp);
        }, errRsp =>
        {
            LoggerUtils.LogError("搜索AI伴侣音色失败:" + errRsp.rmsg);
            callback?.Invoke(false, null);
        });
    }

    private static CabinCharacterTonePublishSubData MockTone(string id, string name, int price, int consumed)
    {
        return new CabinCharacterTonePublishSubData
        {
            characterToneInfo = new CabinToneInfo
            {
                id = id,
                name = name,
                isPgc = 1,
                cover = "",
                paymentInfo = new GameData.Base.PaymentInfo
                {
                    price = price,
                    currencyType = CurrencyType.PinkCoin,
                },
                languageList = new List<ToneLanguageData>
                {
                    new ToneLanguageData { type = 0, voiceId = id, voiceUrl = "" },
                },
            },
            interactInfo = new GameData.Base.BaseInteractInfo
            {
                consumed = consumed,
            },
        };
    }

    #endregion
}
