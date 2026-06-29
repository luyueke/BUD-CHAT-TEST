using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Audio;
using Game.Avatar;
using Newtonsoft.Json;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏场景内召唤 AI 伙伴的选择面板。
/// 拉取玩家持有的全部 Cabin AI 伙伴列表，选中后点击 Call 按钮完成召唤，
/// 召唤出现后随机播放一个唤醒动作并播放动作语音/音效。
/// </summary>
public class AIBoxBuddyCallPanel : BasePanel<AIBoxBuddyCallPanel>
{
    [SerializeField] private Button Btn_Close;
    [SerializeField] private Button Btn_Call;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private MISource changeType;
    [SerializeField] private Button goToStoreBtn;

    public Action CloseAction { private get; set; }

    // AI 伙伴语音（唤醒/口令）在 SFX 音量基础上的额外增益，觉得小可调大（最终 clamp 到 1）。
    private const float BuddyVoiceVolumeScale = 1.5f;

    /// <summary>当前已召唤 AI 伙伴的完整数据，供口令面板等读取（未召唤过为 null）</summary>
    public static CabinCharacterUgcInfo CurrentSummonedInfo { get; private set; }

    /// <summary>当前已装备皮肤的 packId（召唤时为默认皮肤，换装面板确认后更新）</summary>
    public static string CurrentSkinPackId { get; set; }

    /// <summary>
    /// 当前装备皮肤覆盖的口令；为 null 时口令面板回退到 CurrentSummonedInfo（主体）的口令。
    /// 仅当所选皮肤自带非空口令时才设值，避免无口令皮肤把口令清空。
    /// </summary>
    public static System.Collections.Generic.List<voiceCommands> ActiveSkinVoiceCommands { get; set; }

    // 召唤后留给唤醒动作播放的时间，之后再进入待机循环
    private const float AwakeAnimReserveTime = 4f;

    private List<CabinCharacterCardItem> _itemList = new List<CabinCharacterCardItem>();
    private CabinCharacterCardItem _selectedItem;
    private CabinCharacterUgcInfo _selectedInfo;
    private MISource.Source _currentSource = MISource.Source.Create;
    private Text _callBtnLabel;            // Btn_Call 文字（召唤 / 解除召唤）
    private Color _callBtnNormalColor;     // 召唤态按钮原始底色
    private static readonly Color CallBtnDismissColor = new Color(0.745f, 0.745f, 0.745f, 1f); // 解除召唤态灰色

    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(OnCloseBtnClick);
        Btn_Call.onClick.AddListener(OnCallBtnClick);
        changeType.SetCallback(OnSourceChanged);

        _callBtnLabel = Btn_Call.GetComponentInChildren<Text>(true);
        if (Btn_Call.image != null) _callBtnNormalColor = Btn_Call.image.color;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        RefreshCallBtn();
        goToStoreBtn.gameObject.SetActive(false);
        // 默认显示 UGC 内容
        changeType.DefualtOn(MISource.Source.Create);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        ClearItems();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CloseAction?.Invoke();
    }

    public override void OnWindowBeFocused() { }
    public override void OnWindowPop() { }

    private void OnSourceChanged(MISource.Source source)
    {
        _currentSource = source;
        if (source == MISource.Source.Bud)
        {
            // PGC 暂未实现，清空列表，啥也不显示
            ClearItems();
            goToStoreBtn.gameObject.SetActive(false);
            RefreshCallBtn();
        }
        else
        {
            // UGC：加载玩家持有的 AI 伙伴
            FetchAndRefresh();
        }
    }

    private void FetchAndRefresh()
    {
        CabinNetManager.Inst.GetNetCabinCharacterPublishList(CabinPurchasedType.All, (isSuccess, list) =>
        {
            if (this == null) return;
            // 异步返回前若已切到其它 tab，则丢弃本次结果
            if (_currentSource != MISource.Source.Create) return;
            if (!isSuccess || list == null)
            {
                TipPanel.ShowToast("获取 AI 伙伴列表失败，请稍后重试");
                return;
            }
            RefreshItems(list);
        });
    }

    private void RefreshItems(List<CabinPublishData> list)
    {
        ClearItems();

        foreach (var data in list)
        {
            if (data?.characterInfo == null) continue;

            var go = Instantiate(itemPrefab, itemRoot);
            go.SetActive(true); // itemPrefab 为禁用模板，克隆体默认 inactive，需显式激活
            var item = go.GetComponent<CabinCharacterCardItem>();
            if (item == null) continue;

            var capturedInfo = data.characterInfo as CabinCharacterUgcInfo;
            item.SetData(data.characterInfo, _ => OnItemClicked(item, capturedInfo));
            _itemList.Add(item);
        }

        // UGC 列表为空时显示前往商店按钮
        goToStoreBtn.gameObject.SetActive(_itemList.Count == 0);
    }

    private void OnItemClicked(CabinCharacterCardItem clickedItem, CabinCharacterUgcInfo info)
    {
        if (_selectedItem != null)
            _selectedItem.SetSelected(false);

        _selectedItem = clickedItem;
        _selectedItem.SetSelected(true);
        _selectedInfo = info;

        RefreshCallBtn();
    }

    /// <summary>选中项是否正是当前已召唤的伙伴（按 id 判等）。</summary>
    private bool IsSelectedSummoned()
        => _selectedInfo != null && CurrentSummonedInfo != null && _selectedInfo.id == CurrentSummonedInfo.id;

    /// <summary>按选中态刷新 Call 按钮：未选中→禁用；选中已召唤者→灰色「解除召唤」；否则→原色「召唤」。</summary>
    private void RefreshCallBtn()
    {
        Btn_Call.interactable = _selectedInfo != null;
        bool dismiss = IsSelectedSummoned();
        if (_callBtnLabel != null)
            _callBtnLabel.text = dismiss ? "解除召唤" : "召唤";
        if (Btn_Call.image != null)
            Btn_Call.image.color = dismiss ? CallBtnDismissColor : _callBtnNormalColor;
    }

    private void OnCallBtnClick()
    {
        if (_selectedInfo == null)
        {
            TipPanel.ShowToast("请先选择一个 AI 伙伴");
            return;
        }

        // 选中的就是当前已召唤的伙伴 → 解除召唤（收回伙伴，原地刷新按钮回「召唤」，不关面板）
        if (IsSelectedSummoned())
        {
            GameAIBuddyManager.Inst.ExitSelfAIBuddy();
            CurrentSummonedInfo = null;
            RefreshCallBtn();
            return;
        }

        SummonByCabin(_selectedInfo);
        CloseSelf();
    }

    /// <summary>
    /// 用 Cabin 角色数据召唤自己的 AI 伙伴（收回旧伙伴 → 召唤 → 播唤醒动作 → 进待机循环），
    /// 并记录 CurrentSummonedInfo / 默认皮肤 / 重置皮肤口令覆盖。
    /// 供召唤面板与 CameraMode NpcMenu 共用，保证召唤行为一致。
    /// </summary>
    public static void SummonByCabin(CabinCharacterUgcInfo info)
    {
        if (info == null) return;

        // 已有召唤中的 AI 伙伴先收回（CreateOtherBuddy 不替换已存在的 buddy，必须先发 Cancel）
        if (AIBuddyAvatarController.Inst.SelfController != null)
            GameAIBuddyManager.Inst.ExitSelfAIBuddy();

        // 在 UI 层拆解 CabinCharacterUgcInfo，Game 层只接收基础类型
        var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
        var avatarJson = defaultSkin?.avatarJson ?? string.Empty;
        // 记录召唤时装备的默认皮肤，供换装面板标记"当前"；口令回退到主体
        CurrentSkinPackId = defaultSkin?.packId;
        ActiveSkinVoiceCommands = null;

        // 本次随机唤醒动作 + 待机数据，随召唤一起同步给其他玩家（randomResult 在此算好，保证各端一致）
        var activation = PickRandomActivation(info);
        var awakeData = BuildInteractSyncData(activation);
        var usingEmoteJson = info.usingEmote != null
            ? JsonConvert.SerializeObject(info.usingEmote)
            : string.Empty;

        GameAIBuddyManager.Inst.CallSelfAIBuddyByCabin(
            info.id, info.name, avatarJson, usingEmoteJson, awakeData);
        CurrentSummonedInfo = info;

        // 本地播放唤醒动作 + 启动待机（以局部变量捕获 buddy 引用，面板关闭后延迟回调仍安全）
        var buddyCtrl = AIBuddyAvatarController.Inst.SelfStateController;
        var buddyGo = AIBuddyAvatarController.Inst.SelfController != null
            ? AIBuddyAvatarController.Inst.SelfController.gameObject
            : null;
        if (buddyCtrl != null && awakeData != null)
            PlayActivation(buddyCtrl, buddyGo, awakeData);

        // 唤醒动作播完后进入待机循环（initialDelay 给唤醒动作留出播放时间）
        StartBuddyStandbyOn(buddyCtrl, info.usingEmote, awakeData != null ? AwakeAnimReserveTime : 0f);
    }

    /// <summary>在指定 buddy（自己端或其他端）上挂载待机循环。供同步接收方复用。</summary>
    public static void StartBuddyStandbyOn(PlayerStateController stateCtrl, PendingEmoteData usingEmote, float initialDelay)
    {
        if (usingEmote == null || stateCtrl?.Wrap?.Avatar == null) return;
        var animCtrl = stateCtrl.PlayerAnimCtrl;
        if (animCtrl == null) return;

        var ikController = stateCtrl.Wrap.Avatar.GetComponent<AnimIKController>();
        var standby = stateCtrl.Wrap.Avatar.GetOrAddComponent<AIBuddyStandbyBehaviour>();
        standby.StartStandby(animCtrl, ikController, stateCtrl, usingEmote, initialDelay, stateCtrl.PlayerID);
    }

    /// <summary>从唤醒/口令动作（BaseInteractionData）构造同步载体；PGC 动作在此算好 randomResult。</summary>
    /// <param name="buddyName">伙伴名，用于口令互动的聊天显示（唤醒动作可不传）</param>
    public static InteractSyncData BuildInteractSyncData(BaseInteractionData act, string buddyName = null)
    {
        if (act == null) return null;
        int randomResult = 0;
        if (act.isPgc == 1)
        {
            var cfgs = Es.DataTables.GetEmoAniConfigList().FindAll(c => c.emoId == act.emoteId);
            if (cfgs != null && cfgs.Count > 0)
            {
                int random = cfgs[0].randomCount;
                randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
            }
        }
        return new InteractSyncData
        {
            EmoteId      = act.emoteId,
            IsPgc        = act.isPgc,
            UgcAnimId    = act.ugcData?.id ?? string.Empty,
            AudioUrl     = act.audioUrl,
            DelaySecond  = act.delaySecond,
            IsMute       = act.isMute,
            RandomResult = randomResult,
            Text         = act.text,
            Command      = (act as voiceCommands)?.command ?? string.Empty,
            BuddyName    = buddyName ?? string.Empty,
        };
    }

    private void OnCloseBtnClick()
    {
        CloseSelf();
    }

    /// <summary>从唤醒动作列表中随机取一个可用（disabled==0）的。</summary>
    private static characterInteraction PickRandomActivation(CabinCharacterUgcInfo info)
    {
        if (info?.activation == null) return null;
        var usable = info.activation.FindAll(a => a != null && a.disabled == 0);
        if (usable.Count == 0) return null;
        return usable[UnityEngine.Random.Range(0, usable.Count)];
    }

    /// <summary>
    /// 按 delaySecond 协调播放互动动作与语音（与编辑预览 BeginPreviewActivation 规则一致）：
    /// delaySecond >= 0 先播动作、延迟后播语音；delaySecond &lt; 0 先播语音、延迟后播动作。
    /// isMute 只静音动作自带音效，语音 audioUrl 始终播放。
    /// 唤醒动作（characterInteraction）和口令互动（voiceCommands）共用此逻辑。
    /// </summary>
    public static void PlayActivation(PlayerStateController ctrl, GameObject buddyGo, InteractSyncData data)
    {
        if (data == null) return;

        if (data.DelaySecond >= 0)
        {
            PlayActivationAnim(ctrl, data);
            if (!string.IsNullOrEmpty(data.AudioUrl) && buddyGo != null)
            {
                TimerManager.Inst.RunOnce("buddy_call_voice", data.DelaySecond,
                    () => AkSoundManager.Inst.PlayUGCAudioByUrl(data.AudioUrl, false, buddyGo, true, BuddyVoiceVolumeScale));
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(data.AudioUrl) && buddyGo != null)
                AkSoundManager.Inst.PlayUGCAudioByUrl(data.AudioUrl, false, buddyGo, true, BuddyVoiceVolumeScale);
            TimerManager.Inst.RunOnce("buddy_call_anim", Mathf.Abs(data.DelaySecond),
                () => PlayActivationAnim(ctrl, data));
        }
    }

    /// <summary>驱动 buddy 状态机播放互动动作。isMute==1 时动作自带音效静音。randomResult 由发起端统一传入。</summary>
    private static void PlayActivationAnim(PlayerStateController ctrl, InteractSyncData data)
    {
        if (ctrl == null) return;

        if (data.IsPgc == 1)
        {
            if (string.IsNullOrEmpty(data.EmoteId)) return;
            if (!ctrl.CanEnterState(PlayerState.SingleEmote)) return;
            ctrl.EnterState(PlayerState.SingleEmote, data.EmoteId, data.RandomResult, data.IsMute);
        }
        // TODO: UGC 唤醒/口令动作（data.UgcAnimId）局内播放入口待确认（UgcEmoteState 走玩家间同步，需构造 EmoteData）
    }

    private void ClearItems()
    {
        foreach (var item in _itemList)
        {
            if (item != null)
            {
                item.ClearData();
                Destroy(item.gameObject);
            }
        }
        _itemList.Clear();
        _selectedItem = null;
        _selectedInfo = null;
    }
}
