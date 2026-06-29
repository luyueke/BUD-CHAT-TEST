using Game.Store;
using System;
using UI.UIPanels.IncubationCabin;

/// <summary>
/// 养成舱角色面板网络管理器，持有 IncubationCabinRolesPanel 引用并代理面板回调
/// 数据/网络操作委托给 CabinNetManager
/// </summary>
public class CabinRolesNetManager : GlobalInstance<CabinRolesNetManager>
{
    internal IncubationCabinRolesPanel panel;

    // ───── 面板注册 ─────

    // 绑定面板引用，后续 UI 回调将通过此引用转发给面板
    public void RegisterPanel(IncubationCabinRolesPanel p)
    {
        panel = p;
    }

    // 解除面板引用，防止面板关闭后仍持有引用导致空调用
    public void UnregisterPanel()
    {
        panel = null;
    }

    // ───── 代理 CabinNetManager 数据接口 ─────

    private CabinCharacterUgcInfo _currentCharacterUgcInfo;

    public void SelectDefaultCabinCharacterUgcInfo()
    {
        CabinNetManager.Inst.SelectDefaultCabinCharacterUgcInfo();
    }

    // 记录当前正在编辑的角色，后续所有交互操作均针对此角色发起
    public void SetCurrentCharacterUgcInfo(CabinCharacterUgcInfo info)
    {
        _currentCharacterUgcInfo = info;
    }

    public CabinCharacterUgcInfo GetNetCabinCharacterUgcInfo()
    {
        return _currentCharacterUgcInfo;
    }


    // ───── 面板回调（由外部逻辑调用，转发给面板）─────


    // 触发面板开始预览唤醒动作
    public void PreviewActivation(characterInteraction interaction)
    {
        panel?.BeginPreviewActivation(interaction);
    }

    // 触发面板开始预览口令互动
    public void PreviewActivation(voiceCommands voiceCommands)
    {
        panel?.BeginPreviewVoiceCommands(voiceCommands);
    }

    // 本地更新唤醒动作语音文本和 url 后同步到服务端
    public void ModifyActivationTextId(int idx, string text, string url, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyActivationTextId(_currentCharacterUgcInfo, idx, text, url);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地修改唤醒动作绑定表情后同步到服务端
    public void ModifyActivationEmote(int idx, GoodsData goodsData, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyActivationEmoteLocal(_currentCharacterUgcInfo, idx, goodsData);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地修改唤醒动作延迟时间后同步到服务端
    public void ModifyActivationDelaySecond(int idx, int delaySecond, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyActivationDelaySecond(_currentCharacterUgcInfo, idx, delaySecond);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地删除成功后同步到服务端，删除失败直接通过 callback 回调 false
    public void DeleteActivation(int idx, Action<bool> callback = null)
    {
        CabinNetManager.Inst.DeleteActivation(_currentCharacterUgcInfo, idx, isSuccess =>
        {
            if (!isSuccess)
            {
                callback?.Invoke(false);
                return;
            }
            CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
        });
    }

    // 本地修改唤醒动作静音状态后同步到服务端
    public void ModifyActivationMute(int idx, bool isMute, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyActivationMute(_currentCharacterUgcInfo, idx, isMute);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地更新口令互动的触发词后同步到服务端
    public void ModifyVoiceCommandsCommand(int idx, string command, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyVoiceCommandsCommand(_currentCharacterUgcInfo, idx, command);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地更新口令互动语音文本和 url 后同步到服务端
    public void ModifyVoiceCommandsTextId(int idx, string text, string url, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyVoiceCommandsTextId(_currentCharacterUgcInfo, idx, text, url);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地修改口令互动绑定表情后同步到服务端
    public void ModifyVoiceCommandsEmote(int idx, GoodsData goodsData, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyVoiceCommandsEmoteLocal(_currentCharacterUgcInfo, idx, goodsData);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地修改口令互动延迟时间后同步到服务端
    public void ModifyVoiceCommandsDelaySecond(int idx, int delaySecond, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyVoiceCommandsDelaySecond(_currentCharacterUgcInfo, idx, delaySecond);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    // 本地删除成功后同步到服务端，删除失败直接通过 callback 回调 false
    public void DeleteVoiceCommands(int idx, Action<bool> callback = null)
    {
        CabinNetManager.Inst.DeleteVoiceCommands(_currentCharacterUgcInfo, idx, isSuccess =>
        {
            if (!isSuccess)
            {
                callback?.Invoke(false);
                return;
            }
            CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
        });
    }

    // 本地修改口令互动静音状态后同步到服务端
    public void ModifyVoiceCommandsMute(int idx, bool isMute, Action<bool> callback = null)
    {
        CabinNetManager.Inst.ModifyVoiceCommandsMute(_currentCharacterUgcInfo, idx, isMute);
        CabinNetManager.Inst.SyncToServer(_currentCharacterUgcInfo, SetType.Edit, callback);
    }

    /// <summary>
    /// 将指定角色数据以 setType=5 更新发布到服务器（用于本地编辑后的统一保存）
    /// 成功后用服务器回包更新 _currentCharacterUgcInfo，确保后续读取到最新数据
    /// </summary>
    public void SaveCharacterInfo(CabinCharacterUgcInfo info, Action<bool> callback = null)
    {
        if (info == null)
        {
            callback?.Invoke(false);
            return;
        }
        CabinNetManager.Inst.SetCabinCharacterInfo(info, SetType.Update, (isSuccess, updatedInfo) =>
        {
            if (isSuccess && updatedInfo != null)
            {
                _currentCharacterUgcInfo = updatedInfo;
            }
            callback?.Invoke(isSuccess);
        });
    }

    /// <summary>
    /// 向指定角色数据的待机列表本地添加动作（不发服务器，由调用方决定何时保存）。
    /// 返回 true 表示添加成功，false 表示数据异常、重复或已达数量上限。
    /// 数量上限：待机主动作最多 <see cref="CabinConfig.EditViewLoopEmoteMax"/> 个，
    /// 待机表演动作最多 <see cref="CabinConfig.EditViewNonLoopEmoteMax"/> 个。
    /// </summary>
    public bool TryAddPendingEmoteToInfo(CabinCharacterUgcInfo cabinInfo, GoodsData data, bool isLoop)
    {
        if (cabinInfo == null)
            return false;

        pEmoteData pEmoteData;
        bool isPgc;

        // leisure/default 是内置 PGC 待机动作，可直接通过 Id 构建，无需 Assets
        if (data.Id == "leisure" || data.Id == "default")
        {
            pEmoteData = new() { emoteId = data.Id, ugcData = null };
            isPgc = true;
        }
        else
        {
            if (data.Assets == null || data.Assets.Count == 0)
            {
                TipPanel.ShowToast("添加待机动作失败:表情数据异常");
                return false;
            }

            isPgc = data.Assets[0].UgcInfo == null;
            pEmoteData = new();
            if (isPgc)
            {
                pEmoteData.emoteId = data.Assets[0].Id;
                pEmoteData.ugcData = null;
            }
            else
            {
                pEmoteData.emoteId = "";
                pEmoteData.ugcData = Newtonsoft.Json.JsonConvert.DeserializeObject<UgcIdleData>(
                    Newtonsoft.Json.JsonConvert.SerializeObject(data.Assets[0].UgcInfo.animInfo));
            }
        }

        cabinInfo.usingEmote ??= new PendingEmoteData();
        if (isLoop)
        {
            cabinInfo.usingEmote.loopEmoteList ??= new();

            // 手动添加上限检查：超出上限则提示并拒绝（与购买皮肤的自动覆盖逻辑不同）
            if (cabinInfo.usingEmote.loopEmoteList.Count >= CabinConfig.EditViewLoopEmoteMax)
            {
                TipPanel.ShowToast($"待机主动作最多{CabinConfig.EditViewLoopEmoteMax}个");
                return false;
            }

            foreach (var t in cabinInfo.usingEmote.loopEmoteList)
            {
                if (isPgc && t.emoteId == pEmoteData.emoteId || !isPgc && pEmoteData.ugcData?.id == t.ugcData?.id)
                {
                    TipPanel.ShowToast("添加重复动作");
                    return false;
                }
            }
            cabinInfo.usingEmote.loopEmoteList.Add(pEmoteData);
        }
        else
        {
            cabinInfo.usingEmote.emoteList ??= new();

            // 手动添加上限检查：超出上限则提示并拒绝（与购买皮肤的自动覆盖逻辑不同）
            if (cabinInfo.usingEmote.emoteList.Count >= CabinConfig.EditViewNonLoopEmoteMax)
            {
                TipPanel.ShowToast($"待机表演动作最多{CabinConfig.EditViewNonLoopEmoteMax}个");
                return false;
            }

            foreach (var t in cabinInfo.usingEmote.emoteList)
            {
                if (isPgc && t.emoteId == pEmoteData.emoteId || !isPgc && pEmoteData.ugcData?.id == t.ugcData?.id)
                {
                    TipPanel.ShowToast("添加重复动作");
                    return false;
                }
            }
            cabinInfo.usingEmote.emoteList.Add(pEmoteData);
        }
        return true;
    }

    /// <summary>
    /// 用已有本地数据直接打开 IncubationCabinRolesPanel，无需等待网络。
    /// skinPack 不完整时自动降级到 OpenPanelWithFreshData。
    /// creator/interactInfo 缺失由 RefreshBaseMsg 内的异步请求补全。
    /// </summary>
    public void OpenPanelWithLocalData(CabinCharacterUgcInfo existingInfo, string skinId = null, Action onClose = null, bool isFromShop = false)
    {
        if (existingInfo?.skinPack == null || existingInfo.skinPack.Count == 0)
        {
            OpenPanelWithFreshData(existingInfo?.id, skinId, onClose, isFromShop);
            return;
        }
        var publishData = new CabinPublishData(existingInfo);
        UIManager.Inst.OpenPanel(PanelId.IncubationCabinRolesPanel, publishData, skinId, onClose, isFromShop);
    }

    /// <summary>
    /// 先请求 /ugc/character/info 获取最新角色数据，成功后打开 IncubationCabinRolesPanel。
    /// 接口失败时打印日志，不打开面板。
    /// </summary>
    /// <param name="characterId">角色 ID</param>
    /// <param name="skinId">打开时预选的皮肤包 packId，null 则按默认逻辑</param>
    /// <param name="onClose">面板关闭时的回调</param>
    /// <param name="isFromShop">是否从商城入口打开；为 true 时面板待机 Tab 使用 pendingEmote + 扩展包数据</param>
    /// <param name="onOpened">面板打开后的回调，传入面板实例；用于面板打开后立即执行额外操作（如 BOX 同步检测）</param>
    public void OpenPanelWithFreshData(string characterId, string skinId = null, Action onClose = null, bool isFromShop = false, Action<IncubationCabinRolesPanel> onOpened = null)
    {
        CabinNetManager.Inst.GetCabinCharacterInfo(characterId, (isSuccess, rsp) =>
        {
            if (!isSuccess || rsp?.characterInfo == null)
            {
                LoggerUtils.LogError($"打开角色面板失败：获取角色详情失败，id={characterId}");
                return;
            }

            var publishData = new CabinPublishData(rsp.characterInfo);

            if (rsp.creator != null)
                publishData.creatorInfo = rsp.creator;

            if (rsp.interactInfo != null)
                publishData.interactInfo = rsp.interactInfo;

            UIManager.Inst.OpenPanel(PanelId.IncubationCabinRolesPanel, publishData, skinId, onClose, isFromShop);

            // 面板打开后（RegisterPanel 已在 OnShown 中同步执行），回调通知调用方
            onOpened?.Invoke(panel);
        });
    }
}
