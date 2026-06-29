using System;
using Es;
using Game.Audio;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UgcAnimPgcToneItem : MonoBehaviour
{
    public Text Txt_Title;
    public CButton Btn_Select;
    public GameObject Go_Selected;
    public GameObject Go_Normal;
    public GameObject Go_TryPlay;
    public GameObject Go_VipTag;

    private UgcAnimBgmConfig _curConfig;
    private AnimMusicInfo _curUgcInfo;
    private Action<AnimMusicInfo> _onItemSelect;

    private void Awake()
    {
        Btn_Select.onClick.AddListener(OnToneItemSelect);
        Go_Normal.SetActive(true);
        Go_TryPlay.SetActive(false);
    }

    public string GetPgcId()
    {
        return _curConfig?.pgcId;
    }

    private void OnToneItemSelect()
    {
        if (!UgcAnimVipChecker.Inst.CanAddVipTone(this._curConfig))
            return;
        
        this._onItemSelect?.Invoke(_curUgcInfo);
        SetSelectState(true);
        Preview();
    }

    public void InitSelectMode(UgcAnimBgmConfig config, Action<AnimMusicInfo> act)
    {
        this._curConfig = config;
        this._curUgcInfo = UgcAnimToneUtils.CovertPgcConfigToUgcInfo(config.pgcId);
        this._onItemSelect = act;
        Btn_Select.gameObject.SetActive(true);
        Txt_Title.SetLocalText(config.toneName);
        Go_VipTag.gameObject.SetActive(config.isVip == 1);
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
        UgcAnimToneManager.Inst.PreviewTone(this._curUgcInfo);
    }
}