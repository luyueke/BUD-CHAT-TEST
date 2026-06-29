using Com.TheFallenGames.OSA.Util.IO;
using GameData.PgcData;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
public class RoleInteractNode2RoleItem : MonoBehaviour
{
    public Button open_layer1_iconBtn;
    public Image open_layer1_iconImage;
    public Toggle open_layer1_toggle;
    public RemoteImageBehaviour open_layer1_remoteImage;

    //public Button open_layer2_addActionVoiceBtn;
    public GameObject open_layer2_hadAddNodeGo;
    public Button open_layer2_reproduceVoiceBtn;
    public Text open_layer2_voiceNameTxt;
    // public Dropdown open_layer2_dropdown;

    public Button open_layer3_addBtn;
    public Button open_layer3_minusBtn;
    public Text open_layer3_countTxt;
    public Button open_layer3_previewBtn;
    public Button open_layer3_deleteBtn;

    public Text common_titleTxt;
    //public Button common_selectBtn;

    public Text txt_name;
    public GameObject Lock;

    int _idx = -1;
    public Action onCommonSelectBtnClick; //选择事件

    characterInteraction _characterInteraction;
    CabinCharacterBaseInfo characterUgcInfo;
    string _tokenID;

    void Awake()
    {
        open_layer3_previewBtn.onClick.AddListener(OnOpenLayer3PreviewBtnClick);
    }

    public void Init(CabinCharacterBaseInfo characterUgcInfo, characterInteraction characterInteraction, string tokenID, int idx, bool isOpen)
    {
        this.characterUgcInfo = characterUgcInfo;
        _tokenID = tokenID;
        _characterInteraction = characterInteraction;
        _idx = idx;
        common_titleTxt.text = "唤醒动作" + (idx + 1);
        // open_layer1_iconImage.sprite = PgcUtils.GetIconSpriteByPgcId(characterInteraction.emoteId, open_layer1_iconImage.gameObject);
        open_layer1_toggle.isOn = characterInteraction.isMute == 1;
        open_layer3_countTxt.text = characterInteraction.delaySecond.ToString();
        // common_titleTxt.text = characterInteraction.text;


        //bool hadSelect = !string.IsNullOrEmpty(characterInteraction.text);
        //open_layer2_addActionVoiceBtn.gameObject.SetActive(!hadSelect);
        //open_layer2_hadAddNodeGo.SetActive(hadSelect);
        // 动画名
        CabinTools.SetInteractEmoteNameText(
            txt_name,
            characterInteraction.emoteId,
            characterInteraction.ugcData?.id,
            characterInteraction.ugcData?.cover);

        // 语音包名
        open_layer2_voiceNameTxt.text = characterInteraction.text;

        open_layer1_iconImage.gameObject.SetActive(false);
        open_layer1_remoteImage.gameObject.SetActive(false);
        var ugcData = characterInteraction.ugcData;
        // bool isPgc = UniqueType.IsPgc(ugcData.id) && ugcData == null;
        bool isPgc = UniqueType.IsPgc(characterInteraction.emoteId);
        if (isPgc)
        {
            open_layer1_iconImage.gameObject.SetActive(true);
            open_layer1_iconImage.sprite = PgcUtils.GetIconSpriteByPgcId(characterInteraction.emoteId, open_layer1_iconImage.gameObject);
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

        open_layer2_voiceNameTxt.text = _characterInteraction.text;

        open_layer1_toggle.onValueChanged.RemoveAllListeners();
        open_layer1_toggle.onValueChanged.AddListener(OnOpenLayer1ToggleValueChanged);
    }





    /// <summary>
    /// 预览唤醒动作
    /// </summary>
    void OnOpenLayer3PreviewBtnClick()
    {
        if (_characterInteraction.ugcData == null && string.IsNullOrEmpty(_characterInteraction.emoteId))
        {
            TipPanel.ShowToast("请先选择动作");
            return;
        }
        //TODO:
        //CabinRolesNetManager.Inst.PreviewActivation(_characterInteraction);
        CabinRolesNetManager.Inst.PreviewActivation(_characterInteraction);
    }


    public void RefreshLock(string ugcId, string creator)
    {
        CabinSkinLockHelper.ApplyLock(Lock, ugcId, creator);
    }

    void OnOpenLayer1ToggleValueChanged(bool isOn)
    {
        _characterInteraction.isMute = isOn ? 1 : 0;
        CabinRolesNetManager.Inst.ModifyActivationMute(_idx, isOn, (isSuccess) =>
        {
            if (isSuccess)
            {
                LoggerUtils.LogError("修改唤醒动作静音成功");
            }
            else
            {
                LoggerUtils.LogError("修改唤醒动作静音失败");
            }
        });
    }
}
