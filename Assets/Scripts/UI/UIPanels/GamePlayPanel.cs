using Game.Avatar;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using Message;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayPanel : BaseGamePlayPanel<GamePlayPanel>
{
    [SerializeField] private CButton m_returnBtn;
    [SerializeField] private CButton retryBtn;
    [SerializeField] private CButton screenShotBtn;
    [SerializeField] private Button unlinkedBtn;
    [SerializeField] private Button getOutVehicleBtn;

    public override void OnCreate()
    {
        base.OnCreate();
        m_returnBtn.onClick.AddListener(ChangeEditMode);
        retryBtn.onClick.AddListener(RetryBtnClick);
        screenShotBtn.onClick.AddListener(ScreenShotBtnClick);
        screenShotBtn.gameObject.SetActive(false);

        // 试玩态：与地图 buddy 牵手 / 载具的退出按钮，复用正式玩法的解牵手、下车入口（试玩无房间，本地直接生效）
        if (unlinkedBtn != null)
        {
            unlinkedBtn.onClick.AddListener(OnUnlinkBtnClick);
            unlinkedBtn.gameObject.SetActive(false);
        }
        if (getOutVehicleBtn != null)
        {
            getOutVehicleBtn.onClick.AddListener(OnGetOutVehicleBtnClick);
            getOutVehicleBtn.gameObject.SetActive(false);
        }

        MessageHelper.AddListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnBuddyLinkEmoteStateChange);
        MessageHelper.AddListener<string, string>(MessageName.OnSelfVehicleCreated, OnSelfVehicleCreated);
        MessageHelper.AddListener<string, string, Vector3>(MessageName.OnSelfPgcVehicleCreated, OnSelfPgcVehicleCreated);
        MessageHelper.AddListener<string>(MessageName.OnPlayerGetOutVehicle, OnPlayerGetOutVehicle);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnBuddyLinkEmoteStateChange);
        MessageHelper.RemoveListener<string, string>(MessageName.OnSelfVehicleCreated, OnSelfVehicleCreated);
        MessageHelper.RemoveListener<string, string, Vector3>(MessageName.OnSelfPgcVehicleCreated, OnSelfPgcVehicleCreated);
        MessageHelper.RemoveListener<string>(MessageName.OnPlayerGetOutVehicle, OnPlayerGetOutVehicle);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    // 解牵手：与正式玩法 GameGuestPanel.OnUnlinkEmoteBtnClick 同一入口（buddy 牵手走 BuddyLinkEmoteManager）
    private void OnUnlinkBtnClick()
    {
        var selfCtrl = AvatarController.Inst.SelfStateController;
        if (selfCtrl == null || selfCtrl.linkEmoteData == null) return;
        if (BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid))
            BuddyLinkEmoteManager.Inst.SendExitLinkReq(selfCtrl.linkEmoteData);
        else
            LinkEmoteManager.Inst.SendExitLinkReq(selfCtrl.linkEmoteData);
    }

    // 下车 / 收车：广播玩家尝试下车，GameVehicleManager 据此 DiscardVehicle（连带把 buddy 卸下）
    private void OnGetOutVehicleBtnClick()
    {
        MessageHelper.Broadcast(MessageName.OnPlayerTryGetOutVehicle, AccountDataManager.Inst.Uid);
    }

    private void OnBuddyLinkEmoteStateChange(bool isLink)
    {
        if (unlinkedBtn == null) return;
        bool show = isLink && BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid);
        unlinkedBtn.gameObject.SetActive(show);
    }

    private void OnSelfVehicleCreated(string uid, string inputPrefabPath)
    {
        if (getOutVehicleBtn != null && AccountDataManager.Inst.IsMySelf(uid))
            getOutVehicleBtn.gameObject.SetActive(true);
    }

    private void OnSelfPgcVehicleCreated(string uid, string inputPrefabPath, Vector3 cameraOffset)
    {
        if (getOutVehicleBtn != null && AccountDataManager.Inst.IsMySelf(uid))
            getOutVehicleBtn.gameObject.SetActive(true);
    }

    private void OnPlayerGetOutVehicle(string uid)
    {
        if (getOutVehicleBtn != null && AccountDataManager.Inst.IsMySelf(uid))
            getOutVehicleBtn.gameObject.SetActive(false);
    }

    private void ChangeEditMode()
    {
        GameController.ChangeMode(GameMode.Edit, () =>
        {
            CloseSelf();
            UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.OpenPanel(PanelId.GameEditModePanel);
        });
    }

    public void OnSwitchToEdit()
    {
        ChangeEditMode();
    }
}
