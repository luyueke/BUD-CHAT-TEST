using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Avatar;
using Game.MusicalInstrument;
using Game.Store;
using GameData.PgcData;
using System;
using UI.Manager;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseInteractRoleItem<TData> : MonoBehaviour where TData : class
{
    public GameObject openGo;
    public GameObject closeGo;

    public GameObject switchGo;
    public GameObject addGo;

    public GameObject[] hasDataObj;
    public Text iconName;
    public Button open_layer1_iconBtn;
    public Image open_layer1_iconImage;
    public Toggle open_layer1_toggle;
    public RemoteImageBehaviour open_layer1_remoteImage;

    public Button open_layer2_addActionVoiceBtn;
    public GameObject open_layer2_hadAddNodeGo;
    public Button open_layer2_reproduceVoiceBtn;
    public Button open_layer2_previewVoiceBtn;
    public Text open_layer2_voiceNameTxt;

    public Button open_layer3_addBtn;
    public Button open_layer3_minusBtn;
    public Text open_layer3_countTxt;
    public Button open_layer3_previewBtn;
    public Button open_layer3_deleteBtn;

    public Text common_titleTxt;
    public Button common_selectOffBtn;
    public Button common_selectOnBtn;

    protected bool _isOpen;
    protected int _idx = -1;
    protected TData _data;
    protected int _maxDelaySecond = -1; // -1 表示无上限
    private const int MinDelaySecond = -5; // 延迟最小值：允许语音比动作提前 5 秒触发

    protected CabinCharacterBaseInfo _characterUgcInfo;
    protected string _TokenID;

    public Action onCommonSelectBtnClick;
    public Action<GoodsData, Action> onSelectEmote;
    public Action<TData> onPreviewAction;
    public Action<string, string> onAudioChanged;
    public Action<int> onDelayChanged;
    public Action<bool> onMuteChanged;
    public Action onDeleteConfirm;

    // --- 抽象：数据访问 ---
    protected abstract string ItemTitle { get; }
    protected abstract string DataEmoteId { get; }
    protected abstract bool DataHasUgcData { get; }
    protected abstract string DataUgcCoverUrl { get; }
    protected abstract int DataIsMute { get; set; }
    protected abstract string DataAudioUrl { get; }
    protected abstract int DataDelaySecond { get; set; }
    protected abstract string DataText { get; }

    // --- 抽象：网络调用（子类各自实现，内部包含日志） ---
    protected abstract void NetPreview();
    protected virtual void SetupEmotePanelAction(IncubationCabinEmotePopPanel panel)
    {
        panel.onConfirmAction = (goodsDatas, onDone) =>
        {
            DataDelaySecond = 0;
            onSelectEmote?.Invoke(goodsDatas[0], onDone);
        };
    }

    // --- 抽象：仅修改本地内存，不发网络请求 ---
    protected abstract void LocalDelete();
    protected abstract void LocalModifyTextId(string text, string url);

    // --- 虚 Hook：默认空实现，子类按需 override ---
    protected virtual void OnAwakeExtra() { }
    protected virtual void OnInitExtra(TData data) { }

    void Awake()
    {
        open_layer1_iconBtn.onClick.AddListener(OnOpenLayer1IconBtnClick);
        open_layer3_addBtn.onClick.AddListener(OnOpenLayer3AddBtnClick);
        open_layer3_minusBtn.onClick.AddListener(OnOpenLayer3MinusBtnClick);
        open_layer3_previewBtn.onClick.AddListener(OnOpenLayer3PreviewBtnClick);
        open_layer3_deleteBtn.onClick.AddListener(OnOpenLayer3DeleteBtnClick);
        common_selectOffBtn.onClick.AddListener(OnCommonSelectBtnClick);
        common_selectOnBtn.onClick.AddListener(OnCommonSelectBtnClick);
        open_layer2_addActionVoiceBtn.onClick.AddListener(OnAddActionVoiceBtnClick);
        open_layer2_reproduceVoiceBtn.onClick.AddListener(OnAddActionVoiceBtnClick);
        open_layer2_previewVoiceBtn.onClick.AddListener(OnOpenLayer2PreviewVoiceBtnClick);
        OnAwakeExtra();
    }

    public void Init(CabinCharacterBaseInfo characterUgcInfo,string tokenID, TData data, int idx, bool isOpen)
    {
        this._characterUgcInfo = characterUgcInfo;
        _TokenID = tokenID;
        _data = data;
        _idx = idx;
        _isOpen = isOpen;
        openGo.SetActive(isOpen);
        closeGo.SetActive(!isOpen);
        common_titleTxt.text = ItemTitle + (idx + 1);

        OnInitExtra(data);

        open_layer1_toggle.isOn = DataIsMute == 1;
        open_layer3_countTxt.text = DataDelaySecond.ToString();

        bool hadSelect = !string.IsNullOrEmpty(DataText);
        open_layer2_addActionVoiceBtn.gameObject.SetActive(!hadSelect);
        open_layer2_hadAddNodeGo.SetActive(hadSelect);
        open_layer2_voiceNameTxt.text = DataText;

        open_layer1_iconImage.gameObject.SetActive(false);
        open_layer1_remoteImage.gameObject.SetActive(false);

        bool isNullData = string.IsNullOrEmpty(DataEmoteId) && string.IsNullOrEmpty(DataUgcCoverUrl);
        switchGo?.SetActive(!isNullData);
        addGo?.SetActive(isNullData);
        foreach (var item in hasDataObj)
        {
            item.SetActive(!isNullData);
        }
        bool isPgc = UniqueType.IsPgc(DataEmoteId);
        if (isPgc)
        {
            open_layer1_iconImage.gameObject.SetActive(true);
            open_layer1_iconImage.sprite = PgcUtils.GetIconSpriteByPgcId(DataEmoteId, open_layer1_iconImage.gameObject);
            string iconNameStr = PgcUtils.GetEmoteName(DataEmoteId) ?? string.Empty;
            iconName.text = iconNameStr.Length > 6 ? iconNameStr.Substring(0, 5) + "…" : iconNameStr;
        }
        else
        {
            string ugcCoverUrl = DataUgcCoverUrl;
            if (ugcCoverUrl != null)
            {
                open_layer1_remoteImage.Load(ugcCoverUrl, onCompleted: (bool fromCache, bool success) =>
                {
                    if (this == null) return;
                    open_layer1_iconImage.gameObject.SetActive(false);
                    open_layer1_remoteImage.gameObject.SetActive(true);
                });
            }
            string iconNameStr = DataText ?? string.Empty;
            iconName.text = iconNameStr.Length > 6 ? iconNameStr.Substring(0, 5) + "…" : iconNameStr;
        }

        open_layer1_toggle.onValueChanged.RemoveAllListeners();
        open_layer1_toggle.onValueChanged.AddListener(OnOpenLayer1ToggleValueChanged);
    }

    void OnOpenLayer2PreviewVoiceBtnClick()
    {
        if (string.IsNullOrEmpty(DataAudioUrl))
        {
            TipPanel.ShowToast("暂无语音");
            return;
        }

        var soundObj = GameObject.Find("GlobalMainCamera");
        var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
        // 先停止上一次的语音，防止多次点击叠加播放
        ugcToneLoader.Stop();
        ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, DataAudioUrl);
    }

    void OnAddActionVoiceBtnClick()
    {
        //如果是角色编辑就直接用角色的，如果是皮肤就用传入的。
        string tokenID =  _TokenID;
        if (_characterUgcInfo is CabinCharacterUgcInfo ugcInfo)
        {
            tokenID = ugcInfo.toneId;
        }
        var panel = UIManager.Inst.OpenPanel<IncubationCabinPopPanel>(PanelId.IncubationCabinPopPanel, _characterUgcInfo, tokenID);
        panel.SetData(PopType.BigWin1);
        panel.onDoubaoAsrResultCb = (url, text) =>
        {
            if (onAudioChanged != null) { onAudioChanged.Invoke(text, url); return; }
            LocalModifyTextId(text, url);
        };
        panel.onDoubaoBatchPreviewResultCb = (data) =>
        {
            if (data == null || data.list == null || data.list.Count == 0) { Debug.LogError("语音包生成失败"); return; }
            var text = data.list[0].text;
            var url = data.list[0].url;
            if (onAudioChanged != null) { onAudioChanged.Invoke(text, url); return; }
            LocalModifyTextId(text, url);
        };
    }

    protected virtual void OnOpenLayer1IconBtnClick()
    {
        var caracterData = CharacterData.DeserializeObject(_characterUgcInfo.skinPack?[0].avatarJson);
        var panel = UIManager.Inst.OpenPanel<IncubationCabinEmotePopPanel>(PanelId.IncubationCabinEmotePopPanel, false, EmoteTabType.Official | EmoteTabType.Community, caracterData, false, 3);
        SetupEmotePanelAction(panel);
    }

    public void SetMaxDelaySecond(int max)
    {
        _maxDelaySecond = max;

        if (max < 0 || DataDelaySecond <= max)
            return;

        DataDelaySecond = max;
        open_layer3_countTxt.text = DataDelaySecond.ToString();
        onDelayChanged?.Invoke(DataDelaySecond);
    }

    void OnOpenLayer3AddBtnClick()
    {
        if (_maxDelaySecond >= 0 && DataDelaySecond >= _maxDelaySecond)
        {
            TipPanel.ShowToast($"延迟最多 {_maxDelaySecond} 秒，不能超过动画时长");
            return;
        }
        DataDelaySecond++;
        open_layer3_countTxt.text = DataDelaySecond.ToString();
        onDelayChanged?.Invoke(DataDelaySecond);
    }

    void OnOpenLayer3MinusBtnClick()
    {
        if (DataDelaySecond <= MinDelaySecond)
        {
            TipPanel.ShowToast($"延迟最少 {MinDelaySecond} 秒，不能再减少");
            return;
        }
        DataDelaySecond--;
        open_layer3_countTxt.text = DataDelaySecond.ToString();
        onDelayChanged?.Invoke(DataDelaySecond);
    }

    void OnOpenLayer3PreviewBtnClick()
    {
        if (!DataHasUgcData && string.IsNullOrEmpty(DataEmoteId))
        {
            TipPanel.ShowToast("请先选择动作");
            return;
        }
        NetPreview();
    }

    protected virtual void OnOpenLayer3DeleteBtnClick()
    {
        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == DataEmoteId);
        if (emoAniDataList == null || emoAniDataList.Count == 0)
        {
            // return;
        }
        string tipStr = string.Format("确认删除【{0}】吗？", ItemTitle + (_idx + 1));
        CommonBoxConfirmWithTitlePanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
        commonConfirmPanel.SetLocalText("提示", tipStr, "确认", "取消");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            if (onDeleteConfirm != null) { onDeleteConfirm.Invoke(); return; }
            LocalDelete();
        }, () => { });
        commonConfirmPanel.HideCloseBtn();
    }

    protected void OnCommonSelectBtnClick()
    {
        _isOpen = !_isOpen;
        openGo.SetActive(_isOpen);
        closeGo.SetActive(!_isOpen);
        onCommonSelectBtnClick?.Invoke();
    }

    void OnOpenLayer1ToggleValueChanged(bool isOn)
    {
        DataIsMute = isOn ? 1 : 0;
        onMuteChanged?.Invoke(isOn);
    }
}
