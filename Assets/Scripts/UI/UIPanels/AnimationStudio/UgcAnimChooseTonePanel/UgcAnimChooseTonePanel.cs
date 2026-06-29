using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UgcAnimChooseTonePanel : BasePanel<UgcAnimChooseTonePanel>
{
    public CButton Btn_Close;
    public CButton Btn_Confirm;
    public Toggle Tog_Pgc;
    public Toggle Tog_Created;
    public Toggle Tog_Owned;

    public UgcAnimPgcToneInfoPanel PgcTonePanel;
    public UgcAnimUgcToneInfoPanel CreatedPanel;
    public UgcAnimUgcToneInfoPanel OwnedPanel;

    private AnimMusicInfo _curChooseBgmMusicInfo = new AnimMusicInfo();
    private ChooseUgcAnimBgmData panelData;
    public enum ToneStudioType
    {
        PGC = 0,
        Created = 1,
        Owned = 2,
    }
    public override void OnCreate()
    {
        base.OnCreate();
        AddListener();
        Tog_Pgc.isOn = true;
        OnSelectView(ToneStudioType.PGC);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        panelData = (ChooseUgcAnimBgmData)args[0];
    }

    private void AddListener()
    {
        PgcTonePanel.SetOnToneItemSelectAct(OnMusicItemClick);
        CreatedPanel.SetOnToneItemSelectAct(OnMusicItemClick);
        OwnedPanel.SetOnToneItemSelectAct(OnMusicItemClick);
        
        Btn_Close.onClick.AddListener(DoClose);
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
        Tog_Pgc.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
                OnSelectView(ToneStudioType.PGC);
        });
        Tog_Created.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
                OnSelectView(ToneStudioType.Created);
        });
        Tog_Owned.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
                OnSelectView(ToneStudioType.Owned);
        });
    }

    private void DoClose()
    {
        UgcAnimToneManager.Inst.StopPreviewTone();
        CloseSelf();
    }

    private void OnMusicItemClick(AnimMusicInfo info)
    {
        _curChooseBgmMusicInfo = info;
        
        if(_curChooseBgmMusicInfo != null)
            Btn_Confirm.gameObject.SetActive(true);
    }

    private void OnBtnConfirmClick()
    {
        var trackId = panelData.trackId;
        var act = panelData.onChooseBgm;
        act?.Invoke(trackId, _curChooseBgmMusicInfo);
        
        DoClose();
    }

    private void OnSelectView(ToneStudioType studioType)
    {
        switch (studioType)
        {
            case ToneStudioType.PGC:
                PgcTonePanel.gameObject.SetActive(true);
                CreatedPanel.gameObject.SetActive(false);
                OwnedPanel.gameObject.SetActive(false);
                break;
            
            case ToneStudioType.Created:
                PgcTonePanel.gameObject.SetActive(false);
                CreatedPanel.gameObject.SetActive(true);
                OwnedPanel.gameObject.SetActive(false);
                CreatedPanel.GetPublishedData();
                break;
            
            case ToneStudioType.Owned:
                PgcTonePanel.gameObject.SetActive(false);
                CreatedPanel.gameObject.SetActive(false);
                OwnedPanel.gameObject.SetActive(true);
                OwnedPanel.GetPublishedData();
                break;

        }
    }

    public void RefreshOwnedList()
    {
        if (Tog_Owned.isOn)
        {
            OnSelectView(ToneStudioType.Owned);
        }
        else
        {
            Tog_Owned.isOn = true;
        }
    }

    public string GetCurToneId()
    {
        return _curChooseBgmMusicInfo?.id;
    }
}

public class ChooseUgcAnimBgmData
{
    public int trackId;
    public Action<int, AnimMusicInfo> onChooseBgm;
}
