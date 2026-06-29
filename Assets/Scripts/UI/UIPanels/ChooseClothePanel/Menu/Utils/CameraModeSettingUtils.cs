using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Pet;
using Game.Props.PropsBehaviours;
using Game.Vehicle.PGCVehicle;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

public class CameraModeSettingUtils : GlobalInstance<CameraModeSettingUtils>
{
    // PlayerPrefs 持久化 Key
    public const string ShowNameKey = "CameraMode_IsShowName";

    private List<KinematicCharacterController> avatarList = new List<KinematicCharacterController>();

    public bool IsShowName = true;
    public bool IsShowInteractionHint = true;
    public bool IsShowSelf = true;
    public bool IsShowFriend = true;
    public bool IsShowStranger = true;
    public bool IsShowSelfieSticker = true;

    public bool isCheckingFriendship = false;

    //对于玩家来说的朋友列表
    private Dictionary<string, RelationShipInfo> friendshipList = new Dictionary<string, RelationShipInfo>();
    private List<AIBuddyInMapBehaviour> allInMapAIBuddies = new List<AIBuddyInMapBehaviour>();
    private bool _isReleased;
    
    public void Init()
    {
        _isReleased = false;
        IsShowSelfieSticker=true;
        friendshipList.Clear();
        avatarList.Clear();
        var avatarCtrl = AvatarController.Inst;
        avatarCtrl?.AddAvatarCreateListener(OnAvatarCreate);
        avatarCtrl?.AddAvatarRemoveListener(OnAvatarRemove);
        allInMapAIBuddies = GameObject.FindObjectsOfType<AIBuddyInMapBehaviour>().ToList();
        // 恢复用户持久化的名称显示设置
        SetNameShowState(PlayerPrefs.GetInt(ShowNameKey, 1) == 1, saveToPrefs: false);
    }

    /// <summary>
    /// 重新应用持久化的名称显示设置（用于从子面板/OC换装返回时恢复）
    /// </summary>
    public void RestoreNameSetting()
    {
        if (_isReleased) return;
        SetNameShowState(PlayerPrefs.GetInt(ShowNameKey, 1) == 1, saveToPrefs: false);
    }

    public void UIRelease(){
        var avatarCtrl = AvatarController.Inst;

        // 释放时强制还原：无论此前筛选状态如何，都恢复所有玩家可见。
        IsShowSelf = true;
        IsShowFriend = true;
        IsShowStranger = true;
        RestoreAllPlayersVisible();
        if(!IsShowName){
            // saveToPrefs: false —— 只恢复游戏场景状态，不覆盖用户保存的偏好设置
            SetNameShowState(true, saveToPrefs: false);
        }
        if(!IsShowInteractionHint){
            IsShowInteractionHint = true;
            SetInteractionHintShowState(true);
        }
        avatarCtrl?.RemoveAvatarCreateListener(OnAvatarCreate);
        avatarCtrl?.RemoveAvatarRemoveListener(OnAvatarRemove);
        MessageHelper.Broadcast(MessageName.ShowSelfieSticker, true);
        _isReleased = true;
    }

    private void RestoreAllPlayersVisible()
    {
        var avatarCtrl = AvatarController.Inst;
        var account = AccountDataManager.Inst;
        if (avatarCtrl == null) return;

        // 还原自己
        ShowPlayer(avatarCtrl.SelfStateController, true);
        if (!string.IsNullOrEmpty(account?.Uid))
        {
            ShowPet(account.Uid, true);
            ShowNPC(account.Uid, true);
            ShowVehicle(account.Uid, true);
        }

        // 还原其它玩家
        var playerDic = avatarCtrl.GetDic();
        if (playerDic == null) return;
        foreach (var eachPlayer in playerDic)
        {
            ShowPlayer(eachPlayer.Value, true);
            ShowPet(eachPlayer.Key, true);
            ShowNPC(eachPlayer.Key, true);
            ShowVehicle(eachPlayer.Key, true);
        }
    }

    private void OnAvatarCreate(string playerId, KinematicCharacterController kinematicCharacter)
    {
        if (_isReleased) return;

        if((!IsShowFriend || !IsShowStranger) && !friendshipList.ContainsKey(playerId)){
            isCheckingFriendship = true;
            var network = NetworkManager.Inst;
            if (network == null)
            {
                isCheckingFriendship = false;
                return;
            }
            network.SendHttpRequest(HttpUrlDefine.searchByID, HttpMethod.GET, 
            JsonConvert.SerializeObject(new SearchByIdParams() { targetId = playerId }), 
            onReceive: msg => {
                if (_isReleased) return;
                isCheckingFriendship = false;
                if (string.IsNullOrEmpty(msg))
                {
                    return;
                }
                SearchByIdResponse searchByIdResponse =
                    JsonConvert.DeserializeObject<SearchByIdResponse>(msg);
                var relation = searchByIdResponse?.relationShipInfo;
                if (relation != null)
                {
                    friendshipList[playerId] = relation;
                }
                var avatarCtrl = AvatarController.Inst;
                if (avatarCtrl == null || relation == null) return;
                if(!IsShowFriend){
                    if(relation.relationStatus == (int)RelationStatusType.Each){
                        ShowPlayer(avatarCtrl.GetPlayerStateCtrl(playerId), false);
                    }
                }
                if (!IsShowStranger){
                    if(relation.relationStatus != (int)RelationStatusType.Each){
                        ShowPlayer(avatarCtrl.GetPlayerStateCtrl(playerId), false);
                    }
                }
            }, onFail: arg0 => {
                LoggerUtils.LogError(arg0); 
                isCheckingFriendship = false;
            });
        }else{

        }

        if(!IsShowName){
            SetNameShowState(false, saveToPrefs: false);
        }
    }
    private void OnAvatarRemove(string playerId)
    {

    }

    public void SetNameShowState(bool isShow, bool saveToPrefs = true)
    {
        IsShowName = isShow;
        if (saveToPrefs)
        {
            PlayerPrefs.SetInt(ShowNameKey, isShow ? 1 : 0);
        }
        var avatarCtrl = AvatarController.Inst;
        var players = avatarCtrl?.GetDic();
        if (players != null)
        {
            foreach(var player in players){
                var avatarObj = player.Value?.Wrap?.Avatar;
                var head = avatarObj != null ? avatarObj.GetComponentInChildren<UserInfoHeadView>(true) : null;
                if(head != null){
                    head.gameObject.SetActive(isShow);
                }
                var pet = PetAvatarController.Inst != null ? PetAvatarController.Inst.GetPetKCCtrl(player.Key) : null;
                if(pet?.kinematicCharacterController != null){
                    var phead = pet.kinematicCharacterController.GetComponentInChildren<UserInfoHeadView>(true);
                    if(phead != null){
                        phead.gameObject.SetActive(isShow);
                    }
                }
                var npc = AIBuddyAvatarController.Inst != null ? AIBuddyAvatarController.Inst.GetPlayerStateCtrl(player.Key) : null;
                if(npc != null){
                    var npcAvatar = npc.Wrap?.Avatar;
                    var nhead = npcAvatar != null ? npcAvatar.GetComponentInChildren<UserInfoHeadView>(true) : null;
                    if(nhead != null){
                        nhead.gameObject.SetActive(isShow);
                    }
                }
            }
        }

        if (allInMapAIBuddies == null) return;
        foreach(var aibuddy in allInMapAIBuddies){
            if (aibuddy == null) continue;
            var head = aibuddy.GetComponentInChildren<UserInfoHeadView>(true);
            if(head != null){
                head.gameObject.SetActive(isShow);
            }
        }
        
    }

    public void SetInteractionHintShowState(bool isShow)
    {
        IsShowInteractionHint = isShow;
        UIOperationOnWorldPanel.bBeginRaycast = isShow;
    }

    public void SetSelfieStickerShowState(bool isShow)
    {
        IsShowSelfieSticker = isShow;
        MessageHelper.Broadcast(MessageName.ShowSelfieSticker, isShow);
    }

    public void SetShowState(int index, bool isShow)
    {
        var avatarCtrl = AvatarController.Inst;
        var account = AccountDataManager.Inst;
        switch(index)
        {
            case 0:
                IsShowSelf = isShow;
                ShowPlayer(avatarCtrl != null ? avatarCtrl.SelfStateController : null, isShow);
                if (!string.IsNullOrEmpty(account?.Uid))
                {
                    ShowPet(account.Uid, isShow);
                    ShowNPC(account.Uid, isShow);
                    ShowVehicle(account.Uid, isShow);
                }
                break;
            case 1:
                IsShowFriend = isShow;
                StartCheckShowPlayer(isShow);
                break;
            case 2:
                IsShowStranger = isShow;
                StartCheckShowPlayer(isShow);
                break;
        }
    }

    private void StartCheckShowPlayer(bool isShow){
        if (_isReleased) return;
        var avatarCtrl = AvatarController.Inst;
        var account = AccountDataManager.Inst;
        if (avatarCtrl == null || account == null) return;

        var players = avatarCtrl.GetDic();
        if (players == null) return;

        bool isHasUnknowRelationShip = false;
        foreach(var player in players){
            if(player.Key == account.Uid){
                continue;
            }
            if(!friendshipList.ContainsKey(player.Key)){
                isHasUnknowRelationShip = true;
                break;
            }
        }

        if(isHasUnknowRelationShip){
            isCheckingFriendship = true;
            string playIDsStirng = string.Join(",", players.Keys);
            BatchInfoReq batchInfoReq = new BatchInfoReq()
            {
                uidList = playIDsStirng
            };
            var network = NetworkManager.Inst;
            if (network == null)
            {
                isCheckingFriendship = false;
                FilterShowPlayer();
                return;
            }
            network.SendHttpRequest(HttpUrlDefine.batchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(batchInfoReq),
            onReceive: msg =>
            {
                if (_isReleased) return;
                isCheckingFriendship = false;
                BatchInfoResponse batchInfoResponse =
                    JsonConvert.DeserializeObject<BatchInfoResponse>(msg);
                if (batchInfoResponse != null)
                {
                    foreach(var playerInfo in batchInfoResponse.list)
                    {
                        AccountUserInfo accountUserInfo = playerInfo.userInfo;
                        RelationShipInfo relationShipInfo = playerInfo.relationShipInfo;
                        if(accountUserInfo != null && relationShipInfo != null){
                            friendshipList[accountUserInfo.uid] = relationShipInfo;
                        }
                    }  
                    FilterShowPlayer();
                }
            }, onFail: arg0 =>
            {
                LoggerUtils.LogError(arg0);
                isCheckingFriendship = false;
            });

        }else{
            FilterShowPlayer();
        }

    }


    private void FilterShowPlayer(){
        var avatarCtrl = AvatarController.Inst;
        var account = AccountDataManager.Inst;
        if (avatarCtrl == null || account == null) return;
        var playerDic = avatarCtrl.GetDic();
        if (playerDic == null) return;

        //除去自己
        List<string> showPlayerIds = playerDic.Keys.ToList().Where(key => key != account.Uid).ToList();
        if(!IsShowFriend){
            var friendList = friendshipList.Where(player => player.Value.relationStatus == (int)RelationStatusType.Each).ToList();
            foreach(var player in friendList){
                if(showPlayerIds.Contains(player.Key)){
                    showPlayerIds.Remove(player.Key);
                }
            }
        }
        if(!IsShowStranger){
            var notFriendList = friendshipList.Where(player => player.Value.relationStatus != (int)RelationStatusType.Each).ToList();
            foreach(var player in notFriendList){
                if(showPlayerIds.Contains(player.Key)){
                    showPlayerIds.Remove(player.Key);
                }
            }
        }

        var playerList = playerDic;

        foreach(var eachPlayer in playerList){

            if(eachPlayer.Key == account.Uid){
                continue;
            }
            //包括就是显示
            bool isShow = showPlayerIds.Contains(eachPlayer.Key);
            ShowPlayer(eachPlayer.Value, isShow);
            ShowPet(eachPlayer.Key, isShow);
            ShowNPC(eachPlayer.Key, isShow);
            ShowVehicle(eachPlayer.Key, isShow);

        }
    }

    private void ShowPlayer(PlayerStateController player, bool isShow){
        if (player == null) return;
        var character = player.Wrap;

        if(character != null && character.Avatar != null){
            var allReneders = character.Avatar.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in allReneders){
                renderer.enabled = isShow;
            }
        }
    }

    private void ShowPet(string playerId, bool isShow){
        if (string.IsNullOrEmpty(playerId)) return;
        var pet = PetAvatarController.Inst != null ? PetAvatarController.Inst.GetPetKCCtrl(playerId) : null;
        if(pet?.kinematicCharacterController != null){
            var renders = pet.kinematicCharacterController.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renders){
                renderer.enabled = isShow;
            }
        }
    }

    private void ShowNPC(string playerId, bool isShow){
        if (string.IsNullOrEmpty(playerId)) return;
        var npc = AIBuddyAvatarController.Inst != null ? AIBuddyAvatarController.Inst.GetPlayerStateCtrl(playerId) : null;
        if(npc != null){
            var character = npc.Wrap;
            if(character != null && character.Avatar != null){
                var allReneders = character.Avatar.GetComponentsInChildren<Renderer>(true);
                foreach(var renderer in allReneders){
                    renderer.enabled = isShow;
                }
            }
        }
    }

    private void ShowVehicle(string playerId, bool isShow){
        if (string.IsNullOrEmpty(playerId)) return;
        var vehicle = PGCVehicleManager.Inst != null ? PGCVehicleManager.Inst.GetPGCVehicleController(playerId) : null;
        if(vehicle?.VehicleKinematic != null){
            var renders = vehicle.VehicleKinematic.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renders){
                renderer.enabled = isShow;
            }
        }
    }
}