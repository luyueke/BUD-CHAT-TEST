using System;
using System.Collections;
using System.Collections.Generic;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorTypeSelectPanel : BasePanel<CreatorSeasonPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private List<Toggle> ToggleBtns;
    [SerializeField] private List<GameObject> SelectTitleObjs;
    [SerializeField] private CButton JoinBtn;

    private CreatorSelectType _currentSelectType = CreatorSelectType.All;
    private const string CreatorSelectTypeKey = "CreatorSeasonPanel_CreatorSelectType";
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(OnJoinBtnClick);
        JoinBtn.onClick.AddListener(OnJoinBtnClick);
        for(int i = 0; i < ToggleBtns.Count; i++)
        {
            int index = i;
            ToggleBtns[i].onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, index));
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        LoadLocalAndApplyToUI();
    }

    private void OnJoinBtnClick()
    {
        //UIManager.Inst.OpenPanelTakeAni(PanelId.GameHallStudiosPanel);
        SaveAndNotify();
        CloseSelf();
    }
    private void OnToggleValueChanged(bool isOn, int index)
    {
        if(SelectTitleObjs[index] != null)
        {
            SelectTitleObjs[index].SetActive(isOn);
        }
        if(isOn)
        {
            _currentSelectType = (CreatorSelectType)index;
        }
    }

    private void SaveAndNotify()
    {
        PlayerPrefs.SetInt(CreatorSelectTypeKey, (int)_currentSelectType);
        PlayerPrefs.Save();
        MessageHelper.Broadcast(MessageName.OnCreatorSelectTypeChanged, (int)_currentSelectType);
    }

    private void LoadLocalAndApplyToUI()
    {
        var saved = PlayerPrefs.GetInt(CreatorSelectTypeKey, (int)CreatorSelectType.All);
        if (saved < 0 || saved >= Enum.GetValues(typeof(CreatorSelectType)).Length)
        {
            saved = (int)CreatorSelectType.All;
        }
        _currentSelectType = (CreatorSelectType)saved;

        if (ToggleBtns != null && ToggleBtns.Count > 0)
        {
            for (int i = 0; i < ToggleBtns.Count; i++)
            {
                if (ToggleBtns[i] == null) continue;
                ToggleBtns[i].SetIsOnWithoutNotify(i == (int)_currentSelectType);
                SelectTitleObjs[i].SetActive(i == (int)_currentSelectType);
            }
        }
    }
}
