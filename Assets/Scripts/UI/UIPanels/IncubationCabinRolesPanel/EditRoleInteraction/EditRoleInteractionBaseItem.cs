using System;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Store;
using GameData.PgcData;
using UI.Manager;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

public class EditRoleInteractionBaseItem : MonoBehaviour
{
    public Button open_layer1_iconBtn;
    public Image open_layer1_iconImage;
    public Toggle open_layer1_toggle;
    public RemoteImageBehaviour open_layer1_remoteImage;

    //public Button open_layer2_addActionVoiceBtn;
    public GameObject open_layer2_hadAddNodeGo;
    public Button open_layer2_reproduceVoiceBtn;
    public Text open_layer2_voiceNameTxt;

    public Button open_layer3_addBtn;
    public Button open_layer3_minusBtn;
    public Text open_layer3_countTxt;
    public Button open_layer3_previewBtn;
    public Button open_layer3_deleteBtn;

    public Text common_titleTxt;
    public Button common_selectBtn;

    public Text txt_name;
    public Button btn_enable;
    public Button btn_disable;
    public Transform bg_disable;

    public Image img_bg;
    public Sprite sp_bg_normal;
    public Sprite sp_bg_disable;

    public Image[] img_bg2;
    public Sprite sp_bg_normal2;
    public Sprite sp_bg_disable2;

    public Text[] text_bg;
    public Color color_bg_normal;
    public Color color_bg_disable;

    public Text[] text_bg2;
    public Color color_bg_normal2;
    public Color color_bg_disable2;

    public Action onCommonSelectBtnClick;

    /// <summary>启用/禁用状态变化后的回调，由外部（EditRoleInteractionView）设置以刷新剩余数量文本</summary>
    public Action onEnabledChanged;

    protected Func<string>      _getEmoteId;
    protected Func<UgcIdleData> _getUgcData;
    protected Func<int>         _getDelaySecond;
    protected Action<int>       _setDelaySecond;
    protected Func<int>         _getIsMute;
    protected Action<int>       _setIsMute;
    protected Func<int>         _getDisabled;
    protected Action<int>       _setDisabled;
    protected Func<string>      _getText;

    protected Action<int, bool, Action<bool>>           _onModifyMute;
    protected Action<int, int, Action<bool>>            _onModifyDelaySecond;
    protected Action<int, GoodsData, Action<bool>>      _onModifyEmote;
    protected Action<int, string, string, Action<bool>> _onModifyTextId;
    protected Action<int, Action<bool>>                 _onDelete;
    protected Action                                    _onPreview;

    /// <summary>
    /// 子类设置此委托以控制"启用"按钮是否允许执行。
    /// 返回 true 表示可以启用；返回 false 时将弹出 _enableLimitMsg 提示并阻止操作。
    /// </summary>
    protected Func<bool> _canEnable;

    /// <summary>启用数量超限时的 Toast 提示内容，由子类设置</summary>
    protected string _enableLimitMsg;

    CabinCharacterBaseInfo characterUgcInfo;
    protected string _tokenID;
    protected string _itemLabel;
    protected int    _idx;
    protected bool   _isOpen;

    protected void Awake()
    {
        //open_layer1_iconBtn.onClick.AddListener(OnOpenLayer1IconBtnClick);
        //open_layer3_addBtn.onClick.AddListener(OnOpenLayer3AddBtnClick);
        //open_layer3_minusBtn.onClick.AddListener(OnOpenLayer3MinusBtnClick);
        //open_layer3_previewBtn.onClick.AddListener(OnOpenLayer3PreviewBtnClick);
        //open_layer3_deleteBtn.onClick.AddListener(OnOpenLayer3DeleteBtnClick);
        common_selectBtn.onClick.AddListener(OnCommonSelectBtnClick);
        //open_layer2_reproduceVoiceBtn.onClick.AddListener(OnOpenLayer2ReproduceVoiceBtnClick);
        btn_enable.onClick.AddListener(() => SetEnabled(true));
        btn_disable.onClick.AddListener(() => SetEnabled(false));
    }

    protected void InitCommon(CabinCharacterUgcInfo characterUgcInfo,string tokenID,string itemLabel, int idx, bool isOpen)
    {
        this.characterUgcInfo = characterUgcInfo;
        this._tokenID = tokenID;
        _itemLabel = itemLabel;
        _idx = idx;
        _isOpen = isOpen;
        common_titleTxt.text = itemLabel + ":" + (idx + 1);
        SetEnabled(_getDisabled() != 1, isInit: true);
        open_layer1_toggle.isOn = _getIsMute() == 1;
        open_layer3_countTxt.text = _getDelaySecond().ToString();

        // 动画名
        var ugcDataForName = _getUgcData();
        CabinTools.SetInteractEmoteNameText(
            txt_name,
            _getEmoteId(),
            ugcDataForName?.id,
            ugcDataForName?.cover);

        open_layer2_voiceNameTxt.text = _getText();

        open_layer1_iconImage.gameObject.SetActive(false);
        open_layer1_remoteImage.gameObject.SetActive(false);
        var ugcData = _getUgcData();
        bool isPgc = UniqueType.IsPgc(_getEmoteId());
        if (isPgc)
        {
            open_layer1_iconImage.gameObject.SetActive(true);
            open_layer1_iconImage.sprite = PgcUtils.GetIconSpriteByPgcId(_getEmoteId(), open_layer1_iconImage.gameObject);
        }
        else
        {
            if (ugcData != null)
            {
                open_layer1_remoteImage.Load(ugcData.cover, onCompleted: (bool fromCache, bool success) =>
                {
                    if (this == null) return;
                    open_layer1_iconImage.gameObject.SetActive(false);
                    open_layer1_remoteImage.gameObject.SetActive(true);
                });
            }
        }

        open_layer1_toggle.onValueChanged.RemoveAllListeners();
        open_layer1_toggle.onValueChanged.AddListener(OnOpenLayer1ToggleValueChanged);
    }


    void OnCommonSelectBtnClick()
    {
        _isOpen = !_isOpen;
        onCommonSelectBtnClick?.Invoke();
    }

    void SetEnabled(bool enabled, bool isInit = false)
    {
        // 启用时检查数量上限（仅限"启用"方向；禁用不受限；初始化恢复状态时跳过检查）
        if (enabled && !isInit && _canEnable != null && !_canEnable())
        {
            TipPanel.ShowToast(_enableLimitMsg ?? "已达启用数量上限");
            return;
        }

        btn_enable.gameObject.SetActive(!enabled);
        btn_disable.gameObject.SetActive(enabled);
        bg_disable.gameObject.SetActive(!enabled);
        if (img_bg != null)
        {
            img_bg.sprite = enabled ? sp_bg_normal : sp_bg_disable;
        }
        if (img_bg2 != null)
        {
            Sprite sp2 = enabled ? sp_bg_normal2 : sp_bg_disable2;
            foreach (var img in img_bg2)
            {
                if (img != null)
                {
                    img.sprite = sp2;
                }
            }
        }
        if (text_bg != null)
        {
            Color c = enabled ? color_bg_normal : color_bg_disable;
            foreach (var txt in text_bg)
            {
                if (txt != null)
                {
                    txt.color = c;
                }
            }
        }
        if (text_bg2 != null)
        {
            Color c2 = enabled ? color_bg_normal2 : color_bg_disable2;
            foreach (var txt in text_bg2)
            {
                if (txt != null)
                {
                    txt.color = c2;
                }
            }
        }
        _setDisabled(enabled ? 0 : 1);

        if (!isInit)
        {
            onEnabledChanged?.Invoke();
        }
    }

    void OnOpenLayer1ToggleValueChanged(bool isOn)
    {
        _setIsMute(isOn ? 1 : 0);
        _onModifyMute(_idx, isOn, (isSuccess) =>
        {
            if (isSuccess)
            {
                LoggerUtils.LogError("修改" + _itemLabel + "静音成功");
            }
            else
            {
                LoggerUtils.LogError("修改" + _itemLabel + "静音失败");
            }
        });
    }
}
