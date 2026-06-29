using System.Collections.Generic;
using frame8.Logic.Misc.Other.Extensions;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using GameData.Manager;
using GameSync.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Pb.Game;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class RoomMenuPanel : BasePanel<RoomMenuPanel>
{
    [SerializeField] private CButton intiveBtn;
    [SerializeField] private CButton leaveRoomBtn;
    [SerializeField] private CButton spawnBtn;
    [SerializeField] private GameObject headItemPrefab;
    [SerializeField] private Transform PlayersScrollContent;
    [SerializeField] private CButton closeBtn;
    [SerializeField] private Toggle tog_Room;
    [SerializeField] private Toggle tog_Setting;
    [SerializeField] private GameObject RoomPanel;
    [SerializeField] private GameObject SettingPanel;
    
    private enum RoomPanelType
    {
        PlayerList,
        Setting,
    }

    private List<HeadItem> mHeadItemList = new List<HeadItem>();

    public override void OnCreate()
    {
        base.OnCreate();
        headItemPrefab.SetActive(false);
        intiveBtn.onClick.AddListener(OnInviteBtnClick);
        leaveRoomBtn.onClick.AddListener(OnLeaveRoomBtnClick);
        spawnBtn.onClick.AddListener(OnSpawnBtnClick);
        closeBtn.onClick.AddListener(CloseSelf);
        tog_Room.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
                ShowPanelByType(RoomPanelType.PlayerList);
            }
        });
        tog_Setting.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
                ShowPanelByType(RoomPanelType.Setting);
            }
        });
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        //TODO:@JayWill 待优化，可以选择出不同的再删除节点，不用每次都全部重新删除
        InitData();//先用本地数据刷新，再请求网络数据
        // ClientManager.Inst.GetServerPlayerInfos(()=>{
        //     InitData();
        // });
        // InitData();
    }
    
    

    private void InitData()
    {
        InitPlayers();
    }
    
    private void InitPlayers()
    {
        var playerIds = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoIds();
        foreach(var item in PlayersScrollContent.GetChildren())
        {
            Destroy(item.gameObject);
        }
        
        
        mHeadItemList.Clear();

        HeadItem myItem = null;
        string playIDsStirng = string.Join(",", playerIds);
        BatchInfoReq batchInfoReq = new BatchInfoReq()
        {
            uidList = playIDsStirng
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.batchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(batchInfoReq),
            onReceive: msg =>
            {

                BatchInfoResponse batchInfoResponse =
                    JsonConvert.DeserializeObject<BatchInfoResponse>(msg);
                if (batchInfoResponse != null)
                {
                    foreach(var playerInfo in batchInfoResponse.list)
                    {
                        AccountUserInfo accountUserInfo = playerInfo.userInfo;
                        RelationShipInfo relationShipInfo = playerInfo.relationShipInfo;
                        BanStateInfo banStateInfo = playerInfo.banState;
                        if (banStateInfo != null)
                        {
                            MessageHelper.Broadcast<string, bool>(
                                MessageName.OnPlayerAudioBanChanged,
                                accountUserInfo.uid,
                                banStateInfo.isAudioBan == 1
                            );
                        }
                        var headItemNode = Instantiate(headItemPrefab,PlayersScrollContent);
                        var headItem = headItemNode.GetComponentInChildren<HeadItem>(true);
                        headItem.gameObject.SetActive(true);
                        headItem.SetNick(accountUserInfo.nickname);
                        headItem.SetHeadUrl(accountUserInfo);
                        if (relationShipInfo != null)
                        {
                            headItem.SetRelation(accountUserInfo.uid, relationShipInfo);
                        }
                        headItem.onClick.AddListener(OnItemClick);
                        headItem.Uid = accountUserInfo.uid;
                        if(accountUserInfo.uid == AccountDataManager.Inst.Uid)
                        {
                            myItem = headItem;
                        }
                        else
                        {
                            if (banStateInfo != null)
                            {
                                headItem.SetTextBan(banStateInfo.isChatBan == 1);
                                headItem.SetVoiceBan(banStateInfo.isAudioBan == 1);
                                // headItem.SetTextBan(true);
                                // headItem.SetVoiceBan(true);
                                headItem.onTextBan.AddListener(OnTextBanClick);
                                headItem.onVoiceBan.AddListener(OnVoiceBanClick);
                            }
                        }
                        mHeadItemList.Add(headItem);
                    }  
                }
     
                
               
            }, onFail: arg0 =>
            {
                LoggerUtils.LogError(arg0);
            });

    }

    private void OnItemClick(HeadItem headItem)
    {
        string uid = headItem.Uid;
        ProfilePanel profilePanel = UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
    }
    private void OnTextBanClick(HeadItem headItem)
    {
        SendBan(headItem.Uid,headItem.isTextBan?1:2, 1);
    }
    private void OnVoiceBanClick(HeadItem headItem)
    {
        MessageHelper.Broadcast<string, bool>(MessageName.OnPlayerAudioBanChanged, headItem.Uid, headItem.isVoiceBan);
        SendBan(headItem.Uid,headItem.isVoiceBan?1:2, 2);
    }
    private void SendBan( string uid,int setType,int banType)
    {
        SetBanNetData netData = new SetBanNetData();
        netData.SetType = setType;
        netData.ToUid = uid;
        netData.BanType = banType;
        NetSyncManager.Inst.Send(SubCmdType.SetBan,netData);
    }
    private void ExitGuest()
    {   
        GameController.ExitGame(() =>
        {
            GameDataManager.Inst.gameOnlineData.Clear();
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
            UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.BackToLastWindow();
            MarketReviewManager.Inst.CheckExitGameIsShowMarketPointPanel();
        });
    }

    private void OnLeaveRoomBtnClick()
    {
        ExitGuest();
    }

    /// <summary>
    /// 返回出生地
    /// </summary>
    private void OnSpawnBtnClick()
    {
        if(AvatarController.Inst.SelfController.Motor.CurUGCVehicleStatus == KinematicCharacterMotor.UGCVehicleStatus.TakeCar)
        {
            TipPanel.ShowToast("乘坐载具中不能返回出生点");
            CloseSelf();
            return;
        }
        ClientManager.Inst.BackToSpawn();
        CloseSelf();
    }

    private void OnInviteBtnClick()
    {
        GUIUtility.systemCopyBuffer = GameDataManager.Inst.gameOnlineData.roomCode;
        TipPanel.ShowToast("房间码复制成功，去分享给好友吧");
    }

    private void ShowPanelByType(RoomPanelType type)
    {
        RoomPanel.SetActive(type == RoomPanelType.PlayerList);
        SettingPanel.SetActive(type == RoomPanelType.Setting);
    }
}
