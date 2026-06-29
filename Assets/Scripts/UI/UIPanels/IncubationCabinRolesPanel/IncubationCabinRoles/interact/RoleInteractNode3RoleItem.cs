using Com.TheFallenGames.OSA.Util.IO;
using GameData.PgcData;
using System;
using UI.Manager;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class RoleInteractNode3RoleItem : MonoBehaviour
{
    public TextInputView editCommandBox; //编辑口令

    public Button open_layer1_iconBtn;
    public Image open_layer1_iconImage;
    public RemoteImageBehaviour open_layer1_remoteImage;

    public Toggle open_layer1_toggle;
    //public Button open_layer2_addActionVoiceBtn;
    public GameObject open_layer2_hadAddNodeGo;
    public Button open_layer2_reproduceVoiceBtn;
    public Text open_layer2_voiceNameTxt;

    public Button open_layer3_addActionVoiceBtn;

    public Button open_layer3_reproduceVoiceBtn;

    public Button open_layer3_addBtn;
    public Button open_layer3_minusBtn;
    public Text open_layer3_countTxt;
    public Button open_layer3_previewBtn;
    public Button open_layer3_deleteBtn;

    public Text common_titleTxt;
    //public Button common_selectBtn;
    bool _isOpen = false;
    public Text txt_name;
    public GameObject Lock;

    int _idx = -1;
    public Action onCommonSelectBtnClick; //选择事件
    CabinCharacterBaseInfo characterUgcInfo;
    string _tokenID;
    voiceCommands _voiceCommands;
    void Awake()
    {
        open_layer3_previewBtn.onClick.AddListener(OnOpenLayer3PreviewBtnClick);
    }

    public void Init(CabinCharacterBaseInfo characterUgcInfo, voiceCommands voiceCommands, string tokenID, int idx, bool isOpen)
    {
        this.characterUgcInfo = characterUgcInfo;
        _tokenID = tokenID;
        _voiceCommands = voiceCommands;
        _idx = idx;
        _isOpen = isOpen;
        common_titleTxt.text = voiceCommands.command;
        editCommandBox.SetInputWithoutNotify(voiceCommands.command);
        editCommandBox.SetOnInput((input) =>
        {
            CabinRolesNetManager.Inst.ModifyVoiceCommandsCommand(_idx, input, (isSuccess) =>
            {
                if (isSuccess)
                {
                    LoggerUtils.Log("修改口令互动口令成功");
                }
                else
                {
                    LoggerUtils.LogError("修改口令互动口令失败");
                }
            });
        });
        // open_layer1_iconImage.sprite = PgcUtils.GetIconSpriteByPgcId(characterInteraction.emoteId, open_layer1_iconImage.gameObject);
        open_layer1_toggle.isOn = voiceCommands.isMute == 1;
        open_layer3_countTxt.text = voiceCommands.delaySecond.ToString();
        // common_titleTxt.text = characterInteraction.text;

        //bool hadSelect = !string.IsNullOrEmpty(voiceCommands.text);
        //open_layer2_addActionVoiceBtn.gameObject.SetActive(!hadSelect);
        // /open_layer2_hadAddNodeGo.SetActive(hadSelect);
        open_layer2_voiceNameTxt.text = voiceCommands.text;

        // 动画名
        CabinTools.SetInteractEmoteNameText(
            txt_name,
            voiceCommands.emoteId,
            voiceCommands.ugcData?.id,
            voiceCommands.ugcData?.cover);

        //下拉框
        //
        open_layer1_toggle.onValueChanged.RemoveAllListeners();
        open_layer1_toggle.onValueChanged.AddListener(OnOpenLayer1ToggleValueChanged);

        open_layer1_iconImage.gameObject.SetActive(false);
        open_layer1_remoteImage.gameObject.SetActive(false);
        var ugcData = voiceCommands.ugcData;
        // bool isPgc = UniqueType.IsPgc(ugcData.id) && ugcData == null;
        bool isPgc = UniqueType.IsPgc(voiceCommands.emoteId);
        if (isPgc)
        {
            open_layer1_iconImage.gameObject.SetActive(true);
            open_layer1_iconImage.sprite = PgcUtils.GetIconSpriteByPgcId(voiceCommands.emoteId, open_layer1_iconImage.gameObject);
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
    }


    /// <summary>
    /// 预览唤醒动作
    /// </summary>
    void OnOpenLayer3PreviewBtnClick()
    {
        //TODO:
        if (_voiceCommands.ugcData == null && string.IsNullOrEmpty(_voiceCommands.emoteId))
        {
            TipPanel.ShowToast("请先选择动作");
            return;
        }
        //TODO:
        CabinRolesNetManager.Inst.PreviewActivation(_voiceCommands);
    }

    public void RefreshLock(string ugcId, string creator)
    {
        CabinSkinLockHelper.ApplyLock(Lock, ugcId, creator);
    }

    void OnOpenLayer1ToggleValueChanged(bool isOn)
    {
        _voiceCommands.isMute = isOn ? 1 : 0;

        CabinRolesNetManager.Inst.ModifyVoiceCommandsMute(_idx, isOn, (isSuccess) =>
        {
            if (isSuccess)
            {
                LoggerUtils.Log("修改口令互动静音成功");
            }
            else
            {
                LoggerUtils.LogError("修改口令互动静音失败");
            }
        });
    }
}
