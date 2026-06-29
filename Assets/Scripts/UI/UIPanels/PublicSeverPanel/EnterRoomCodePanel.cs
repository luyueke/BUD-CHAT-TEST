using Game.Base;
using Game.GameSync;
using GameData;
using GameData.UGCData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class EnterRoomCodePanel : BasePanel<EnterRoomCodePanel>
{
    public Transform BG;
    public LoadingButton Btn_Confirm;
    public CButton Btn_Return;
    public Image Img_Confirm;
    public CreateAndPublishEditBox EditBox;
    private string unEnableColor = "#D9D9D9";
    private string enableColor = "#FFD400";
    private string _curRoomCode;

    public override void OnCreate()
    {
        base.OnCreate();
        Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/AvatarBg.prefab").Instantiate(BG);
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
        Btn_Return.onClick.AddListener(CloseSelf);
        EditBox.SetAfterTextChangeAction(AfterTextChangeAction);
        EditBox.InitUI("输入房间码");
        Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(unEnableColor);
        Btn_Confirm.SetClickAble(false);
    }

    public void AfterTextChangeAction(string roomCode)
    {
        Btn_Confirm.SetClickAble(!string.IsNullOrEmpty(_curRoomCode));

        _curRoomCode = roomCode;
        var btnColor = string.IsNullOrEmpty(_curRoomCode) ? unEnableColor : enableColor;
        Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(btnColor);
    }

    private void OnBtnConfirmClick()
    {
        Btn_Confirm.ShowLoading();

        GameSyncHttpHelper.Inst.EnterRoomByRoomCode(_curRoomCode, OnEnterRoomSuccess, OnEnterRoomFail);
    }

    private void OnEnterRoomSuccess(UgcInfoRsp rspData)
    {
        if(this == null)
            return;

        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(rspData.mapInfo, rspData.creator, LoadingType.Map);
        // GameController.StartGame(EnterGameModel.GuestScene, rspData.mapInfo);
        GameController.StartGuestGame(rspData.mapInfo,rspData.creator,rspData.interactInfo);
        Btn_Confirm.HideLoading();
    }

    private void OnEnterRoomFail()
    {
        if(this == null)
            return;

        Btn_Confirm.HideLoading();
    }
}
