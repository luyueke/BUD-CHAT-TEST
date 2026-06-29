using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class ContestCreateOcPanel : BasePanel<ContestCreateOcPanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private LoadingButton DoneBtn;
    [SerializeField] private ContestCreateOcEntry OcEntry;

    public Action JoinSuccessAction;
    private AvatarOcFixData cOcData;
    public override void OnCreate()
    {
        BackBtn?.onClick.AddListener(() =>
        {
            CloseSelf();
        });

        DoneBtn?.onClick.AddListener(onClickDone);
        ChangeDoneButtonState(false);

    }

    private ContestInfo contestInfo;
    public override void OnShow(params object[] args)
    {
        ContestInfo contestInfo = args[0] as ContestInfo;
        if (contestInfo == null)
        {
            return;
        }
        this.contestInfo = contestInfo;

        bool isPet = contestInfo.CurrentContestType == BUDContestType.PetOC;
        OcEntry.SetActions(isPet, onSelectItemAct, null);
        OcEntry.GetFirstPageDatas(null);
    }

    public override void OnWindowBeFocused()
    {
        OcEntry.GetFirstPageDatas(null);
    }

    private void onClickDone()
    {
        if (cOcData == null)
        {
            return;
        }

        var ocId = cOcData?.ocInfo?.ocId;
        if (ocId == ContestCreateOcRequester.gAddOcKey)
        {
            return;
        }

        var contestId = contestInfo?.contestId;
        if (string.IsNullOrEmpty(contestId))
        {
            return;
        }

        DoneBtn.ShowLoading();
        ContestDataManager.Inst.JoinContest(ocId, new List<string>() { contestId }, b =>
        {
            if (this == null)
            {
                return;
            }
            DoneBtn.HideLoading();
            if (b)
            {
                CloseSelf();
                JoinSuccessAction?.Invoke();
            }
        });
    }

    private void onSelectItemAct(bool isAdd, AvatarOcFixData ocData)
    {
        if (isAdd)
        {
            ResetSelect();
            cOcData = null;
            ChangeDoneButtonState(false);
            bool isPet = contestInfo.CurrentContestType == BUDContestType.PetOC;
            UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, isPet);
            return;
        }

        if (cOcData != null && cOcData.OcId == ocData.OcId)
        {
            return;
        }

        ResetSelect();
        cOcData = ocData;
        cOcData.isSelect = true;
        OcEntry.UpdateSingleItem(cOcData);
        ChangeDoneButtonState(true);
    }

    private void ChangeDoneButtonState(bool isEnable)
    {
        var spriteName = isEnable ? "Yellow_Btn_3" : "Gray_Btn_3";
        DoneBtn.image.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
    }

    private void ResetSelect()
    {
        if (cOcData == null)
        {
            return;
        }

        cOcData.isSelect = false;
        OcEntry.UpdateSingleItem(cOcData);
    }
}
