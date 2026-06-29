using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Audio;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class VehiclePgcToneItem : MonoBehaviour
{
    public Text Txt_Title;
    public CButton Btn_Select;
    public GameObject Go_Selected;
    public GameObject Go_Normal;
    public GameObject Go_TryPlay;
    public GameObject Go_VipTag;

    private VehicleAudioData _curUgcInfo;
    private Action<VehicleAudioData> _onItemSelect;
    private GameObject _globalSoundObj;

    private void Awake()
    {
        _globalSoundObj = GameObject.Find("GlobalMainCamera");
        Btn_Select.onClick.AddListener(OnToneItemSelect);
        Go_Normal.SetActive(true);
        Go_TryPlay.SetActive(false);
    }

    private void OnToneItemSelect()
    {
        this._onItemSelect?.Invoke(_curUgcInfo);
        SetSelectState(true);
        Preview();
    }

    public void InitSelectMode(VehicleAudioData data, Action<VehicleAudioData> act)
    {
        //this._curUgcInfo = UgcAnimToneUtils.CovertPgcConfigToUgcInfo(config.pgcId);
        this._curUgcInfo = data;
        this._onItemSelect = act;
        Btn_Select.gameObject.SetActive(true);
        Txt_Title.SetLocalText(data.name);
        Go_VipTag.gameObject.SetActive(false);
    }

    public void SetSelectState(bool isSelect, bool playAnim = true)
    {
        Go_Selected.SetActive(isSelect);

        if (playAnim)
        {
            Go_Normal.SetActive(!isSelect);
            Go_TryPlay.SetActive(isSelect);
        }
        else
        {
            Go_Normal.SetActive(true);
            Go_TryPlay.SetActive(false);
        }
    }

    private void Preview()
    {
        StopPlayAudio();
        AkSoundManager.Inst.PlaySound(_curUgcInfo.wwiseInfo.group, _curUgcInfo.wwiseInfo.switchs, _curUgcInfo.wwiseInfo.wwise, _globalSoundObj);
    }

    private void StopPlayAudio()
    {
        AkSoundManager.Inst.StopGameMusicNode();
        AkSoundManager.Inst.StopSound(_curUgcInfo.wwiseInfo.stopWwise, _globalSoundObj);
    }
}
