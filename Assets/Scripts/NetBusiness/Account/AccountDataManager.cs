using System;
using System.Collections.Generic;
using Basic.Utils;
using EventTracking;
using Game.Avatar;
using GameData;
using GameData.Account;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class AccountDataManager : GlobalInstance<AccountDataManager>
{
    /*
    superProperties["diamond_amount"] = balanceInfo.GetAccountCount(CurrencyType.Gem);//剩余钻石数量
     superProperties["pinkcoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.PinkCoin);//剩余粉币数量
     superProperties["bluecoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.Badge);//剩余徽章数量
     superProperties["coin_amount"] = balanceInfo.GetAccountCount(CurrencyType.Coin);//剩余金币数量
    */
    public int GemCount = -1;
    public int PinkCoinCount = -1;
    public int BadgeCount = -1;
    public int CoinCount = -1;
    /// <summary>
    /// 当前登录者UID
    /// </summary>
    public string Uid
    {
        get
        {
            return userInfo?.uid;
        }
    }

    /// <summary>
    ///  当前登录的平台
    /// </summary>
    private AccountPlatform platform = AccountPlatform.Unknown;

    public AccountPlatform accountPlatform
    {
        get
        {
            return platform;
        }
    }

    private AccountBalanceInfo _balanceInfo =  new AccountBalanceInfo();
    public AccountBalanceInfo BalanceInfo
    {
        get
        {
            return _balanceInfo;
        }
    }

    /// <summary>
    /// 当前官方登录的unionid
    /// </summary>
    private string unionid;

    public string accountUnionid
    {
        get
        {
            return unionid;
        }
    }

    private string token = "";
    /// <summary>
    /// 当前登陆者Token
    /// </summary>
    public string Token
    {
        get
        {
            return token;
        }
    }

    public bool isNewOpenld = false;
    //是否需要绑定账号
    public bool NeedBind = false;

    private AccountUserInfo userInfo = new AccountUserInfo();

    private AccountPetInfo petInfo = new AccountPetInfo();

    private AIBuddyInfo aibuddyInfo = new AIBuddyInfo();

    // 大厅 AI 伙伴角色（GameData 侧复刻类型，可见性 isHidden 内置，对标 aibuddyInfo）
    private HallCharacterInfo hallCharacterInfo;

    private VehicleInfo vehicleInfo = null;

    public AccountUserInfo UserInfo
    {
        get
        {
            return userInfo;
        }
    }

    public AccountPetInfo PetInfo {
        get {
            return petInfo;
        }
    }

    public AIBuddyInfo AIBuddyInfo {
        get {
            return aibuddyInfo;
        }
    }

    // 大厅伙伴角色（可见性读 characterInfo.isHidden）
    public HallCharacterInfo HallCharacterInfo {
        get {
            return hallCharacterInfo;
        }
    }

    public VehicleInfo VehicleInfo {
        get {
            return vehicleInfo;
        }
    }

    private bool isSaveEncode = true;

    private bool IsSaveEncode
    {
        get
        {
#if UNITY_EDITOR
            return false;
#endif
            return isSaveEncode;
        }
    }

    public MIData TryListenMIData
    {
        get
        {
            var str = PlayerPrefs.GetString("MusicDataMI-" + Uid, "");
            try
            {
                MIData data = JsonConvert.DeserializeObject<MIData>(str);
                return data == null ? new MIData() : data;
            }
            catch
            {
                return new MIData();
            }
        }
        set
        {
            PlayerPrefs.SetString("MusicDataMI-" + Uid, JsonConvert.SerializeObject(value));
        }
    }

    public MSData TryListenMSData
    {
        get
        {
            var str = PlayerPrefs.GetString("MusicDataMS-" + Uid, "");
            try
            {
                MSData data = JsonConvert.DeserializeObject<MSData>(str);
                return data == null ? new MSData() : data;
            }
            catch
            {
                return new MSData();
            }
        }
        set
        {
            PlayerPrefs.SetString("MusicDataMS-" + Uid, JsonConvert.SerializeObject(value));
        }
    }

    /// <summary>
    /// 本地数据已经发生改变
    /// </summary>
    private Action<AccountUserInfo> OnUserInfoChange;
    private Action<AccountPetInfo> OnPetInfoChange;
    private Action<VehicleInfo> OnVehicleInfoChange;
    private Action<string> OnNickNameChange;
    private Action<AccountUserInfo> OnAvatarChange;
    private Action<AccountUserInfo,AccountPetInfo, AIBuddyInfo, VehicleInfo> OnIdleChange;
    private Action<AccountPetInfo> OnPetAvatarChange;
    private Action<int> OnShowedPopupRatingChange;
    private Action<AIBuddyInfo> OnAIBuddyInfoChange;
    private Action<AIBuddyOp, AIBuddyInfo> OnAIBuddySyncChange;
    // 大厅伙伴角色变更。UI 侧订阅后刷新展示
    private Action<HallCharacterInfo> OnHallCharacterChange;
    public void AddUserInfoChangeListener(Action<AccountUserInfo> callback)
    {
        OnUserInfoChange += callback;
    }

    public void RemoveUserInfoChangeListener(Action<AccountUserInfo> callback)
    {
        OnUserInfoChange -= callback;
    }

    public void AddIdleChangeListener(Action<AccountUserInfo,AccountPetInfo, AIBuddyInfo, VehicleInfo> callback)
    {
        OnIdleChange += callback;
    }

    public void RemoveIdleChangeListener(Action<AccountUserInfo,AccountPetInfo, AIBuddyInfo, VehicleInfo> callback)
    {
        OnIdleChange -= callback;
    }


    public void AddPetInfoChangeListener(Action<AccountPetInfo> callback) {
        OnPetInfoChange += callback;
    }

    public void RemovePetInfoChangeListener(Action<AccountPetInfo> callback)
    {
        OnPetInfoChange -= callback;
    }


    public void AddNickChangeListener(Action<string> callback)
    {
        OnNickNameChange += callback;
    }

    public void RemoveNickChangeListener(Action<string> callback)
    {
        OnNickNameChange -= callback;
    }

    public void AddAvatarChangeListener(Action<AccountUserInfo> callback)
    {
        OnAvatarChange += callback;
    }

    public void RemoveAvatarChangeListener(Action<AccountUserInfo> callback)
    {
        OnAvatarChange -= callback;
    }

    public void AddPetAvatarChangeListener(Action<AccountPetInfo> callback) {
        OnPetAvatarChange += callback;
    }

    public void RemovePetAvatarChangeListener(Action<AccountPetInfo> callback)
    {
        OnPetAvatarChange -= callback;
    }

    public void AddAIBuddyChangeListener(Action<AIBuddyInfo> callback) {
        OnAIBuddyInfoChange += callback;
    }

    public void RemoteAIBuddyChangeListener(Action<AIBuddyInfo> callback) {
        OnAIBuddyInfoChange -= callback;
    }

    public void AddHallCharacterChangeListener(Action<HallCharacterInfo> callback) {
        OnHallCharacterChange += callback;
    }

    public void RemoveHallCharacterChangeListener(Action<HallCharacterInfo> callback) {
        OnHallCharacterChange -= callback;
    }

    // 大厅伙伴角色刷新（服务端拉取）/ 本地设置后回写，统一更新缓存并广播
    public void UpdateHallCharacter(HallCharacterInfo info) {
        hallCharacterInfo = info;
        OnHallCharacterChange?.Invoke(info);
    }
    public void AddVehicleInfoChangeListener(Action<VehicleInfo> callback) {
        OnVehicleInfoChange += callback;
    }

    public void RemoveVehicleInfoChangeListener(Action<VehicleInfo> callback) {
        OnVehicleInfoChange -= callback;
    }

    public void AddAIBuddyOpListener(Action<AIBuddyOp,AIBuddyInfo> callback)
    {
        OnAIBuddySyncChange += callback;
    }

    public void RemoveAIBuddyOpListener(Action<AIBuddyOp,AIBuddyInfo> callback)
    {
        OnAIBuddySyncChange -= callback;
    }

    public void AddShowedPopupRatingChangeListener(Action<int> callback)
    {
        OnShowedPopupRatingChange += callback;
    }

    public void RemoveShowedPopupRatingChangeListener(Action<int> callback)
    {
        OnShowedPopupRatingChange -= callback;
    }

    public void CreateUserInfo()
    {
        userInfo = new AccountUserInfo();
    }

    public bool IsMySelf(string uid)
    {
        string myUid = Uid;
        if (!string.IsNullOrEmpty(uid) && uid == myUid)
        {
            return true;
        }
        return false;
    }

    public void UpdateAccountPlatform(AccountPlatform platform,string openId)
    {
        this.platform = platform;
        this.unionid = openId;
        LoggerUtils.Log("###UpdateAccount Platform："+this.platform + "  openId:"+this.unionid);
        SyncDiskCache();
    
    }

    public void UpdateAccountId(string uid,string newToken)
    {
        this.userInfo.uid = uid;
        this.token = newToken;
        SetHttpTokenInfo(uid,newToken);
    }

    public void SignIn(AccountPlatform platform, string openId, SignChannelInfo channelInfo, Action<AccountData> successAction, Action<HttpResponseFailDataStruct>failAction)
    {
        this.platform = platform;
        this.unionid = openId;
        var req = new SignInReq
        {
            provider = (int)platform,
            openId = openId,
            channelInfo = channelInfo
        };

        Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
        superProperties["open_id"] = openId;//openId 
        AnalyticsManager.Inst.SetSuperProperties(superProperties);//设置公共事件属性
      //  Debug.LogError($"SignIn openid={openId}");
        //AnalyticsManager.Inst.UpdateOpenId(openId);
#if UNITY_EDITOR
        if (DebugSetting.Inst != null && !string.IsNullOrEmpty(DebugSetting.Inst.userAccount.openId))
        {
            req.openId = DebugSetting.Inst.userAccount.openId;
        }
#endif

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.login,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                AccountData authData = JsonConvert.DeserializeObject<AccountData>(msg);
                Debug.Log("isNewOpenld=" + authData.isNewOpenId);
                var tokenRes = authData.token;
                if (!string.IsNullOrEmpty(tokenRes))
                {
                    token = tokenRes;
                }

                userInfo = authData.userInfo;
                petInfo = authData.petInfo;
                if (petInfo == null) {
                    petInfo = AccountPetInfo.GetDefaultInfo();
                }
                aibuddyInfo = authData.aibuddyInfo;
                if (aibuddyInfo == null) {
                    aibuddyInfo = AIBuddyInfo.GetDefaultInfo();
                }
                SetHttpTokenInfo(authData);
                if (!authData.IsNewUserRegister)
                {
                    SyncDiskCache();
                }

                Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
                superProperties["open_id"] = AccountDataManager.Inst.accountUnionid;//UID 
                superProperties["role_id"] = userInfo.username;//UID
                superProperties["role_name"] = userInfo.nickname;//名字
              //  Debug.LogError($"SignIn openid={openId},userInfo.username={userInfo.username},userInfo.nickname={userInfo.nickname}");
                AnalyticsManager.Inst.SetSuperProperties(superProperties);//设置公共事件属性


                MessageHelper.Broadcast(MessageName.LoginSuccess);

                successAction?.Invoke(authData);


            }, onFail: arg0 =>
            {
                Debug.Log("login onFail" );
                HttpResponseFailDataStruct failRes = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(arg0);
                failAction?.Invoke(failRes);
            });
    }

    public void RequestAvatarFrameOrChat()
    {
        var jb = new JObject
        {
            ["targetUid"] = Uid
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publicProfile,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            OnUserInfoSuccess,
            OnUserInfoFail,
            retryCount: 3);
    }

    private void OnUserInfoSuccess(string content)
    {
        GetUserInfoRsp resData = JsonConvert.DeserializeObject<GetUserInfoRsp>(content);
        AccountUserInfo _userInfo = resData.userInfo;
        AccountDataManager.Inst.UpdateHeadCycleWithoutNotify(_userInfo.ownedAvatarFrameList);
        AccountDataManager.Inst.UpdateChatBubbleWithoutNotify(_userInfo.ownedChatBubblesList);

        MessageHelper.Broadcast(MessageName.OnUserStyleSuccess);
    }

    private void OnUserInfoFail(string message)
    {

    }

    public void GetBindStatus(Action<AccountData> successAction, Action<HttpResponseFailDataStruct>failAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BindStatus, HttpMethod.GET,"",
            onReceive: arg0 =>
            {
                BindStatusData resData = JsonConvert.DeserializeObject<BindStatusData>(arg0);
                bool canConvert = Enum.IsDefined(typeof(AccountPlatform), resData.provider);
                if (canConvert)
                {
                    this.platform = (AccountPlatform)resData.provider;
                }
                else
                {
                    LoggerUtils.LogError("GetBindStatus provider is unKnow:"+resData.provider);
                    this.platform = AccountPlatform.Unknown;
                }

                var authData = resData.accountData;
                successAction?.Invoke(resData.accountData);

            },
            onFail: arg0 =>
            {

            },
            retryCount:3);
    }



    /// <summary>
    /// 从后端刷新当前人物数据
    /// </summary>
    /// <param name="successAction"></param>
    public void RefreshUserInfo(Action<GetImageRes> successAction = null)
    {
        if (string.IsNullOrEmpty(Uid))
        {
            return;
        }

        var jb = new JObject
        {
            ["targetUid"] = Uid
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getUserImage,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                GetImageRes resData = JsonConvert.DeserializeObject<GetImageRes>(arg0);
                var lobbyVehicleInfo = resData.GetLobbyVehicleInfo();
                Debug.Log($"[Vehicle] RefreshUserInfo: vehicleInfo={resData.vehicleInfo?.id} lobbyVehicle={resData.lobbyVehicle?.id} resolved={lobbyVehicleInfo?.id} isHidden={lobbyVehicleInfo?.isHidden}");
                successAction?.Invoke(resData);
                OnRefreshHallAnim(resData.userInfo,resData.petInfo, resData.aibuddyInfo, lobbyVehicleInfo);
                OnRefreshUserSuccess(resData.userInfo);
                OnRefreshPetSuccess(resData.petInfo);
                OnRefreshAIBuddyInfoSuccess(resData.aibuddyInfo);
                UpdateHallCharacter(resData.characterInfo);
                OnRefreshVehicleSuccess(lobbyVehicleInfo);
                OnShowedPopupRatingChange?.Invoke(resData.showedPopupRating);
            },
            onFail: arg0 =>
            {
                OnRefreshFail(arg0);
            },
            retryCount:3);
    }

    public void RefreshUserInfoSignin()
    {
        if (string.IsNullOrEmpty(Uid))
        {
            return;
        }
        var jb = new JObject
        {
            ["targetUid"] = Uid
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getUserImage,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                GetImageRes resData = JsonConvert.DeserializeObject<GetImageRes>(arg0);
                OnShowedPopupRatingChange?.Invoke(resData.showedPopupRating);
            },
            onFail: arg0 =>
            {
                OnRefreshFail(arg0);
            },
            retryCount: 3);
    }


    private void OnRefreshPetSuccess(AccountPetInfo petResData) {
        if (petResData == null)
        {
            return;
        }
        if (petInfo.CompareTo(petResData) != 0)
        {
            bool isPetAvatarChange = !string.IsNullOrEmpty(petResData.avatarJson) && petInfo.avatarJson != petResData.avatarJson;
            petInfo = petResData;
            if (isPetAvatarChange) {
                OnPetAvatarChange?.Invoke(petResData);
            }
            OnPetInfoChange?.Invoke(petResData);
            SyncDiskCache();
        }
    }

    private void OnRefreshAIBuddyInfoSuccess(AIBuddyInfo info)
    {
        if(info == null)
            return;

        OnAIBuddyInfoChange?.Invoke(info);
    }

    public void UpdateHeadCycleWithoutNotify(List<AccountUserInfo.OwnedAvatarFrame> ownedAvatarFrameList)
    {
        if (userInfo != null && ownedAvatarFrameList != null && ownedAvatarFrameList.Count > 0)
        {
            userInfo.ownedAvatarFrameList = ownedAvatarFrameList;
        }
    }

    public void UpdateChatBubbleWithoutNotify(List<AccountUserInfo.OwnedChatBubble> ownedChatBubblesList)
    {
        if (userInfo != null && ownedChatBubblesList != null && ownedChatBubblesList.Count > 0)
        {
            userInfo.ownedChatBubblesList = ownedChatBubblesList;
        }
    }

    /// <summary>
    /// /image/publicProfile 等接口返回的创作者徽章信息；本地 UserInfo.creatorBadgeInfo 可能为空，需回填后再做「使用中」等对比。
    /// </summary>
    public void ApplyCreatorBadgeInfo(CreatorBadgeInfoData badge)
    {
        if (userInfo == null || badge == null)
        {
            return;
        }

        userInfo.creatorBadgeInfo = badge;
    }

    public bool IsMyAIBuddy(AINpcInfo npcInfo)
    {
        if(this.AIBuddyInfo == null || this.AIBuddyInfo.id != npcInfo.id)
            return false;

        return true;
    }

    public bool IsMyAIBuddy(string npcId)
    {
        if(this.AIBuddyInfo == null || this.AIBuddyInfo.id != npcId)
            return false;

        return true;
    }

    public void CreateAIBuddy(AINpcInfo npcInfo, Action<AINpcInfo> onSuccess,Action<HttpResponseFailDataStruct> onFail = null)
    {
        AIBuddyInfo aiBuddyInfo = new AIBuddyInfo();
        aiBuddyInfo.npc = npcInfo;
        SyncAIBuddy(AIBuddyOp.Create,aiBuddyInfo, (aiBuddyInfo) =>
        {
            onSuccess?.Invoke(aiBuddyInfo.npc);
        },(arg0) => {
            HttpResponseFailDataStruct failRes = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(arg0);
            onFail?.Invoke(failRes);
        });
    }

    public void DeleteAIBuddy(AIBuddyInfo aiBuddyInfo,Action<AIBuddyInfo> onSuccess,Action<string> onFail = null)
    {
        SyncAIBuddy(AIBuddyOp.Delete,aiBuddyInfo,onSuccess,onFail);
    }





    public void SetInLobby(AIBuddyInfo aiBuddyInfo, Action<AIBuddyInfo> onSuccess, Action<string> onFail = null) {
        SyncAIBuddy(AIBuddyOp.SetInLobby,aiBuddyInfo,onSuccess,onFail);
    }

    public void SyncAIBuddy(AIBuddyOp op,AIBuddyInfo aiBuddyInfo, Action<AIBuddyInfo> onSuccess,Action<string> onFail = null)
    {
        var reqParam = new AIBuddySetReq
        {
            type = (int)op,
            info = aiBuddyInfo
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIBuddySet,
            HttpMethod.POST,
            JsonConvert.SerializeObject(reqParam),
            onReceive: arg0 =>
            {
                onSuccess?.Invoke(aiBuddyInfo);
                OnAIBuddySyncChange?.Invoke(op,aiBuddyInfo);
            },
            onFail: arg0 =>
            {
                LoggerUtils.LogError("SyncAIBuddy Fail");
                onFail?.Invoke(arg0);
            },
            retryCount:3);
    }

    public void OnRefreshHallAnim(AccountUserInfo userData,AccountPetInfo petData, AIBuddyInfo aiBuddyData, VehicleInfo vehicleData)
    {
        if (userInfo.CompareTo(userData) != 0 || petInfo.CompareTo(petData) != 0 || aibuddyInfo.CompareTo(aiBuddyData) != 0)
        {
            bool isAvatarIdleChange = userData.idleData != null && (userInfo.idleData == null || !userData.idleData.Equals(userInfo.idleData));
            bool isPetIdleChange = petData.idleData != null && (petInfo.idleData == null || !petData.idleData.Equals(petInfo.idleData));
            bool isAIBuddyIdleChange = aiBuddyData != null && aiBuddyData.idleData!= null && (aibuddyInfo.idleData == null || aibuddyInfo.idleData == null ||!aiBuddyData.idleData.Equals(aibuddyInfo.idleData));
            if (isAvatarIdleChange || isPetIdleChange || isAIBuddyIdleChange)
            {
                OnIdleChange?.Invoke(userData,petData, aiBuddyData, vehicleData);
            }
        }
    }

    private void OnRefreshUserSuccess(AccountUserInfo resData)
    {
        if (resData == null)
        {
            return;
        }
        if (userInfo.CompareTo(resData) != 0)
        {
            bool isAvatarChange = !string.IsNullOrEmpty(resData.avatarJson) && userInfo.avatarJson != resData.avatarJson;
            bool isNickChange = !string.IsNullOrEmpty(resData.nickname) && userInfo.nickname != resData.nickname;
            bool isIdleChange = resData.idleData != null && (userInfo.idleData == null || !resData.idleData.Equals(userInfo.idleData));
            CreatorLevelInfo creatorLevelInfo = null ;
            if (resData.creatorLevelInfo == null && userInfo.creatorLevelInfo != null)
            {
                creatorLevelInfo = userInfo.creatorLevelInfo;
            }
            userInfo = resData;
            if(creatorLevelInfo != null)
            {
                userInfo.creatorLevelInfo = creatorLevelInfo;
                creatorLevelInfo = null;
            }
            if (isAvatarChange)
            {
                OnAvatarChange?.Invoke(userInfo);
                MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);//通知修改形象类的任务panel
            }

            if (isNickChange)
            {
                OnNickNameChange?.Invoke(resData.nickname);
            }
            OnUserInfoChange?.Invoke(resData);
            SyncDiskCache();
        }
    }

    private void OnRefreshVehicleSuccess(VehicleInfo vehicleInfo)
    {
        if (vehicleInfo == null)
        {
            return;
        }
        this.vehicleInfo = vehicleInfo;
        OnVehicleInfoChange?.Invoke(vehicleInfo);
        SyncDiskCache();
    }
    private void OnRefreshFail(string msg)
    {
        Debug.LogError($"Refresh Account User fail {msg}");
    }

    /// <summary>
    /// 完成新手注册流程
    /// </summary>
    /// <param name="userInfo"></param>
    public void CompleteNewUser(AccountUserInfo userInfo, Action<bool> resultAction = null)
    {
        // 调用后端接口
        var req = new SetImageReq
        {
            userInfo = userInfo,
            setType = (int)SetUserInfoType.Registration
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    /// <summary>
    /// 同步人物形象数据
    /// </summary>
    /// <param name="data">当前人物形象</param>
    /// <param name="resultAction">结果</param>
    public void SyncAvatarData(CharacterData data, Action<bool> resultAction = null)
    {
        if (data == null)
        {
            resultAction?.Invoke(false);
            return;
        }

        var userCopy = userInfo.Clone() as AccountUserInfo;
        var jsonContent = CharacterData.SerializeObject(data);
        userCopy.avatarJson = jsonContent;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.Avatar,
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    public void SyncIdleData(IdleData data, Action<bool> resultAction = null)
    {
        if (data == null)
        {
            resultAction?.Invoke(false);
            return;
        }

        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.idleData = data;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.LobbyIdle,
        };

        SendSetUserInfoRequest(req, resultAction);
    }

    public void SyncPetIdleData(IdleData data, Action<bool> resultAction = null)
    {
        if (data == null)
        {
            resultAction?.Invoke(false);
            return;
        }
        var userCopy = petInfo.Clone() as AccountPetInfo;
        userCopy.idleData = data;
        var req = new SetImageReq
        {
            petInfo = userCopy,
            setType = (int)SetUserInfoType.PetLobbyIdle,
        };
        SendSetPetInfoRequest(req, resultAction);
    }

    public void SyncPetData(bool isHidden)
    {
        petInfo.isHidden = isHidden ? 1 : 0;
        var userCopy = petInfo.Clone() as AccountPetInfo;
        userCopy.isHidden = isHidden ? 1 : 0;
        var req = new SetImageReq
        {
            petInfo = userCopy,
            setType = (int)SetUserInfoType.SetPetLobbyVisible,
        };
        SendSetPetInfoRequest(req, (suc) =>
        {
            LoggerUtils.Log("SyncPetVisible--",suc,"-------",isHidden);
        });
    }

    public void SyncAIBuddyIdleData(IdleData data, Action<bool> resultAction = null) {
        if (data == null)
        {
            resultAction?.Invoke(false);
            return;
        }
        var userCopy = aibuddyInfo.Clone() as AIBuddyInfo;
        userCopy.idleData = data;
        SyncAIBuddy(AIBuddyOp.SetIdleData, userCopy, (info) => {
            resultAction?.Invoke(true);
        }, (err) => {
            resultAction?.Invoke(false);
        });
    }

    public void SyncAIBuddyLobby(AIBuddyInfo buddyInfo) {
        if (buddyInfo == null || string.IsNullOrEmpty(buddyInfo.id)) {
            return;
        }
        aibuddyInfo.CopyFrom(buddyInfo);
        SyncAIBuddy(AIBuddyOp.SetInLobby, aibuddyInfo, info => {
            LoggerUtils.Log("SyncNpcLobby Success -------",buddyInfo.id);
        }, err => {
            LoggerUtils.LogError("SyncNpcLobby Fail -------",buddyInfo.id + "," + err);
        });


    }

    public void SyncAIBuddy(bool isHidden) {
        aibuddyInfo.isHidden = isHidden ? 1 : 0;
        var userCopy = aibuddyInfo.Clone() as AIBuddyInfo;
        userCopy.isHidden = isHidden? 1 : 0;
        SyncAIBuddy(AIBuddyOp.SetHidden, userCopy, (info) => {
            LoggerUtils.Log("SyncNpcVisible Success -------",isHidden);
        });
    }

    public void SyncGamePetData(int isGameHidden)
    {
        petInfo.isGameHidden = isGameHidden;
        var userCopy = petInfo.Clone() as AccountPetInfo;
        userCopy.isGameHidden = isGameHidden;
        var req = new SetImageReq
        {
            petInfo = userCopy,
            setType = (int)SetUserInfoType.SetPetLobbyVisible,
        };
        SendSetPetInfoRequest(req, (suc) =>
        {
            LoggerUtils.Log("SyncPetVisible-isGameHidden-",suc,"-------",isGameHidden);
        });
    }

	public void SyncPetAvatarData(PetData data, Action<bool> resultAction = null) {

        if (data == null)
        {
            resultAction?.Invoke(false);
            return;
        }

        var petCopy = petInfo.Clone() as AccountPetInfo;
        var jsonContent = PetData.SerializeObject(data);
        petCopy.avatarJson = jsonContent;
        var req = new SetImageReq
        {
            petInfo = petCopy,
            setType = (int)SetUserInfoType.Avatar,
        };

        SendSetPetInfoRequest(req,resultAction);
    }

    public void SyncVehicleData(VehicleInfo data, Action<bool> resultAction = null)
    {
        if (data == null)
        {
            resultAction?.Invoke(false);
            return;
        }
        var userVehicleCopy = data.Clone() as VehicleInfo;
        var req = new SetImageReq
        {
            vehicleInfo = userVehicleCopy,
        };
        SendSetVehicleInfoRequest(req,resultAction);
    }

    public void SyncVehicleData(bool isHidden) {
        //vehicleInfo.isHidden = isHidden ? 1 : 0;
        var userCopy = vehicleInfo.Clone() as VehicleInfo;
        userCopy.isHidden = vehicleInfo.isHidden;
        var req = new SetImageReq
        {
            vehicleInfo = userCopy,
        };
        SendSetVehicleInfoRequest(req, (suc) => {
            //LoggerUtils.LogError("SyncVehicleVisible Success -------",isHidden);
        });
    }

    /// <summary>
    /// 修改个人简介
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetBioReqeust(string bioStr, Action<bool> resultAction = null)
    {
        if (string.IsNullOrEmpty(bioStr))
        {
            return;
        }

        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.bio = bioStr;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.Bio,
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    /// <summary>
    /// 修改个人昵称
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetNickReqeust(string newNick, Action<bool> resultAction = null)
    {
        if (string.IsNullOrEmpty(newNick))
        {
            return;
        }

        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.nickname = newNick;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.Nick,
        };

        SendSetUserInfoRequest(req,resultAction);
    }
    /// <summary>
    /// 修改宠物昵称
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetPetNickReqeust(string newNick, Action<bool> resultAction = null)
    {
        if (string.IsNullOrEmpty(newNick))
        {
            return;
        }

        var userCopy = petInfo.Clone() as AccountPetInfo;
        userCopy.nickname = newNick;
        var req = new SetImageReq
        {
            petInfo = userCopy,
            setType = (int)SetUserInfoType.Nick,
        };

        SendSetPetInfoRequest(req,resultAction);
    }
    /// <summary>
    /// 修改头像
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetHeadImgReqeust(string portraitUrl, Action<bool> resultAction = null)
    {
        if (string.IsNullOrEmpty(portraitUrl))
        {
            return;
        }

        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.portraitUrl = portraitUrl;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.Portrait,
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    /// <summary>
    /// 修改个人主页皮肤
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetProfileThemeReqeust(int themeId, Action<bool> resultAction = null)
    {
        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.homepageSkin = themeId;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.ProfileTheme,
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    /// <summary>
    /// 修改头像框
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetAvatarFrameRequest(int headCycle, Action<bool> resultAction = null)
    {
        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.avatarFrame = headCycle;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.SetAvatarFrame,
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    /// <summary>
    /// 修改聊天气泡
    /// </summary>
    /// <param name="req"></param>
    /// <param name="resultAction"></param>
    ///
    public void SendSetChatBubbleRequest(int bubbleId, Action<bool> resultAction = null)
    {
        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.chatBubbles = bubbleId;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.SetChatBubbles,
        };

        SendSetUserInfoRequest(req,resultAction);
    }

    public void SendSetNicknameBgRequest(int bubbleId, Action<bool> resultAction = null)
    {
        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.nicknameFrame = bubbleId;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.SetNicknameBg,
        };

        SendSetUserInfoRequest(req, resultAction);
    }

    public void SendSetTitleIdRequest(int titleId, Action<bool> resultAction = null)
    {
        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.titleId = titleId;
        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.SetTitleId,
        };

        SendSetUserInfoRequest(req, resultAction);
    }

    /// <summary>
    /// 设置当前展示的创作者赛季徽章品类（/image/set，setType=27）。
    /// </summary>
    public void SendSetCreatorBadgeRequest(CreatorBadgeInfoData badge, Action<bool> resultAction = null)
    {
        if (userInfo == null || badge == null)
        {
            resultAction?.Invoke(false);
            return;
        }

        var userCopy = userInfo.Clone() as AccountUserInfo;
        userCopy.creatorBadgeInfo = new CreatorBadgeInfoData
        {
            score = badge.score,
            level = badge.level,
            category = badge.category,
            id = badge.id,
            rank = badge.rank
        };

        var req = new SetImageReq
        {
            userInfo = userCopy,
            setType = (int)SetUserInfoType.SetCreatorBadge,
        };

        SendSetUserInfoRequest(req, resultAction);
    }

    private void SendSetUserInfoRequest(SetImageReq req,Action<bool> resultAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setImage,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                GetImageRes serverData = JsonConvert.DeserializeObject<GetImageRes>(arg0);
                OnRefreshUserSuccess(serverData.userInfo);
                resultAction?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false);
            },  retryCount:3);
    }
    private void SendSetPetInfoRequest(SetImageReq req,Action<bool> resultAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetPetImage,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                GetImageRes serverData = JsonConvert.DeserializeObject<GetImageRes>(arg0);
                OnRefreshPetSuccess(serverData.petInfo);
                resultAction?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false);
            },  retryCount:3);
    }

    private void SendSetVehicleInfoRequest(SetImageReq req,Action<bool> resultAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetVehicleImage,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                GetImageRes serverData = JsonConvert.DeserializeObject<GetImageRes>(arg0);
                OnRefreshVehicleSuccess(serverData.GetLobbyVehicleInfo());
                resultAction?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false);
            },  retryCount:3);
    }


    public void deregister(Action<bool> resultAction = null)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.deregister,
            HttpMethod.POST,
            "",
            onReceive: arg0 =>
            {
                resultAction?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false);
            },  retryCount:3);
    }

    /// <summary>
    /// 校验本地缓存文件
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private bool VerifyCache(AccountData data)
    {
        if (data == null)
        {
            return false;
        }

        var token = data?.token;
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }
        var uid = data.userInfo.uid;
        if (string.IsNullOrEmpty(uid))
        {
            return false;
        }

        var platform = data.platform == 0;
        if (platform)
        {
            return false;
        }

        var username = data?.userInfo?.username;
        if (string.IsNullOrEmpty(username))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 是否有文件缓存
    /// </summary>
    /// <returns></returns>
    public bool HasDiskCache()
    {
        Init();
        if (!SaveGameUtil.Inst.Exists(SaveGameUtil.gAccountKey))
        {
            return false;
        }

        AccountData diskData = null;
        try
        {
            diskData = SaveGameUtil.Inst.Load<AccountData>(SaveGameUtil.gAccountKey, IsSaveEncode);
        }
        catch (Exception e)
        {
            LoggerUtils.LogError("HasDiskCache:"+e.StackTrace);
        }

        if (diskData == null)
        {
            return false;
        }

        bool isVerify = VerifyCache(diskData);
        if (!isVerify)
        {
            SaveGameUtil.Inst.Delete(SaveGameUtil.gAccountKey);
        }
        // LoggerUtils.Log($"User disk cache : {isVerify}");
        return isVerify;
    }

    public void ReadCache()
    {
        if (!HasDiskCache())
        {
            return;
        }

        AccountData diskData = SaveGameUtil.Inst.Load<AccountData>(SaveGameUtil.gAccountKey, IsSaveEncode);
        if (diskData.platform != 0)
        {
            this.platform = (AccountPlatform)diskData.platform;
        }

        this.unionid = diskData.unionid;
        userInfo = diskData.userInfo;
        petInfo = diskData.petInfo;
        aibuddyInfo = diskData.aibuddyInfo;
        token = diskData.token;
        SyncUserInfoToNative();
        SetHttpTokenInfo(diskData);
    }

    public void DeleteCache()
    {
        userInfo = new AccountUserInfo();
        SaveGameUtil.Inst.Delete(SaveGameUtil.gAccountKey);
    }

    /// <summary>
    /// 同步磁盘缓存
    /// </summary>
    public void SyncDiskCache()
    {
        var diskData = new AccountData();
        diskData.token = token;
        diskData.userInfo = userInfo;
        diskData.platform = (int)this.platform;
        diskData.unionid = this.unionid;
        diskData.petInfo = petInfo;
        diskData.aibuddyInfo = aibuddyInfo;
        SaveGameUtil.Inst.Save(SaveGameUtil.gAccountKey, diskData, IsSaveEncode);
    }

    private void SetHttpTokenInfo(AccountData authData)
    {
        string uid =  authData.userInfo.uid;
        string token = authData.token;

#if UNITY_EDITOR
        if (DebugSetting.Inst != null)
        {
            if (!string.IsNullOrEmpty(DebugSetting.Inst.userAccount.uid) && !string.IsNullOrEmpty(DebugSetting.Inst.userAccount.token))
            {
                uid  = DebugSetting.Inst.userAccount.uid;
                token = DebugSetting.Inst.userAccount.token;
            }
        }
        this.platform = AccountPlatform.Tourists;
#endif

        SetHttpTokenInfo(uid,token);

        _balanceInfo.Refresh();
    }

    private void SetHttpTokenInfo(string uid, string token)
    {
        var tokenInfo = new Dictionary<string, string>();
        tokenInfo["uid"] = uid;
        tokenInfo["token"] = token;
        NetworkManager.Inst.SetHttpTokenInfo(tokenInfo);
    }

    private void RegisterNativeCallback()
    {
        Debug.Log($"AccountDataManager RegisterNativeCallback ");
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.replyUserInfo, ReplyUserInfo);
#if UNITY_ANDROID
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.checkOrderSuccess, CheckOrderSuccess);
#endif
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.realNameRegFailed, OnRealNameReqFailed);
    }

    private void CheckOrderSuccess(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.checkOrderSuccess);
        _balanceInfo.Refresh();
    }

    private void OnRealNameReqFailed(string data)
    {
        Debug.Log($"实名认证失败,data={data}");
        AnalyticsManager.Inst.Track(
                "realname_done",
                new Dictionary<string, object>
                {
                    ["uid"] = AccountDataManager.Inst.Uid,
                    ["EventType"] = data
                }
       );
    }


    private void ReplyUserInfo(string message)
    {
        var jb = new JObject
        {
            ["uid"] = Uid,
            ["token"] = token
        };

        var str = JsonConvert.SerializeObject(jb);

        LoggerUtils.Log("ReplyUserInfo5: "+ str);

        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.replyUserInfo, JsonConvert.SerializeObject(jb));
    }

    public bool IsSelf(string uid)
    {
        if (Inst==null) return false;
        return uid == Uid;
    }

    private bool OnceFlag = false;
    public void Init()
    {
        if (OnceFlag)
        {
            return;
        }
        OnceFlag = true;

        RegisterNativeCallback();
    }

    public void SyncUserInfoToNative()
    {
        LoggerUtils.Log("ReplyUserInfo4:");
        ReplyUserInfo("");
    }

    public override void Release()
    {
        base.Release();
        OnNickNameChange = null;
    }
}
