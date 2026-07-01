using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 养成舱聊天管理器
/// </summary>
public class CabinChatManager : GlobalInstance<CabinChatManager>
{
    /// <summary>
    /// 创建Bot流式聊天（/boxchat/createBot/stream）
    /// </summary>
    /// <param name="req">请求数据，包含 messages 和 sessionId</param>
    /// <param name="onReceive">每帧回调，参数为当前帧数据和是否结束</param>
    /// <param name="onClose">流关闭或出错时回调</param>
    public void CreateBotStream(createBotStreamReq req, Action<CabinChatCreateBotData, bool> onReceive, Action onClose = null)
    {
        NetworkManager.Inst.SendHttpRequestOnStream(
            HttpUrlDefine.createBotStream,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            (content) =>
            {
                if (string.IsNullOrEmpty(content))
                {
                    onReceive?.Invoke(null, false);
                    return;
                }
                if (content == "[DONE]")
                {
                    onReceive?.Invoke(null, true);
                    return;
                }
                // UnityEngine.Debug.LogError(content);
                CabinChatCreateBotData data = null;
                if (!string.IsNullOrEmpty(content))
                {
                    try
                    {
                        data = JsonConvert.DeserializeObject<CabinChatCreateBotData>(content);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("createBotStream sse error:" + ex.Message);
                        return;
                    }
                }
                onReceive?.Invoke(data, false);
                // onReceive?.Invoke(data, true);
            },
            () => onClose?.Invoke()
        );
    }

    /// <summary>
    /// Box聊天流式请求（/boxchat/stream）
    /// </summary>
    /// <param name="req">请求数据，包含 messages 和 characterId</param>
    /// <param name="onReceive">每帧回调，参数为当前帧数据和是否结束</param>
    /// <param name="onClose">流关闭或出错时回调</param>
    public void BoxchatStream(boxchatStreamReq req, Action<CabinChatCreateBotData, bool> onReceive, Action onClose = null)
    {
        NetworkManager.Inst.SendHttpRequestOnStream(
            HttpUrlDefine.boxchatStream,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            (content) =>
            {
                if (string.IsNullOrEmpty(content))
                {
                    onReceive?.Invoke(null, false);
                    return;
                }

                if (content == "[DONE]")
                {
                    onReceive?.Invoke(null, true);
                    return;
                }


                CabinChatCreateBotData data = JsonConvert.DeserializeObject<CabinChatCreateBotData>(content);
                onReceive?.Invoke(data, false);
            },
            () => onClose?.Invoke()
        );
    }

    /// <summary>
    /// 获取聊天会话列表（/box/sessionList）
    /// </summary>
    /// <param name="onSuccess">成功回调，返回会话列表数据</param>
    /// <param name="onFail">失败回调，返回错误信息</param>
    public void GetBoxSessionList(Action<CabinChatSessionList> onSuccess, Action<string> onFail = null)
    {
        GetBoxSessionList(0, onSuccess, onFail);
    }

    /// <summary>
    /// 获取聊天会话列表（/box/sessionList）
    /// </summary>
    /// <param name="type">0=文字聊天</param>
    /// <param name="onSuccess">成功回调，返回会话列表数据</param>
    /// <param name="onFail">失败回调，返回错误信息</param>
    public void GetBoxSessionList(int type, Action<CabinChatSessionList> onSuccess, Action<string> onFail = null)
    {
        var paramStr = JsonConvert.SerializeObject(new { type });
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.boxSessionList,
            HttpMethod.GET,
            paramStr,
            (content) =>
            {
                Debug.Log("获取聊天会话列表（GetBoxSessionList type=" + type + " " + content);
                var data = JsonConvert.DeserializeObject<CabinChatSessionList>(content);
                onSuccess?.Invoke(data);
            },
            (error) => onFail?.Invoke(error)
        );
    }

    /// <summary>
    /// 获取语音通话历史记录（/box/audioHistory）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="cookid">设备cookid</param>
    /// <param name="onSuccess">成功回调，返回历史记录数据</param>
    /// <param name="onFail">失败回调，返回错误信息</param>
    public void GetBoxAudioHistory(string sessionId, Action<CabinAudioHistoryData> onSuccess, Action<string> onFail = null)
    {
        var paramStr = JsonConvert.SerializeObject(new { sessionId });
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.boxAudioHistory,
            HttpMethod.GET,
            paramStr,
            (content) =>
            {
                Debug.Log("获取语音通话历史记录（GetBoxAudioHistory=" + content);
                var data = JsonConvert.DeserializeObject<CabinAudioHistoryData>(content);
                onSuccess?.Invoke(data);
            },
            (error) => onFail?.Invoke(error)
        );
    }

    /// <summary>
    /// 获取文字聊天历史记录（/box/textHistory）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="onSuccess">成功回调，返回历史记录数据</param>
    /// <param name="onFail">失败回调，返回错误信息</param>
    public void GetBoxTextHistory(string sessionId, Action<CabinChatTextHistoryData> onSuccess, Action<string> onFail = null)
    {
        var paramStr = JsonConvert.SerializeObject(new { sessionId });
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.boxTextHistory,
            HttpMethod.GET,
            paramStr,
            (content) =>
            {
                Debug.Log("获取文字聊天历史记录（GetBoxTextHistory=" + content);
                var data = JsonConvert.DeserializeObject<CabinChatTextHistoryData>(content);
                onSuccess?.Invoke(data);
            },
            (error) => onFail?.Invoke(error)
        );
    }




    /// <summary>
    /// 匹配角色标签（/boxchat/createBot/matchTags）
    /// </summary>
    /// <param name="botProfileJson">botProfile 序列化后的 JSON 字符串</param>
    /// <param name="onSuccess">成功回调，返回标签列表</param>
    /// <param name="onFail">失败回调</param>
    public void CreateBotTags(string botProfileJson, Action<List<BotMatchTags>> onSuccess, Action<string> onFail = null)
    {
        var paramStr = JsonConvert.SerializeObject(new { botProfile = botProfileJson });
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.createBotTags,
            HttpMethod.POST,
            paramStr,
            (content) =>
            {
                var resp = JsonConvert.DeserializeObject<CreateBotTagsResponse>(content);
                onSuccess?.Invoke(resp?.botMatchTags);
            },
            (error) => onFail?.Invoke(error)
        );
    }

    /// <summary>
    /// 聊天内容tts（/box/textAudio)
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="onSuccess">成功回调，返回历史记录数据</param>
    /// <param name="onFail">失败回调，返回错误信息</param>
    public void GetBoxTextAudio(string sessionId,string msgId,string content,Action<CabinChatTextHistory> onSuccess,Action<string> onFail)
    {
        var paramStr = JsonConvert.SerializeObject(new { sessionId, msgId , content });
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.boxTextAudio,
            HttpMethod.POST,
            paramStr,
            (content) =>
            {
                var data = JsonConvert.DeserializeObject<CabinChatTextHistory>(content);
                onSuccess?.Invoke(data);
            },
            (error) => onFail?.Invoke(error)
        );
    }

    /// <summary>
    /// 随机生成角色扮演场景（/chat/aiCharacter/scene/random）
    /// </summary>
    public void GetAiCharacterSceneRandom(string deviceId, Action<AiCharacterSceneRandomResponse> onSuccess, Action<string> onFail = null)
    {
        var paramStr = JsonConvert.SerializeObject(new { deviceId });
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.aiCharacterSceneRandom,
            HttpMethod.POST,
            paramStr,
            (content) =>
            {
                var resp = JsonConvert.DeserializeObject<AiCharacterSceneRandomResponse>(content);
                Debug.LogError(content);
                onSuccess?.Invoke(resp);
            },
            (error) => onFail?.Invoke(error)
        );
    }
}
