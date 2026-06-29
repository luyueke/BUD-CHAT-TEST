using Game.Avatar;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 养成舱网络管理器
/// </summary>
public class CabinNetManager : GlobalInstance<CabinNetManager>
{
    private const string PgcToneConfigPath = "Assets/Arts/Config/CabinToneConfig/pgcToneConfig.json";

    public bool IsPreviewShowRoleWithCabin = false; //是否预览显示 带养成舱角色/不带BOX角色

    public bool isVip
    {
        get
        {
            return VipDataManager.Inst.isVip;
        }
    }


    public bool isInit = false;


    public CabinNetManager()
    {
    }

    // 初始化：读取本地预览配置，拉取草稿列表；无角色时自动用当前形象创建默认草稿
    public void Init(Action cb = null)
    {
        LoadPgcData();
        IsPreviewShowRoleWithCabin = PlayerPrefs.GetInt("IsPreviewShowRoleWithCabin", 0) == 1;
        //GetCabinCharacterDraftList((isSuccess) =>
        //{
        //    if (isSuccess)
        //    {
        //        if (_CabinCharacterDic.Count == 0)
        //        {
        //            //这里后续要去掉 加新手引导流程
        //            //没有角色 默认使用当前设置的形象
        //            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo as CharacterData;
        //            CabinCharacterUgcInfo cabinCharacterUgcInfo = new CabinCharacterUgcInfo();
        //            SkinPackInfo skinPack = new SkinPackInfo();
        //            skinPack.avatarJson = CharacterData.SerializeObject(saveCharacterData);
        //            skinPack.isDefault = 1;
        //            cabinCharacterUgcInfo.skinPack = new() { skinPack };
        //            cabinCharacterUgcInfo.pendingEmote = new()
        //            {
        //                emoteList = new(),
        //                loopEmoteList = new(),
        //            };
        //            cabinCharacterUgcInfo.activation = new();
        //            cabinCharacterUgcInfo.voiceCommands = new();
        //            var list = CabinNetManager.Inst.GetPgcToneInfo();
        //            cabinCharacterUgcInfo.toneId = list != null && list.Count > 0 ? list[0].id : string.Empty;
        //            SetCabinCharacterInfo(cabinCharacterUgcInfo, SetType.Edit, (isSuccess) =>
        //            {
        //                if (isSuccess)
        //                {
        //                    LoggerUtils.Log("设置默认形象成功");
        //                    // GetNetCabinCharacterPublishList(); //暂时屏蔽
        //                }
        //                isInit = true;
        //                cb?.Invoke();
        //            });
        //        }
        //        else
        //        {
        //            string cabinCharacterId = _CabinCharacterDic.Keys.First();
        //            GetCabinCharacterInfo(cabinCharacterId, (isSuccess) =>
        //            {
        //                isInit = true;
        //                cb?.Invoke();
        //            });
        //            GetCabinCharacterTonePublishList();
        //        }
        //    }
        //});
        //GetNetCabinCharacterPublishList(CabinPurchasedType.All);
    }


    // 从本地 JSON 配置文件解析 PGC 音色列表，填充缓存字典供运行时快速查询
    private void LoadPgcData()
    {
        var wrapper = Loader.Load<TextAsset>(PgcToneConfigPath);
        if (wrapper == null)
        {
            Debug.LogError("[CabinUgcAnimPgcToneInfoPanel] 加载 PGC 音色配置失败: " + PgcToneConfigPath);
            isInit = true;
            return;
        }

        var textAsset = wrapper.RetainAsset();
        if (textAsset == null)
        {
            Debug.LogError("[CabinUgcAnimPgcToneInfoPanel] PGC 音色配置文件为空");
            isInit = true;
            return;
        }

        var entries = JsonConvert.DeserializeObject<List<PgcToneConfigEntry>>(textAsset.text);

        if (entries != null)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.ugcId)) continue;

                var musicInfo = new CabinToneInfo
                {
                    id = entry.ugcId,
                    name = entry.name,
                    isPgc = 1,
                    metaDataUrl = entry.previewUrl,
                };
                musicInfo.languageList.Add(new ToneLanguageData()
                {
                    type = 0,
                    voiceId = entry.voiceId,
                    voiceUrl = entry.previewUrl,
                });
                CabinToneNetManager.Inst.CacheToneInfo(musicInfo);
            }
        }
    }


    /// <summary>
    /// 选择默认养成舱角色
    /// </summary>
    public void SelectDefaultCabinCharacterUgcInfo()
    {
        // if (_CabinCharacterDraftList == null)
        // {
        //     return;
        // }
        // foreach (var item in _CabinCharacterDraftList)
        // {
        //     foreach (var skinPack in item.characterInfo.skinPack)
        //     {
        //         if (skinPack.isDefault == 1)
        //         {
        //             _netCabinCharacterUgcInfo = item.characterInfo;
        //             break;
        //         }
        //     }
        // }


        // var skinPacks = _netCabinCharacterUgcInfo.skinPack;
        // foreach (var item in skinPacks)
        // {
        //     if (item.isDefault == 1)
        //     {
        //         _netCabinCharacterUgcInfo = item;
        //         break;
        //     }
        // }
        // incubationCabinPanel?.SelectCabinCharacterUgcInfo(_netCabinCharacterUgcInfo.characterInfo);
    }



    // 将指定皮肤包设为默认（针对传入的角色信息，而非当前临时角色）
    public void SetAsDefaultForCabinCharacterUgcInfo(string skinPackId, CabinCharacterBaseInfo cabinCharacterUgcInfo, Action<bool> callback = null)
    {
        bool isSuccess = false;
        for (int i = 0; i < cabinCharacterUgcInfo.skinPack.Count; i++)
        {
            if (cabinCharacterUgcInfo.skinPack[i].packId == skinPackId)
            {
                cabinCharacterUgcInfo.skinPack[i].isDefault = 1;
                isSuccess = true;
            }
            else
            {
                cabinCharacterUgcInfo.skinPack[i].isDefault = 0;
            }
        }
        if (isSuccess)
        {
            if (cabinCharacterUgcInfo is CabinCharacterUgcInfo)
            {
                SetCabinCharacterInfo((CabinCharacterUgcInfo)cabinCharacterUgcInfo, SetType.Edit, callback);
            }
            else if (cabinCharacterUgcInfo is CabinCharacterPackInfo)
            {
                SetCabinCharacterPackInfo((CabinCharacterPackInfo)cabinCharacterUgcInfo, SetType.Edit, (b, data) => { callback(b); });
            }
        }
        else
        {
            Debug.LogError("设置为默认养成舱角色失败:皮肤包不存在");
            callback?.Invoke(false);
        }
    }



    // 将新皮肤包追加到当前临时角色并同步到服务端
    public void AddSkinPack(CabinCharacterBaseInfo characterUgcInfo, CharacterData characterData, string url, Action<bool> callback = null)
    {
        if (characterUgcInfo == null)
        {
            LoggerUtils.LogError("添加皮肤包失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        characterUgcInfo.skinPack.Add(new SkinPackInfo()
        {
            cover = url,
            avatarJson = CharacterData.SerializeObject(characterData),
            isDefault = 0,
            // packId = Guid.NewGuid().ToString(),
        });
        if (characterUgcInfo is CabinCharacterUgcInfo)
        {
            SetCabinCharacterInfo((CabinCharacterUgcInfo)characterUgcInfo, SetType.Edit, callback);
        }
        else if (characterUgcInfo is CabinCharacterPackInfo)
        {
            SetCabinCharacterPackInfo((CabinCharacterPackInfo)characterUgcInfo, SetType.Edit, (b, data) => { callback(b); });
        }
    }

    /// <summary>
    /// 删除皮肤包
    /// </summary>
    /// <param name="skinPackId"></param>
    /// <param name="callback"></param> <summary>
    /// 
    /// </summary>
    /// <param name="cabinCharacterId"></param>
    /// <param name="callback"></param>
    public void DeleteSkinPack(CabinCharacterBaseInfo cabinCharacterUgcInfo, string skinPackId, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("删除皮肤包失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }

        foreach (var item in cabinCharacterUgcInfo.skinPack)
        {
            if (item.packId == skinPackId)
            {
                cabinCharacterUgcInfo.skinPack.Remove(item);
                break;
            }
        }
        if (cabinCharacterUgcInfo is CabinCharacterUgcInfo)
        {
            SetCabinCharacterInfo((CabinCharacterUgcInfo)cabinCharacterUgcInfo, SetType.Edit, callback);
        }
        else if (cabinCharacterUgcInfo is CabinCharacterPackInfo)
        {
            SetCabinCharacterPackInfo((CabinCharacterPackInfo)cabinCharacterUgcInfo, SetType.Edit, (b, data) => { callback(b); });
        }
    }


    #region 唤醒动作相关

    private bool IsEmoteDuplicate(List<pEmoteData> list, pEmoteData target, bool isPgc)
    {
        if (list == null)
            return false;

        foreach (var item in list)
        {
            if (isPgc && item.emoteId == target.emoteId)
                return true;

            if (!isPgc && item.ugcData?.id == target.ugcData?.id)
                return true;
        }
        return false;
    }

    // 仅修改本地 characterUgcInfo 内存，不发网络请求
    public void AddPendingEmoteLocal(CabinCharacterBaseInfo characterUgcInfo, GoodsData data, bool isLoop, Action<bool> callback = null)
    {
        if (characterUgcInfo == null)
        {
            callback?.Invoke(false);
            return;
        }

        if (data.Assets == null || data.Assets.Count == 0)
        {
            TipPanel.ShowToast("添加唤醒动作失败:表情数据异常");
            callback?.Invoke(false);
            return;
        }

        var isPgc = data.Assets[0].UgcInfo == null;
        pEmoteData _pEmoteData = new();
        if (isPgc)
        {
            _pEmoteData.emoteId = data.Assets[0].Id;
            _pEmoteData.ugcData = null;
        }
        else
        {
            _pEmoteData.emoteId = "";
            _pEmoteData.ugcData = JsonConvert.DeserializeObject<UgcIdleData>(JsonConvert.SerializeObject(data.Assets[0].UgcInfo.animInfo));
        }

        characterUgcInfo.pendingEmote ??= new PendingEmoteData();
        var targetList = isLoop
            ? (characterUgcInfo.pendingEmote.loopEmoteList ??= new())
            : (characterUgcInfo.pendingEmote.emoteList ??= new());

        // 检查自身列表是否已有重复
        if (IsEmoteDuplicate(targetList, _pEmoteData, isPgc))
        {
            TipPanel.ShowToast("添加重复动作");
            return;
        }

        // 根据角色类型（本体/皮肤）取对应上限，统一由 CabinConfig 配置
        int pendingEmoteMaxCount = isLoop
            ? CabinConfig.GetLoopEmoteMax(characterUgcInfo)
            : CabinConfig.GetNonLoopEmoteMax(characterUgcInfo);
        string pendingEmoteLimitMsg = isLoop
            ? $"待机主动作最多{pendingEmoteMaxCount}个"
            : $"待机表演动作最多{pendingEmoteMaxCount}个";
        if (targetList.Count >= pendingEmoteMaxCount)
        {
            TipPanel.ShowToast(pendingEmoteLimitMsg);
            return;
        }

        void CommitAdd()
        {
            targetList.Add(_pEmoteData);
            RefreshInteractContent();
            callback?.Invoke(true);
        }

        if (characterUgcInfo is CabinCharacterUgcInfo ugcInfo)
        {
            // 主角色：检查所有扩展包是否有重复
            GetExtensionPackBatchInfo(ugcInfo.extensionPackList, (packSuccess, packList) =>
            {
                if (packSuccess && packList != null)
                {
                    foreach (var pack in packList)
                    {
                        var checkList = isLoop ? pack.pendingEmote?.loopEmoteList : pack.pendingEmote?.emoteList;
                        if (IsEmoteDuplicate(checkList, _pEmoteData, isPgc))
                        {
                            TipPanel.ShowToast($"添加重复动作，皮肤包「{pack.name}」中已存在该动作");
                            return;
                        }
                    }
                }
                CommitAdd();
            });
        }
        else if (characterUgcInfo is CabinCharacterPackInfo packInfo)
        {
            // 扩展包：先检查父角色，再检查同角色的其他扩展包
            GetCabinCharacterInfo(packInfo.characterId, (charSuccess, charData) =>
            {
                if (!charSuccess || charData?.characterInfo == null)
                {
                    CommitAdd();
                    return;
                }

                var charCheckList = isLoop
                    ? charData.characterInfo.pendingEmote?.loopEmoteList
                    : charData.characterInfo.pendingEmote?.emoteList;

                if (IsEmoteDuplicate(charCheckList, _pEmoteData, isPgc))
                {
                    TipPanel.ShowToast($"添加重复动作，角色「{charData.characterInfo.name}」中已存在该动作");
                    return;
                }

                GetExtensionPackBatchInfo(charData.characterInfo.extensionPackList, (packBatchSuccess, packList) =>
                {
                    if (packBatchSuccess && packList != null)
                    {
                        foreach (var pack in packList)
                        {
                            if (pack.id == packInfo.id)
                                continue;

                            var checkList = isLoop ? pack.pendingEmote?.loopEmoteList : pack.pendingEmote?.emoteList;
                            if (IsEmoteDuplicate(checkList, _pEmoteData, isPgc))
                            {
                                TipPanel.ShowToast($"添加重复动作，皮肤包「{pack.name}」中已存在该动作");
                                return;
                            }
                        }
                    }
                    CommitAdd();
                });
            });
        }
        else
        {
            CommitAdd();
        }
    }

    // 仅从本地 characterUgcInfo 内存移除动作，不发网络请求
    public void RemovePendingEmoteLocal(CabinCharacterBaseInfo characterUgcInfo, pEmoteData emoteData, bool isLoop, Action<bool> callback = null)
    {
        if (characterUgcInfo == null)
        {
            LoggerUtils.LogError("删除唤醒动作失败:未找到对应动作");
            callback?.Invoke(false);
            return;
        }

        var emoteList = isLoop ? characterUgcInfo.pendingEmote?.loopEmoteList : characterUgcInfo.pendingEmote?.emoteList;
        if (emoteList == null)
        {
            return;
        }
        if (characterUgcInfo is CabinCharacterUgcInfo && emoteList.Contains(emoteData) && emoteList.Count == 1)
        {
            TipPanel.ShowToast("删除失败，至少需要保留一个动作");
            return;
        }
        bool removeResult = emoteList.Remove(emoteData);
        RefreshInteractContent();
        if (removeResult)
        {
            callback?.Invoke(true);
        }
        else
        {
            LoggerUtils.LogError("删除唤醒动作失败:未找到对应动作");
            callback?.Invoke(false);
        }
    }

    // 修改第 idx 个唤醒动作绑定的表情，PGC/UGC 分别填充对应字段（仅本地修改，不发网络请求）
    public void ModifyActivationEmoteLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, GoodsData data, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null || data == null || data.Assets == null || data.Assets.Count == 0)
        {
            LoggerUtils.LogError("表情数据异常");
            callback?.Invoke(false);
            return;
        }
        var isPgc = data.Assets[0].UgcInfo == null;
        if (isPgc)
        {
            cabinCharacterUgcInfo.activation[idx].ugcData = null;
            cabinCharacterUgcInfo.activation[idx].emoteId = data.Assets[0].Id;
            cabinCharacterUgcInfo.activation[idx].isPgc = 1;
        }
        else
        {
            cabinCharacterUgcInfo.activation[idx].emoteId = "";
            cabinCharacterUgcInfo.activation[idx].isPgc = 0;
            cabinCharacterUgcInfo.activation[idx].ugcData = JsonConvert.DeserializeObject<UgcIdleData>(JsonConvert.SerializeObject(data.Assets[0].UgcInfo.animInfo));
        }
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 修改唤醒动作静音（仅本地修改，不发网络请求）
    public void ModifyActivationMute(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, bool isMute, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改唤醒动作静音失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.activation[idx].isMute = isMute ? 1 : 0;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 修改唤醒动作延迟时间（仅本地修改，不发网络请求）
    public void ModifyActivationDelaySecond(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, int delaySecond, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改唤醒动作延迟时间失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.activation[idx].delaySecond = delaySecond;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 修改第 idx 个唤醒动作的语音文本 id 及音频 url（仅本地修改，不发网络请求）
    public void ModifyActivationTextId(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, string textId, string audioUrl, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改唤醒动作语音包失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.activation[idx].text = textId;
        cabinCharacterUgcInfo.activation[idx].audioUrl = audioUrl;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 更新第 idx 个口令互动的语音文本 id 及音频 url（仅本地修改，不发网络请求）
    public void ModifyVoiceCommandsTextId(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, string textId, string audioUrl, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改口令互动语音包失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.voiceCommands[idx].text = textId;
        cabinCharacterUgcInfo.voiceCommands[idx].audioUrl = audioUrl;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    /// <summary>
    /// 删除唤醒动作（仅本地修改，不发网络请求）
    /// </summary>
    public void DeleteActivation(CabinCharacterBaseInfo characterUgcInfo, int idx, Action<bool> callback = null)
    {
        if (characterUgcInfo == null)
        {
            LoggerUtils.LogError("删除唤醒动作失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }

        if (idx >= characterUgcInfo.activation.Count)
        {
            LoggerUtils.LogError("删除唤醒动作失败:索引越界");
            callback?.Invoke(false);
            return;
        }

        if (characterUgcInfo is CabinCharacterUgcInfo && characterUgcInfo.activation.Count == 1)
        {
            TipPanel.ShowToast("删除失败，至少需要保留一个动作");
            callback?.Invoke(false);
            return;
        }

        characterUgcInfo.activation.RemoveAt(idx);
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    /// <summary>
    /// 追加一条空唤醒动作（仅本地修改，不发网络请求）
    /// </summary>
    public void AddEmptyActivationLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("添加空唤醒动作失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.activation ??= new();
        cabinCharacterUgcInfo.activation.Add(new characterInteraction()
        {
            emoteId = "",
            isPgc = 0,
            isMute = 0,
            delaySecond = 0,
            text = ""
        });
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 向 characterUgcInfo 追加一条有默认配置的唤醒动作（仅本地修改，不发网络请求）
    public void AddDefActivationLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, string tokenID, Es.CabinDefActionConfig config, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("添加默认唤醒动作失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        Inst.GetCabinCharacterToneBatchPreview(tokenID, new List<string> { config.Cover }, (b, data) =>
        {
            if (!b || data == null || data.list == null || data.list.Count == 0)
            {
                callback?.Invoke(false);
                return;
            }

            var item = data.list[0];
            if (item == null)
            {
                callback?.Invoke(false);
                return;
            }

            cabinCharacterUgcInfo.activation ??= new();
            cabinCharacterUgcInfo.activation.Add(new characterInteraction()
            {
                emoteId = config.EmoUIID,
                isPgc = 1,
                isMute = 0,
                delaySecond = 0,
                text = item.text,
                audioUrl = item.url
            });
            RefreshInteractContent();
            callback?.Invoke(true);
        });
    }

    // 向 characterUgcInfo 批量追加多条有默认配置的唤醒动作（仅本地修改，不发网络请求）
    public void AddDefActivationsLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, string tokenID, List<Es.CabinDefActionConfig> configs, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("批量添加默认唤醒动作失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        if (configs == null || configs.Count == 0)
        {
            callback?.Invoke(true);
            return;
        }
        var covers = new List<string>();
        foreach (var cfg in configs)
        {
            covers.Add(cfg.Cover);
        }
        Inst.GetCabinCharacterToneBatchPreview(tokenID, covers, (b, data) =>
        {
            if (!b || data == null || data.list == null || data.list.Count == 0)
            {
                callback?.Invoke(false);
                return;
            }
            cabinCharacterUgcInfo.activation ??= new();
            for (int i = 0; i < configs.Count; i++)
            {
                if (i >= data.list.Count)
                {
                    break;
                }
                var previewItem = data.list[i];
                if (previewItem == null)
                {
                    continue;
                }
                cabinCharacterUgcInfo.activation.Add(new characterInteraction()
                {
                    emoteId = configs[i].EmoUIID,
                    isPgc = 1,
                    isMute = 0,
                    delaySecond = 0,
                    text = previewItem.text,
                    audioUrl = previewItem.url
                });
            }
            RefreshInteractContent();
            callback?.Invoke(true);
        });
    }

    #endregion
    #region 口令互动相关

    // 追加一条空口令互动（仅本地修改，不发网络请求）
    public void AddEmptyVoiceCommandsLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("添加空口令互动失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.voiceCommands ??= new();
        cabinCharacterUgcInfo.voiceCommands.Add(new voiceCommands()
        {
            command = "",
            emoteId = "",
            isPgc = 0,
            isMute = 0,
            delaySecond = 0,
            text = ""
        });
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    public void AddDefVoiceCommandsLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, string tokenID, Es.CabinDefActionConfig config, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("添加空口令互动失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }

        Inst.GetCabinCharacterToneBatchPreview(tokenID, new List<string> { config.Cover }, (b, data) =>
        {
            if (!b || data == null || data.list == null || data.list.Count == 0)
            {
                callback?.Invoke(false);
                return;
            }

            var item = data.list[0];
            if (item == null)
            {
                callback?.Invoke(false);
                return;
            }

            cabinCharacterUgcInfo.voiceCommands ??= new();
            cabinCharacterUgcInfo.voiceCommands.Add(new voiceCommands()
            {
                emoteId = config.EmoUIID,
                command = config.Command,
                isPgc = 1,
                isMute = 0,
                delaySecond = 0,
                text = item.text,
                audioUrl = item.url
            });
            RefreshInteractContent();
            callback?.Invoke(true);
        });
    }

    // 向 characterUgcInfo 批量追加多条有默认配置的口令互动（仅本地修改，不发网络请求）
    public void AddDefVoiceCommandsLocalBatch(CabinCharacterBaseInfo cabinCharacterUgcInfo, string tokenID, List<Es.CabinDefActionConfig> configs, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("批量添加默认口令互动失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        if (configs == null || configs.Count == 0)
        {
            callback?.Invoke(true);
            return;
        }
        var covers = new List<string>();
        foreach (var cfg in configs)
        {
            covers.Add(cfg.Cover);
        }
        Inst.GetCabinCharacterToneBatchPreview(tokenID, covers, (b, data) =>
        {
            if (!b || data == null || data.list == null || data.list.Count == 0)
            {
                callback?.Invoke(false);
                return;
            }
            cabinCharacterUgcInfo.voiceCommands ??= new();
            for (int i = 0; i < configs.Count; i++)
            {
                if (i >= data.list.Count)
                {
                    break;
                }
                var previewItem = data.list[i];
                if (previewItem == null)
                {
                    continue;
                }
                cabinCharacterUgcInfo.voiceCommands.Add(new voiceCommands()
                {
                    emoteId = configs[i].EmoUIID,
                    command = configs[i].Command,
                    isPgc = 1,
                    isMute = 0,
                    delaySecond = 0,
                    text = previewItem.text,
                    audioUrl = previewItem.url
                });
            }
            RefreshInteractContent();
            callback?.Invoke(true);
        });
    }

    // 修改第 idx 个口令互动绑定的表情，PGC/UGC 分别填充对应字段（仅本地修改，不发网络请求）
    public void ModifyVoiceCommandsEmoteLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, GoodsData data, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null || data.Assets == null || data.Assets.Count == 0)
        {
            LoggerUtils.LogError("表情数据异常");
            callback?.Invoke(false);
            return;
        }
        var isPgc = data.Assets[0].UgcInfo == null;
        if (isPgc)
        {
            cabinCharacterUgcInfo.voiceCommands[idx].ugcData = null;
            cabinCharacterUgcInfo.voiceCommands[idx].emoteId = data.Assets[0].Id;
            cabinCharacterUgcInfo.voiceCommands[idx].isPgc = 1;
        }
        else
        {
            cabinCharacterUgcInfo.voiceCommands[idx].emoteId = "";
            cabinCharacterUgcInfo.voiceCommands[idx].isPgc = 0;
            cabinCharacterUgcInfo.voiceCommands[idx].ugcData = JsonConvert.DeserializeObject<UgcIdleData>(JsonConvert.SerializeObject(data.Assets[0].UgcInfo.animInfo));
        }
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 更新第 idx 个口令互动的触发口令文本（仅本地修改，不发网络请求）
    public void ModifyVoiceCommandsCommand(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, string command, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改口令互动口令失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.voiceCommands[idx].command = command;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 设置第 idx 个口令互动的静音开关（仅本地修改，不发网络请求）
    public void ModifyVoiceCommandsMute(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, bool isMute, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改口令互动静音失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.voiceCommands[idx].isMute = isMute ? 1 : 0;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 更新第 idx 个口令互动的触发延迟时间（秒）（仅本地修改，不发网络请求）
    public void ModifyVoiceCommandsDelaySecond(CabinCharacterBaseInfo cabinCharacterUgcInfo, int idx, int delaySecond, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("修改口令互动延迟时间失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        cabinCharacterUgcInfo.voiceCommands[idx].delaySecond = delaySecond;
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    // 删除第 idx 个口令互动（仅本地修改，不发网络请求）
    public void DeleteVoiceCommands(CabinCharacterBaseInfo characterUgcInfo, int idx, Action<bool> callback = null)
    {
        if (characterUgcInfo == null)
        {
            LoggerUtils.LogError("删除口令互动失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        if (idx >= characterUgcInfo.voiceCommands.Count)
        {
            LoggerUtils.LogError("删除口令互动失败:索引越界");
            callback?.Invoke(false);
            return;
        }

        if (characterUgcInfo is CabinCharacterUgcInfo && characterUgcInfo.voiceCommands.Count == 1)
        {
            TipPanel.ShowToast("删除失败，至少需要保留一个动作");
            callback?.Invoke(false);
            return;
        }

        characterUgcInfo.voiceCommands.RemoveAt(idx);
        RefreshInteractContent();
        callback?.Invoke(true);
    }

    #endregion

    // 将本地已修改的 info 数据同步到服务端，根据实际类型分发到对应接口
    public void SyncToServer(CabinCharacterBaseInfo info, SetType setType, Action<bool> callback = null)
    {
        if (info is CabinCharacterUgcInfo ugcInfo)
        {
            SetCabinCharacterInfo(ugcInfo, setType, callback);
        }
        else if (info is CabinCharacterPackInfo packInfo)
        {
            SetCabinCharacterPackInfo(packInfo, setType, (b, _) => callback?.Invoke(b));
        }
    }


    /// <summary>
    /// 设置是否预览显示 带养成舱角色/不带BOX角色
    /// </summary>
    /// <param name="isPreviewShowRoleWithCabin"></param>
    public void SetPreviewShowRoleWithCabin(bool isPreviewShowRoleWithCabin)
    {
        IsPreviewShowRoleWithCabin = isPreviewShowRoleWithCabin;
        PlayerPrefs.SetInt("IsPreviewShowRoleWithCabin", isPreviewShowRoleWithCabin ? 1 : 0);
    }

    /// <summary>
    /// 获取角色草稿列表
    /// </summary>
    public void GetCabinCharacterDraftList(Action<bool, List<CabinPublishData>> callback = null)
    {
        var req = new JObject()
        {
            ["uid"] = AccountDataManager.Inst.Uid,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterDraftList, HttpMethod.GET,
          JsonConvert.SerializeObject(req),
          rspStr =>
          {
              LoggerUtils.LogError("获取角色草稿列表成功:" + rspStr);
              CabinPublishListData rsp = JsonConvert.DeserializeObject<CabinPublishListData>(rspStr);
              if (rsp == null)
              {
                  return;
              }
              var draftList = rsp.list ?? new();
              callback?.Invoke(true, draftList);
          }, errRspStr =>
          {
              LoggerUtils.LogError("获取角色草稿列表失败:" + errRspStr);
              callback?.Invoke(false, null);
          });
    }
    /// <summary>
    /// 搜索角色列表 GET /search/character
    /// </summary>
    public void SearchCharacter(string keyword, CabinPurchasedType purchasedType,
        Action<bool, List<CabinPublishData>> callback = null)
    {
        var req = new JObject()
        {
            ["searchWord"] = keyword ?? "",
            ["searchScope"] = $"{(int)purchasedType}",
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SearchCharacter, HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                if (string.IsNullOrEmpty(rspStr))
                {
                    LoggerUtils.LogError("[CabinNetManager] SearchCharacter 响应体为空");
                    callback?.Invoke(false, null);
                    return;
                }

                CabinSearchListData rsp = JsonConvert.DeserializeObject<CabinSearchListData>(rspStr);
                if (rsp == null)
                {
                    callback?.Invoke(false, null);
                    return;
                }

                callback?.Invoke(true, rsp.GetData());
            }, errRspStr =>
            {
                LoggerUtils.LogError("[CabinNetManager] 搜索角色列表失败:" + errRspStr);
                callback?.Invoke(false, null);
            });
    }

    /// <summary>
    /// 获取角色发布列表
    /// </summary>
    public void GetNetCabinCharacterPublishList(CabinPurchasedType cabinPurchasedType, Action<bool, List<CabinPublishData>> callback = null)
    {
        var req = new JObject()
        {
            ["uid"] = AccountDataManager.Inst.Uid,
            ["purchasedType"] = $"{(int)cabinPurchasedType}",
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterPublishList, HttpMethod.GET,
          JsonConvert.SerializeObject(req),
          rspStr =>
              {
                  LoggerUtils.LogError("获取角色发布列表成功:" + rspStr);
                  CabinPublishListData rsp = JsonConvert.DeserializeObject<CabinPublishListData>(rspStr);
                  if (rsp == null)
                  {
                      return;
                  }
                  var publishList = rsp.list ?? new();
                  LoggerUtils.LogError("获取角色发布列表成功:" + publishList.Count);
                  callback?.Invoke(true, publishList);
              }, errRspStr =>
              {
                  LoggerUtils.LogError("获取角色发布列表失败:" + errRspStr);
                  callback?.Invoke(false, null);
              });
    }


    /// <summary>
    /// 角色设置
    /// </summary>
    public void SetCabinCharacterInfo(CabinCharacterUgcInfo cabinCharacterUgcInfo, SetType tSetType, Action<bool> callback = null)
    {
        SetCabinCharacterInfo(cabinCharacterUgcInfo, tSetType, (ok, _) => callback?.Invoke(ok));
    }

    // 将角色设置请求发往服务端，成功后按 setType 同步更新本地字典，并将完整角色信息回传给调用方
    public void SetCabinCharacterInfo(CabinCharacterUgcInfo cabinCharacterUgcInfo, SetType tSetType, Action<bool, CabinCharacterUgcInfo> callback)
    {
        var req = new SetCabinCharacterInfoData()
        {
            setType = tSetType,
            characterInfo = cabinCharacterUgcInfo,
        };
        if (string.IsNullOrEmpty(cabinCharacterUgcInfo.toneId))
        {
            LoggerUtils.LogError("角色设置失败没有设置音色:");
            callback?.Invoke(false, null);
            return;
        }
        Debug.LogError("setType:" + tSetType);
        Debug.LogError("SetCabinCharacterInfo: " + JsonConvert.SerializeObject(req));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterSet, HttpMethod.POST,
          JsonConvert.SerializeObject(req),
          rspStr =>
          {
              SetCabinCharacterInfoRspData rsp = JsonConvert.DeserializeObject<SetCabinCharacterInfoRspData>(rspStr);

              LoggerUtils.LogError("角色设置成功:" + rsp?.characterInfo?.id);
              callback?.Invoke(true, rsp.characterInfo);
              // 按 setType 广播对应消息事件，通知各模块更新本地列表缓存
              switch (tSetType)
              {
                  case SetType.Create:
                  case SetType.Copy:
                      MessageHelper.Broadcast<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterAdded, rsp.characterInfo);
                      break;
                  case SetType.Edit:
                  case SetType.Update:
                      MessageHelper.Broadcast<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterEdited, rsp.characterInfo);
                      break;
                  case SetType.Publish:
                  case SetType.ForcePublish:
                      MessageHelper.Broadcast<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterPublished, rsp.characterInfo);
                      break;
                  case SetType.Delete:
                      MessageHelper.Broadcast<string>(MessageName.OnCabinDraftCharacterDeleted, cabinCharacterUgcInfo.id);
                      break;
                  case SetType.Unpublish:
                      MessageHelper.Broadcast<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterUnPublished, rsp.characterInfo);
                      break;
              }
          }, errRspStr =>
          {
              HttpResponseRawData errRsp = JsonConvert.DeserializeObject<HttpResponseRawData>(errRspStr);
              Debug.LogError("角色设置失败:" + errRspStr);
              TipPanel.ShowToast(errRsp?.rmsg);
              callback?.Invoke(false, null);
          });
    }

    /// <summary>
    /// 只修改角色名称
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    public void ModifyCabinName(CabinCharacterUgcInfo cabinCharacter, Action<bool> callback = null)
    {
        if (cabinCharacter == null)
        {
            LoggerUtils.LogError("修改角色名称失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        SetCabinCharacterInfo(cabinCharacter, SetType.Edit, callback);
    }
    /// <summary>
    /// 只修改角色背景故事
    /// </summary>
    /// <param name="backgroundStory"></param>
    /// <param name="callback"></param>
    public void ModifyCabinBackgroundStory(CabinCharacterUgcInfo cabinCharacter, Action<bool> callback = null)
    {
        if (cabinCharacter == null)
        {
            LoggerUtils.LogError("修改角色背景故事失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        SetCabinCharacterInfo(cabinCharacter, SetType.Edit, callback);
    }

    /// <summary>
    /// 更新皮肤包avatarJson及封面
    /// </summary>
    /// <param name="skinPackId"></param>
    /// <param name="characterData"></param>
    // 按 packId 找到对应皮肤包，更新其 avatarJson 和封面 url，然后提交到服务端
    public void UpdateSkinPack(CabinCharacterBaseInfo cabinCharacterUgcInfo, string skinPackId, CharacterData characterData, string cover, Action<bool> callback = null)
    {
        if (cabinCharacterUgcInfo == null)
        {
            LoggerUtils.LogError("更新皮肤包失败:角色信息为空");
            callback?.Invoke(false);
            return;
        }
        for (int i = 0; i < cabinCharacterUgcInfo.skinPack.Count; i++)
        {
            if (cabinCharacterUgcInfo.skinPack[i].packId == skinPackId)
            {
                cabinCharacterUgcInfo.skinPack[i].avatarJson = CharacterData.SerializeObject(characterData);
                cabinCharacterUgcInfo.skinPack[i].cover = cover;
                break;
            }
        }
        Action<bool> onUpdate = (isSuccess) =>
        {
            if (isSuccess)
            {
                LoggerUtils.LogError("更新皮肤包成功");
                callback?.Invoke(true);
            }
            else
            {
                LoggerUtils.LogError("更新皮肤包失败");
                callback?.Invoke(false);
            }
        };
        if (cabinCharacterUgcInfo is CabinCharacterUgcInfo)
        {
            SetCabinCharacterInfo((CabinCharacterUgcInfo)cabinCharacterUgcInfo, SetType.Edit, onUpdate);
        }
        else if (cabinCharacterUgcInfo is CabinCharacterPackInfo)
        {
            SetCabinCharacterPackInfo((CabinCharacterPackInfo)cabinCharacterUgcInfo, SetType.Edit, (b, data) => { onUpdate(b); });
        }
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    public void GetCabinCharacterInfo(string cabinCharacterId, Action<bool, CabinCharacterDetailData> callback = null)
    {
        if (string.IsNullOrEmpty(cabinCharacterId))
        {
            LoggerUtils.LogError("获取角色详情失败:参数为空");
            callback?.Invoke(false, null);
            return;
        }
        var req = new JObject()
        {
            ["id"] = cabinCharacterId,
        };
        NetworkManager.Inst.SendHttpRequest<CabinCharacterDetailData>(HttpUrlDefine.CabinCharacterInfo, HttpMethod.GET,
        JsonConvert.SerializeObject(req),
        (UnityEngine.Events.UnityAction<CabinCharacterDetailData>)(rsp =>
        {
            if (rsp == null)
            {
                callback?.Invoke(false, rsp);
                return;
            }
            CabinCharacterUgcInfo cabinCharacterUgcInfo = rsp.characterInfo;
            if (cabinCharacterUgcInfo == null)
            {
                callback?.Invoke(false, rsp);
                return;
            }
            // cabinCharacterUgcInfo.activation = new()
            // {
            //     new characterInteraction()
            //     {
            //         emoteId = "40200515",
            //         isMute = 0,
            //         delaySecond = 1,
            //         text = "1",
            //     },
            //     new characterInteraction()
            //     {
            //         emoteId = "40200516",
            //         isMute = 1,
            //         delaySecond = 2,
            //         text = "2",
            //     },
            // };
            // cabinCharacterUgcInfo.voiceCommands = new()
            // {
            //     new voiceCommands()
            //     {
            //         command = "1111",
            //         emoteId = "40200518",
            //         isMute = 0,
            //         delaySecond = 2,
            //         text = "1",
            //     },
            //     new voiceCommands()
            //     {
            //         command = "2222",
            //         emoteId = "40200519",
            //         isMute = 1,
            //         delaySecond = 3,
            //         text = "2",
            //     },
            // };
            //
            cabinCharacterUgcInfo.activation ??= new();
            cabinCharacterUgcInfo.voiceCommands ??= new();
            cabinCharacterUgcInfo.extensionPackList ??= new();
            // net 和 temp 共享同一引用，编辑时修改 temp 即等同于修改 net
            LoggerUtils.LogError("1获取角色详情成功:" + cabinCharacterUgcInfo.id);
            LoggerUtils.LogError("2获取角色详情成功:" + JsonConvert.SerializeObject(cabinCharacterUgcInfo));
            callback?.Invoke(true, rsp);
        }), errRsp =>
        {
            LoggerUtils.LogError("获取角色详情失败:" + errRsp.rmsg);
            callback?.Invoke(false, null);
        });
    }

    /// <summary>
    /// 获取角色详情（含创作者信息和互动信息），用于在详情面板展示作者头像/昵称及点赞数
    /// </summary>
    public void GetCabinCharacterCreatorInfo(string characterId, Action<CabinCharacterDetailData> callback)
    {
        var req = new JObject()
        {
            ["id"] = characterId,
        };
        NetworkManager.Inst.SendHttpRequest<CabinCharacterDetailData>(HttpUrlDefine.CabinCharacterInfo, HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            (rsp) =>
            {
                callback?.Invoke(rsp);
            }, errRsp =>
            {
                LoggerUtils.LogError("获取角色创作者信息失败:" + errRsp.rmsg);
                callback?.Invoke(null);
            });
    }



    /// <summary>
    /// 批量获取音色音频
    /// </summary>
    /// <param name="toneId">音色 ID</param>
    /// <param name="texts">待生成语音的文本列表</param>
    /// <param name="callback">回调：(是否成功, 预览数据)</param>
    /// <param name="languageType">语言类型：0=中文（默认），1=英文，2=日文</param>
    public void GetCabinCharacterToneBatchPreview(string toneId, List<string> texts, Action<bool, CabinDoubaoBatchPreviewData> callback = null, int languageType = 0)
    {
        var toneinfo = CabinToneNetManager.Inst.GetToneInfo(toneId);

        if (toneinfo == null)
        {
            // 本地缓存未命中（如编辑皮肤时本体使用了 UGC 音色），先从网络拉取音色详情
            CabinToneNetManager.Inst.GetCabinToneInfo(toneId, success =>
            {
                if (!success)
                {
                    LoggerUtils.LogError($"[CabinNetManager] GetCabinCharacterToneBatchPreview：音色 {toneId} 详情获取失败");
                    callback?.Invoke(false, null);
                    return;
                }

                // 缓存已更新，重新调用自身完成批量预览
                GetCabinCharacterToneBatchPreview(toneId, texts, callback, languageType);
            });
            return;
        }

        // 按语言类型查找对应 voiceId；找不到时回调失败
        var langData = toneinfo.languageList?.Find(l => l.type == languageType);

        if (langData == null)
        {
            callback?.Invoke(false, null);
            return;
        }

        var req = new ReqToneBatchPreview()
        {
            voiceId = langData.voiceId,
            texts = texts,
            languageType = languageType,
        };

        string str = JsonConvert.SerializeObject(req);
        NetworkManager.Inst.SendHttpRequest<CabinDoubaoBatchPreviewData>(HttpUrlDefine.CabinCharacterToneBatchPreview, HttpMethod.POST,
          JsonConvert.SerializeObject(req),
          rsp =>
          {
              if (rsp == null)
              {
                  return;
              }
              callback?.Invoke(true, rsp);
              LoggerUtils.LogError("批量获取音色音频成功:");
          }, errRspStr =>
          {
              callback?.Invoke(false, null);
              LoggerUtils.LogError("批量获取音色音频失败:" + errRspStr);
          });
    }

    /// <summary>批量获取扩展包详情，packIds 来自 CabinCharacterUgcInfo.extensionPackList</summary>
    public void GetExtensionPackBatchInfo(List<string> packIds, Action<bool, List<CabinCharacterPackInfo>> callback = null)
    {
        if (packIds?.Count == 0)
        {
            callback?.Invoke(false, null);
            return;
        }
        var req = new JObject { ["idList"] = string.Join(",", packIds) };

        NetworkManager.Inst.SendHttpRequest<CabinExtensionPackBatchRspData>(
            HttpUrlDefine.CabinCharacterPackBatchInfo, HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rsp =>
            {
                callback?.Invoke(true, rsp?.characterPackList?.ConvertAll(x => x.characterPackInfo) ?? new());
                LoggerUtils.Log("批量获取扩展包成功");
            },
            errRsp =>
            {
                callback?.Invoke(false, null);
                LoggerUtils.LogError("批量获取扩展包失败: " + errRsp);
            });
    }

    /// <summary>
    /// 扩展包设置，setType：1 创建扩展包  2 编辑扩展包  3 复制扩展包  4 发布扩展包  5 更新发布  7 删除 8 直接发布
    /// 调用方在传入 SetType.Publish 前，应自行确认角色已处于发布状态（ugcclass == 2）。
    /// </summary>
    public void SetCabinCharacterPackInfo(CabinCharacterPackInfo packInfo, SetType setType, Action<bool, CabinCharacterBaseInfo> callback = null)
    {
        var req = new SetCabinCharacterPackInfoData()
        {
            setType = setType,
            characterPackInfo = packInfo,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterPackSet, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                var rsp = JsonConvert.DeserializeObject<SetCabinCharacterPackInfoRspData>(rspStr);
                LoggerUtils.Log("扩展包设置成功: " + rsp?.characterPackInfo?.id);
                callback?.Invoke(true, rsp?.characterPackInfo);
                switch (setType)
                {
                    case SetType.Copy:
                    case SetType.Create:
                        MessageHelper.Broadcast(MessageName.OnCabinExtPackCreated, rsp?.characterPackInfo);
                        break;
                    case SetType.Edit:
                    case SetType.Unpublish:
                        MessageHelper.Broadcast(MessageName.OnCabinExtPackUpdated, rsp?.characterPackInfo);
                        break;
                    case SetType.Delete:
                        break;
                    case SetType.Publish:
                    case SetType.ForcePublish:
                        MessageHelper.Broadcast(MessageName.OnCabinExtPackDelect, packInfo);
                        MessageHelper.Broadcast(MessageName.OnCabinExtPackCreated, rsp?.characterPackInfo);
                        break;
                }
            },
            errRspStr =>
            {
                HttpResponseRawData errRsp = JsonConvert.DeserializeObject<HttpResponseRawData>(errRspStr);
                TipPanel.ShowToast(errRsp?.rmsg);
                LoggerUtils.LogError("扩展包设置失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }



    /// <summary>
    /// 语音转文字
    /// </summary>
    /// <param name="audioUrl"></param>
    /// <param name="callback"></param>
    public void AsrCabinCharacterTone(string audioUrl, Action<bool, string> callback = null)
    {
        var req = new JObject()
        {
            ["audioUrl"] = audioUrl,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinCharacterDoubaoAsr, HttpMethod.GET,
          JsonConvert.SerializeObject(req),
          rspStr =>
          {
              var rsp = JsonConvert.DeserializeObject<JObject>(rspStr);
              var text = rsp["text"].ToString();
              LoggerUtils.LogError("语音转文字成功:" + text);
              callback?.Invoke(true, text);
          }, errRspStr =>
          {
              LoggerUtils.LogError("语音转文字失败:" + errRspStr);
              HttpResponseRawData errRsp = JsonConvert.DeserializeObject<HttpResponseRawData>(errRspStr);
              callback?.Invoke(false, errRsp?.rmsg);
          });
    }

    /// <summary>
    /// 对音频转录出的文字进行文字审核。
    /// 审核通过时回调 true，审核未通过或请求失败时回调 false。
    /// </summary>
    /// <param name="text">待审核的文字内容</param>
    /// <param name="callback">回调：true=审核通过，false=未通过</param>
    public void AuditCabinText(string text, Action<bool> callback)
    {
        var req = new Dictionary<string, string> { { "text", text } };
        NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditText,
            HttpMethod.POST, req,
            rsp =>
            {
                bool passed = rsp != null && rsp.auditResult == (int)AuditResult.Passed;
                LoggerUtils.LogError("文字审核结果:" + (passed ? "通过" : "未通过"));
                callback?.Invoke(passed);
            }, errRspStr =>
            {
                LoggerUtils.LogError("文字审核请求失败:" + errRspStr);
                callback?.Invoke(false);
            });
    }

    /// <summary>
    /// 皮肤包皮肤编辑结束(1.更新avatar 2.重新拍照)
    /// </summary>
    public void SkinEditEnd(CabinCharacterBaseInfo cabinCharacterUgcInfo, string skinPackId, CharacterData characterData, bool isDataChange)
    {
        if (isDataChange)
        {
            //数据发生改变
            //更新avatar
            UpdateAvatar(characterData);
            //重新拍照
            ReTakePhoto((isSuccess, url) =>
            {
                if (isSuccess)
                {
                    //重新拍照成功
                }
                if (cabinCharacterUgcInfo == null)
                {
                    return;
                }
                UpdateSkinPack(cabinCharacterUgcInfo, skinPackId, characterData, url);
            });
        }
    }


    /// <summary>
    /// 皮肤包皮肤编辑结束（仅本地修改：更新 avatarJson 内存 + 刷新显示，不截图不发网络请求）
    /// </summary>
    public void SkinEditEndLocal(CabinCharacterBaseInfo cabinCharacterUgcInfo, string skinPackId, CharacterData characterData, bool isDataChange)
    {
        if (!isDataChange || cabinCharacterUgcInfo == null)
        {
            return;
        }
        string newAvatarJson = CharacterData.SerializeObject(characterData);
        for (int i = 0; i < cabinCharacterUgcInfo.skinPack.Count; i++)
        {
            if (cabinCharacterUgcInfo.skinPack[i].packId == skinPackId)
            {
                cabinCharacterUgcInfo.skinPack[i].avatarJson = newAvatarJson;
                break;
            }
        }
        UpdateAvatar(CharacterData.DeserializeObject(newAvatarJson));
    }

    // 皮肤编辑完成后追加新皮肤包：更新 avatar 并重新拍照作为封面
    public void SkinEditEndCreateSkinPack(CabinCharacterBaseInfo characterUgcInfo, CharacterData characterData, bool isDataChange)
    {
        if (isDataChange)
        {
            //数据发生改变
            //更新avatar
            UpdateAvatar(characterData);
            //重新拍照
            ReTakePhoto((isSuccess, url) =>
            {
                if (isSuccess)
                {
                    //重新拍照成功
                }
                AddSkinPack(characterUgcInfo, characterData, url);
            });
        }
    }

    // 广播消息通知各模块更新 avatar 外观
    public void UpdateAvatar(CharacterData characterData)
    {
        MessageHelper.Broadcast<CharacterData>(MessageName.OnCabinUpdateAvatar, characterData);
    }

    // 广播消息触发重新拍照，通过回调返回成功状态和图片 url
    public void ReTakePhoto(Action<bool, string> callback)
    {
        MessageHelper.Broadcast(MessageName.OnCabinTakeMatchPhoto, callback);
    }
    // 广播消息通知刷新互动内容展示
    public void RefreshInteractContent()
    {
        MessageHelper.Broadcast(MessageName.OnCabinRefreshInteractContent);
    }

    // 广播消息触发预览唤醒动作（characterInteraction 版本）
    public void PreviewActivation(characterInteraction interaction)
    {
        MessageHelper.Broadcast<characterInteraction>(MessageName.OnCabinBeginPreviewActivation, interaction);
    }

    // 广播消息触发预览口令互动（voiceCommands 版本）
    public void PreviewActivation(voiceCommands voiceCommands)
    {
        MessageHelper.Broadcast<voiceCommands>(MessageName.OnCabinBeginPreviewVoiceCommands, voiceCommands);
    }
}

public enum RecordVoiceResult
{
    Success,
    Fail,
    TimeOut,
}

