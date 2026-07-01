using System;
using System.Collections.Generic;
using Game.Audio;
using Game.Avatar;
using Game.AvatarTool;
using Game.Config;
using Game.Event;
using GameData;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using UI.Base;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;
using View.UI.PopupPanelSystem;
using xasset;
using Game;
using EventTracking;
using GameUI;
using System.Reflection;
using System.Collections;
/// <summary>
/// Author:
/// Desc:
/// Date:23-07-17 16:47:12
/// </summary>
public class SignInPanel : BasePanel<SignInPanel>
{
    [SerializeField] private Transform signInContainer;
    [SerializeField] private SignInButton signInBtn;
    [SerializeField] private Toggle privacyToggle;
    [SerializeField] private CButton ageTipBtn;
    [SerializeField] private CButton ageTipCloseBtn;
    [SerializeField] private GameObject ageTipView;
    [SerializeField] private CButton termBtn;
    [SerializeField] private CButton privacyBtn;
    [SerializeField] private CButton childBtn;
    [SerializeField] private CButton thirdParyBtn;
    [SerializeField] private Text version;
    [SerializeField] private CButton fullScreenBtn;
    [SerializeField] private BUDPrivacyPolicyView PrivacyPolicyView;
    [SerializeField] private GameObject LoadingObj;
    [SerializeField] private CButton icpBtn;
    [SerializeField] private GameObject mainViewCN;
    [SerializeField] private GameObject mainViewUS;
    [SerializeField] private GameObject privacyRoot;
    [SerializeField] private CButton startBtnUS;
    [SerializeField] private CButton fullScreenBtnUS;
    [SerializeField] private CButton loginBtnUS;
    [SerializeField] private LoginViewUS LoginViewUs;
    [SerializeField] private GameObject BgView;

    
    private List<SignInButton> _signInButtons = new List<SignInButton>();

    // 新用户登录信息
    private AccountUserInfo userInfo = new AccountUserInfo();
    private bool PrivacyFlag = false;
    private string banToastMsg = "";
    // 登录序号：每次发起登录自增，回调中比对，丢弃过期（被新登录取代）的登录结果
    private int _loginSeq = 0;
    // 记住上次测试登录的账号，重启后直接重登（不走原生第三方/实名）
    private const string TestAccountOpenIdKey = "LastTestAccountOpenId";
    private const string TestAccountNickKey = "LastTestAccountNick";
    static public bool isNewPlayer = false;
    private bool IsRunBackgroud = false;
    void Destory()
    {
        if (IsRunBackgroud == false)
        {
            Debug.Log("恢复不后台运行");
            Application.runInBackground = false;
        }
    }
    public override void OnCreate()
    {
#if PACKAGE_TYPE_US
        //@Jaywill TODO 待判断本地设置语言，目前写死
        LocalizationManager.Inst.SetLang(LangCode.en);
#endif
        IsRunBackgroud = Application.runInBackground;
        Debug.Log($"SignInPanel IsRunBackgroud={IsRunBackgroud}");
        if (IsRunBackgroud == false)
        {
            Debug.Log("先保持后台运行，不然实名认证会把unity转到后台，收不到消息");
            Application.runInBackground = true;
        }

        bool hasLoginIn = AccountDataManager.Inst.HasDiskCache();
        if (hasLoginIn)
        {   
            //如果缓存过登录时间，检查一下
            if (PlayerPrefs.HasKey("PlayerLoginTime"))
            {   
                if (PlayerPrefs.GetString("PlayerLoginTime") == DateTime.UtcNow.ToString("yyyyMMdd"))
                {
                    isNewPlayer = true;
                }
            }//如果没有缓存过，说明一定是老玩家，设置为开服时间
            else
            {
                PlayerPrefs.SetString("PlayerLoginTime", "20190909");//设置为开服时间
                PlayerPrefs.Save();
            }
        }
        else
        {   
            //如果没有标记时间，那么就当作新玩家，如果标记过了，看一下是否时当日注册
            if (!PlayerPrefs.HasKey("PlayerLoginTime") || PlayerPrefs.GetString("PlayerLoginTime") == DateTime.UtcNow.ToString("yyyyMMdd"))
            {
                isNewPlayer = true;
            }
        }
        AdjustSignInUI();

        BusinessLiveManager.Inst.GetBusinessConfig();

        AddListeners();

        bool isDidShowPrivacy = BUDPrivacyPolicyView.DidShow();
        PrivacyPolicyView.gameObject.SetActive(!isDidShowPrivacy);
        PrivacyPolicyView.AgreeAction = () =>
        {
            InitThinkingData();
        };
        if (isDidShowPrivacy)
        {
            InitThinkingData();
        }
        icpBtn.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.IcpPolicy);
        });
      //  OpenLogReport();
#if PACKAGE_TYPE_US
        AdapterUIByPackage();
#endif
        //var cg = GameObject.Find("CGVedio");
        //cg.transform.SetParent(transform);
        //cg.transform.localPosition = Vector3.zero;
        //cg.transform.localScale = Vector3.one;

        //if(CheckVideoVersion())
        //{
        //    BgView.SetActive(false);     
        //}

#if UNITY_ANDROID
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.forceLogout, ForceLogout);
#endif

        // 显示「测试登录」入口：仅未登录时显示。
        // 有记住的测试账号会在 AdjustSignInUI 自动重登（显示"开始游戏"）；有缓存老用户也是登录态——都不显示测试UI。
        // 短路：开关关闭（线上）时不读 PlayerPrefs，零影响。
        if (IsTestAccountLoginEnabled() && !hasLoginIn
            && string.IsNullOrEmpty(PlayerPrefs.GetString(TestAccountOpenIdKey, "")))
        {
            TestAccountLoginPanel.Instance.Show(OnTestAccountSelected);
        }
    }

    /// <summary>
    /// 清掉记住的测试账号（登出时调用），使下次回到登录页显示账号选择而非自动重登。
    /// 仅测试开关开启时生效；线上开关关闭时为空操作，零影响。
    /// </summary>
    public static void ClearRememberedTestAccount()
    {
        if (!IsTestAccountLoginEnabled())
        {
            return;
        }
        PlayerPrefs.DeleteKey(TestAccountOpenIdKey);
        PlayerPrefs.DeleteKey(TestAccountNickKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 是否启用测试账号登录入口。
    /// 通过反射读取 AOT 程序集里 GameStart.TestAccountLoginEntryEnabled 静态开关
    /// （热更程序集不能直接引用 AOT 的 GameStart）。读取失败一律视为关闭。
    /// 线上安全由底包隔离保证：正式底包不含该 AOT 代码/开关，反射读不到即返回 false。
    /// </summary>
    // 反射结果缓存：开关运行期不变，只反射一次，避免线上每次调用都反射+打日志
    private static bool? _testLoginEnabledCache;

    private static bool IsTestAccountLoginEnabled()
    {
        if (_testLoginEnabledCache.HasValue)
        {
            return _testLoginEnabledCache.Value;
        }

        bool enabled = false;
        try
        {
            Type type = Type.GetType("GameStart, Assembly-CSharp");
            if (type == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType("GameStart");
                    if (type != null)
                    {
                        break;
                    }
                }
            }

            var field = type?.GetField("TestAccountLoginEntryEnabled",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            enabled = field?.GetValue(null) is bool b && b;
        }
        catch (Exception)
        {
            enabled = false;
        }

        _testLoginEnabledCache = enabled;
        return enabled;
    }

    /// <summary>
    /// 选中测试账号后，以游客(Tourists)身份 + 固定 openId 直接登录后端。
    /// 不经过原生 U8SDK 渠道登录与实名认证流程；同一 openId 始终对应同一账号。
    /// </summary>
    private void OnTestAccountSelected(string openId, string nickname)
    {
        if (string.IsNullOrEmpty(openId))
        {
            return;
        }
        LoggerUtils.Log($"测试账号登录: openId={openId}, nickname={nickname}");
        userInfo.nickname = nickname;
        // 记住该测试账号，重启后直接重登（不走原生第三方/实名）
        PlayerPrefs.SetString(TestAccountOpenIdKey, openId);
        PlayerPrefs.SetString(TestAccountNickKey, nickname);
        PlayerPrefs.Save();
        // 登录开始即隐藏入口，避免覆盖后续的性别选择/大厅界面；失败时会重新显示
        if (TestAccountLoginPanel.InstExists)
        {
            TestAccountLoginPanel.Instance.Hide();
        }

        // 关键：取消「老用户自动登录」回调并清掉本地缓存，
        // 否则缓存账号的登录回调会在测试登录后返回，把账号覆盖成旧号。
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.OldSignAuth);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.OldSignAuth);
        AccountDataManager.Inst.DeleteCache();

#if UNITY_EDITOR
        // 编辑器下忽略 DebugSetting 账号覆盖，保证登录的就是所选测试账号（手机平台不编译）
        AccountDataManager.Inst.IgnoreDebugAccount = true;
#endif

        // isFirstLogin=true：新账号走性别/新手流程，老账号直接进大厅
        OnSignInDerect(AccountPlatform.Tourists, openId, new SignChannelInfo(), true);
    }

    private void ForceLogout(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.forceLogout);
        GameInstanceManager.Release();
        AccountDataManager.Inst.DeleteCache();
        // 登出时清掉记住的测试账号：回到登录页不再自动重登，从而显示测试账号选择，实现切换账号
        ClearRememberedTestAccount();
        LoginViewUs.ResetAllLoader();
        HideLoading();
        HideAllLoading();


        fullScreenBtn.interactable = true;
        SetupSignInView();

        // 登出后若开启测试登录，显示测试账号选择，便于切换账号
        if (IsTestAccountLoginEnabled())
        {
            TestAccountLoginPanel.Instance.Show(OnTestAccountSelected);
        }

        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout,
            "");
    }

    // protected override void OnEnable()
    //{
    //    base.OnEnable();
    //    if (DeviceInfoManager.Inst.CheckVersion_1_0_14())
    //    {
    //        BgView.SetActive(false);
    //        var cgVideo = GameObject.Find("UIRoot/Canvas/CGVedio");
    //        if (cgVideo == null)
    //        {
    //            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

    //            foreach (var assembly in assemblies)
    //            {
    //                if (assembly.FullName.Contains("Assembly-CSharp"))
    //                {
    //                    Type t = assembly.GetType("VideoTools");
    //                    if (t != null)
    //                    {
    //                        MethodInfo method = t.GetMethod("DoShowVideo");
    //                        IEnumerator enumerator = (IEnumerator)method.Invoke(null, null);
    //                        StartCoroutine(enumerator);
    //                    }
    //                }

    //            }
    //        }
    //    }
    //}


    #region 海外服适配
    private void AdapterUIByPackage()
    {
        bool hasLoginIn = AccountDataManager.Inst.HasDiskCache();
        PrivacyPolicyView.gameObject.SetActive(false);
        privacyRoot.SetActive(true);
        mainViewCN.SetActive(false);
        mainViewUS.SetActive(true);
        startBtnUS.GetComponentInChildren<Text>().SetLocalText(hasLoginIn ? "开始游戏" : "游客登陆");

        loginBtnUS.gameObject.SetActive(!hasLoginIn);//默认显示登录按钮，防止卡死玩家
    }

    private void AdapterSignInUI()
    {
        if (DeviceInfoManager.Inst.IsOldUsUser())
        {
            //老用户，且未登录过
            if (!AccountDataManager.Inst.HasDiskCache())
            {
                string uid = DeviceInfoManager.Inst.OldUserInfo.uid;
                string token = DeviceInfoManager.Inst.OldUserInfo.token;
                AccountDataManager.Inst.CreateUserInfo();
                AccountDataManager.Inst.UpdateAccountId(uid,token);
                //TODO: @Jaywill 从后端获取平台信息
            }
        }
        AdapterUIByPackage();
    }

    private void CheckUserBindState(AccountData authData)
    {
#if PACKAGE_TYPE_US
        if (authData.platform == (int)AccountPlatform.Tourists)
        {

        }
#endif
    }

    private void OnLoginBtnClickUS(AccountPlatform platform)
    {
        LoggerUtils.Log("###点击了登录："+platform);
        if (platform == AccountPlatform.Tourists)
        {
            SignInGuest();
        }
        else
        {
            GetSignInAuth(platform);
        }
    }

    private void OnStartBtnClick()
    {
    
#if UNITY_EDITOR
        if (DebugSetting.Inst != null)
        {
            string uid = DebugSetting.Inst.userAccount.uid;
            string token = DebugSetting.Inst.userAccount.token;
            AccountDataManager.Inst.CreateUserInfo();
            AccountDataManager.Inst.UpdateAccountId(uid,token);
            GetBindStatus();
        }
        return;
#endif
        LoggerUtils.Log("#####点击了海外服 开始游戏按钮");
        bool hasLoginIn = AccountDataManager.Inst.HasDiskCache();
        if (hasLoginIn)
        {
            LoggerUtils.Log("#####已登录");
            AccountDataManager.Inst.ReadCache();
            if (AccountDataManager.Inst.accountPlatform == AccountPlatform.Unknown)
            {
                SignInGuest();
                return;
            }
            PreloadUserAvatar(AccountDataManager.Inst.UserInfo);
            if (string.IsNullOrEmpty(AccountDataManager.Inst.accountUnionid))//原海外用户登录依旧是没有openId的
            {
                GetBindStatus();
            }
            else
            {
                OnSignInDerect(AccountDataManager.Inst.accountPlatform, AccountDataManager.Inst.accountUnionid, new SignChannelInfo(), true);
            }


        }
        else
        {
            //原海外老用户，且未登录过
            if (DeviceInfoManager.Inst.IsOldUsUser())
            {
                string uid = DeviceInfoManager.Inst.OldUserInfo.uid;
                string token = DeviceInfoManager.Inst.OldUserInfo.token;
                AccountDataManager.Inst.CreateUserInfo();
                AccountDataManager.Inst.UpdateAccountId(uid,token);
                GetBindStatus();
            }
            else
            {
                SignInGuest();
            }

        }
    }

    private string CreateGuestOpenId()
    {
        Guid uuid = Guid.NewGuid();
        DateTime now = DateTime.Now;
        long timestamp = now.Ticks / TimeSpan.TicksPerMillisecond;
        string openId = uuid.ToString() + "_" + timestamp.ToString();
        Debug.Log("### Create openId: " + openId);
        return openId;
    }

    private void SignInGuest()
    {
        LoggerUtils.Log("#####新游客登录");
        string openId = CreateGuestOpenId();
        var mockData = new AccountAuthData();
        mockData.username = "";
        mockData.platform = (int)AccountPlatform.Tourists;
        mockData.unionid = openId;
        HandleU8DataFromNative(JsonConvert.SerializeObject(mockData), true);
    }

    //从后端获取平台和用户信息
    private void GetBindStatus()
    {
        LoggerUtils.Log("####海外服老用户登录");
        ShowLoading();
        AccountDataManager.Inst.GetBindStatus((authData) =>
        {
            LoggerUtils.Log("####海外服老用户登录成功");
            onSignInSuccess(authData,true);
        }, (fRes) =>
        {
            LoggerUtils.Log("####海外服老用户登录失败");
            HandleLoginFail(false, fRes);
        });
    }


    #endregion

    //初始化数数
    private void InitThinkingData()
    {
     
#if PACKAGE_TYPE_US
        AnalyticsManager.Inst.InitAfterIfNeed();
        AnalyticsManager.Inst.Track(AnalyticsEventName.LANDINGPAGEVIEW);
        return;
#endif
#if UNITY_ANDROID
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.getChannelId, GetChannelId);
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.getChannelId,"");
#elif UNITY_IOS
       AnalyticsManager.Inst.InitAfterIfNeed();
       AnalyticsManager.Inst.Track(AnalyticsEventName.LANDINGPAGEVIEW);
#endif
#if UNITY_IOS
        if(GravityManager.Inst.Check())
        {
            GravityManager.Inst.StartEngine(); //初始化引力引擎

            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.initBDConvert, ""); //初始化巨量引擎
            GravityManager.Inst.BDEventRegister("bud", true);
            /**------巨量引擎检测需要上报，通过后可注掉-----*/
            //GravityManager.Inst.BDEventPurchase("gift", "flower", "008", 1, "wechat", "¥", true, 1);
            //GravityManager.Inst.BDEventV3("custom_event", new JObject() { ["test"] = "test" });

        }
#endif
    }

    private void GetChannelId(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.getChannelId);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        GetChannelIdResponse getchannelIdResponse = JsonConvert.DeserializeObject<GetChannelIdResponse>(message);
        IAPDataManager.Inst.channelId = getchannelIdResponse.channelId;
        int channelId = getchannelIdResponse.channelId;
        AnalyticsManager.Inst.InitAfterIfNeed(channelId);
        TapCoreManager.Inst.InitAfterIfNeed(channelId);
        AnalyticsManager.Inst.Track(AnalyticsEventName.LANDINGPAGEVIEW);
    }

    //TODO:暂时开启日志，后续可根据热更修改
    private void OpenLogReport()
    {
        GameObject rep = GameObject.Find("Reporter");
        if (rep == null)
        {
           Debug.LogError("Reporter is not exist");
        }
        if (rep.TryGetComponent<Reporter>(out var report))
        {
            if (DeviceInfoManager.Inst.Environment == GameEnvironment.PROD)
            {
                report.enabled = false;
            }
            else
            {
                report.enabled = true;
            }

#if PACKAGE_TYPE_US
            //测试代码：后续海外服prod需要关闭转圈圈
            report.enabled = true;
#endif
        }
    }

    private void AddListeners()
    {
        privacyToggle.isOn = PrivacyFlag;
        privacyToggle.onValueChanged.AddListener(OnPrivacyToggle);

        ageTipBtn?.onClick.AddListener(OnClickAgeTip);
        ageTipCloseBtn.onClick.AddListener(OnClickCloseAgeTip);
        termBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Permission);
        });
        privacyBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.Privacy);
        });
        childBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.ChildrenPolicy);
        });
        thirdParyBtn?.onClick.AddListener(() =>
        {
            BUDPrivacyPolicy.Open(BUDPrivacyPolicy.PrivacyType.ThreePartyInfo);
        });

        fullScreenBtn?.onClick.AddListener(OnClickScrren);

        loginBtnUS.onClick.AddListener(()=>{
            LoginViewUs.Show();
        });

        LoginViewUs.ClearLoginBtnListener();
        LoginViewUs.AddLoginBtnListener(OnLoginBtnClickUS);

        startBtnUS.onClick.AddListener(OnStartBtnClick);
        fullScreenBtnUS.onClick.AddListener(OnStartBtnClick);
    }

    private void AdjustSignInUI()
    {
        LoginViewUs.Hide();
        mainViewCN.SetActive(true);
        mainViewUS.SetActive(false);

#if !PACKAGE_TYPE_US
        // 记住的测试账号：开启测试登录且记录过测试账号时，直接用 openId 重登后端，
        // 绝不走原生 U8（第三方/实名）；其余正常流程不受影响。
        if (IsTestAccountLoginEnabled())
        {
            string savedTestOpenId = PlayerPrefs.GetString(TestAccountOpenIdKey, "");
            if (!string.IsNullOrEmpty(savedTestOpenId))
            {
                privacyToggle.gameObject.SetActive(false);
                fullScreenBtn?.gameObject.SetActive(true);
                // 与 SetupOldUserView 一致：创建老用户样式的"开始游戏"按钮，否则看不到可见的进入按钮
                SetupSignInButtons(new List<AccountPlatform>() { AccountPlatform.Tourists }, false, true);
                ShowLoading();
                userInfo.nickname = PlayerPrefs.GetString(TestAccountNickKey, "");
#if UNITY_EDITOR
                // 编辑器下忽略 DebugSetting 账号覆盖，保证重登的就是记住的测试账号（手机平台不编译）
                AccountDataManager.Inst.IgnoreDebugAccount = true;
#endif
                OnSignInDerect(AccountPlatform.Tourists, savedTestOpenId, new SignChannelInfo(), false);
                return;
            }
        }
#endif

        bool hasLoginIn = AccountDataManager.Inst.HasDiskCache();
        fullScreenBtn?.gameObject.SetActive(hasLoginIn);
        if (hasLoginIn)
        {
            ShowLoading();
            SetupOldUserView();
            privacyToggle.gameObject.SetActive(false);
        }
        else
        {
            SetupSignInView();
        }
    }

    /// <summary>
    /// 新用户登陆UI
    /// </summary>
    private void SetupSignInView()
    {
#if PACKAGE_TYPE_US
        fullScreenBtn?.gameObject.SetActive(false);
        AdapterSignInUI();
        return;
#endif

        fullScreenBtn?.gameObject.SetActive(false);
        privacyToggle.gameObject.SetActive(true);

        List<AccountPlatform> signInList = new List<AccountPlatform>
        {
            AccountPlatform.Wechat, AccountPlatform.Qq, AccountPlatform.Douyin
        };

        var isShowTaptap = DeviceInfoManager.Inst.ShowTaptapLogin;

#if UNITY_IPHONE
        if (DeviceInfoManager.Inst.ShowAppleLogin)
        {
            signInList.Add(AccountPlatform.Apple);
        }

        if (isShowTaptap)
        {
            signInList.Add(AccountPlatform.Taptap);
        }
        if (GlobalConfigManager.Instance.IsShowAppleAudit)
        {
            signInList.Add(AccountPlatform.AppleAudit);
        }

#elif UNITY_ANDROID
        if (isShowTaptap)
        {
            signInList.Add(AccountPlatform.Taptap);
        }
        if (DeviceInfoManager.Inst.ShowChannelLogin)
        {
            signInList = new List<AccountPlatform> {
                AccountPlatform.Channel
            };
        }
#endif

        SetupSignInButtons(signInList);
    }

    private void SetupSignInButtons(List<AccountPlatform> signInList, bool addAction = true,bool isOldUser = false)
    {
        if (signInList == null || signInList.Count == 0)
        {
            return;
        }
        foreach (var avatarCatagoryItem in _signInButtons)
        {
            GameObject.Destroy(avatarCatagoryItem.gameObject);
        }
        _signInButtons.Clear();

        if (isOldUser)
        {
            var signInData = signInList[0];
            var newItem = GameObject.Instantiate(signInBtn, signInBtn.transform.parent);
            newItem.transform.SetSiblingIndex(signInBtn.transform.GetSiblingIndex());
            newItem.gameObject.SetActive(true);
            newItem.UpdateStyle(signInData);
            _signInButtons.Add(newItem);
        }
        else
        {
            foreach (var accountPlatform in signInList)
            {
                var newItem = GameObject.Instantiate(signInBtn, signInContainer);
                newItem.gameObject.SetActive(true);
                newItem.UpdateStyle(accountPlatform);
                if (addAction)
                {
                    newItem.GetComponent<CButton>()?.onClick.AddListener(() => { onClickSignInBtn(accountPlatform); });
                }
                _signInButtons.Add(newItem);
            }
            if (isNewPlayer)
            {
                LoadEvent.ReportPopupStatus("1", "login_begin");
            }
        }
    }

    private void SetupOldUserView()
    {
        AccountDataManager.Inst.ReadCache();

        if (AccountDataManager.Inst.accountPlatform == AccountPlatform.Unknown)
        {
            SetupSignInView();
            return;
        }

        PreloadUserAvatar(AccountDataManager.Inst.UserInfo);
#if !PACKAGE_TYPE_US
        SetupSignInButtons( new List<AccountPlatform>() { AccountPlatform.Tourists }, false,true);
        ShowLoading();
        // 游客/测试账号(Tourists)：用缓存 openId 直接重登后端，不走原生 OldSignAuth，
        // 否则原生会对这个游客账号弹出 U8 登录界面。正常 CN 用户为微信/QQ，不进此分支。
        if (AccountDataManager.Inst.accountPlatform == AccountPlatform.Tourists)
        {
            OnSignInDerect(AccountPlatform.Tourists, AccountDataManager.Inst.accountUnionid, new SignChannelInfo(), false);
            return;
        }
        OldUserSignInDirect();
#endif
    }
    public void PreloadNewbeeUserCloth()
    {
        //新用户默认衣服
        var bData = Es.DataTables.GetAvatarCommonData(GameConsts.NewbeeDefCloth);
        if (bData == null)
        {
            return;
        }
        Loader.LoadAsync<GameObject>(bData.texDir + bData.prefabName +".prefab", (isSuc, warpper) =>
        {
        });
    }
    public void PreloadUserAvatar(AccountUserInfo UserInfo)
    {
        if (UserInfo!=null&&UserInfo.avatarInfo!=null)
        {
            foreach (var partData in UserInfo.avatarInfo.partDatas)
            {
                ResourceType resourceType = UniqueType.ResourceType(partData.Type);
                AvatarSubType avatarSubType = UniqueType.AvatarSubType(partData.Type);
                //只预加载主要部位
                if (avatarSubType == AvatarSubType.Clothes||avatarSubType == AvatarSubType.Shoe)
                {
                    if (resourceType == ResourceType.Avatar)
                    {
                        if (partData.IsNull()) continue;
                        var bData = Es.DataTables.GetAvatarCommonData(partData.Id);
                        if (bData == null)
                        {
                            continue;
                        }
                        Loader.LoadAsyncOrSync<GameObject>(bData.texDir + bData.prefabName +".prefab", (isSuc, warpper) =>
                        {
                        });
                    }
                    else if (resourceType == ResourceType.UgcAvatar)
                    {
                        if (partData.IsNull()) continue;
                        var dataList = Es.DataTables.GetUgcPartDataList();
                        var bData = dataList.Find(x => x.uId == partData.Id);
                        if (bData == null)
                        {
                            continue;
                        }
                        Loader.LoadAsyncOrSync<GameObject>(GameConsts.ClothesAssetDir + bData.prefabName + ".prefab", (isSuc, warpper) =>
                        {
                        });

                        Loader.LoadRemoteImageAsync(partData.Url);
                    }
                }

            }
        }

    }
    private void OnPrivacyToggle(bool isOn)
    {
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
        PrivacyFlag = isOn;
        if (!PlayerPrefs.HasKey("privacy_agree") && isOn && isNewPlayer)
        {
            PlayerPrefs.SetInt("privacy_agree", 1);
            PlayerPrefs.Save();
            LoadEvent.ReportPopupStatus("1", "privacy_agree");
        }
    }

#region Button Action

    private void OnClickAgeTip()
    {
        ageTipView.SetActive(true);
    }

    private void OnClickCloseAgeTip()
    {
        ageTipView.SetActive(false);
    }

    private void OldUserSignInDirect()
    {
        // AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_EnterGame_A1);
        fullScreenBtn.interactable = false;

        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.OldSignAuth, ReciveU8LoginSuccess);
        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.OldSignAuth, ReciveU8LoginFail);
        var jb = new JObject
        {
            ["platform"] = (int)AccountDataManager.Inst.accountPlatform,
            ["unionid"] = AccountDataManager.Inst.accountUnionid
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.OldSignAuth, JsonConvert.SerializeObject(jb));

#if UNITY_EDITOR
        var mockData = new AccountAuthData();
        mockData.username = LocalizationManager.Inst.GetLocalizedText("未命名");
        mockData.platform = (int)AccountDataManager.Inst.accountPlatform;
        mockData.unionid = AccountDataManager.Inst.accountUnionid;

        ReciveU8LoginSuccess(JsonConvert.SerializeObject(mockData));
#endif
    }

    private void HandleAppleDemoLogin(string uniId)
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.OldSignAuth, arg0 =>
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.OldSignAuth);
            MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.OldSignAuth);

            HandleU8DataFromNative(arg0, true);
        });
        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.OldSignAuth, ReciveU8LoginFail);

        var jb = new JObject
        {
            ["platform"] = (int)AccountPlatform.Apple,
            ["unionid"] = uniId
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.OldSignAuth, JsonConvert.SerializeObject(jb));

#if UNITY_EDITOR
        var mockData = new AccountAuthData();
        mockData.username = LocalizationManager.Inst.GetLocalizedText("未命名");
        mockData.platform = (int)AccountPlatform.Apple;
        mockData.unionid = uniId;

        HandleU8DataFromNative(JsonConvert.SerializeObject(mockData), true);
#endif
    }


    private void OnClickScrren()
    {
        if (!string.IsNullOrEmpty(banToastMsg))
        {
            TipPanel.ShowToast(banToastMsg);
            return;
        }
        GoGameHallPage();
    }

#endregion

    public override void OnShow(params object[] args)
    {
        LoggerUtils.Log("SignInPanel OnShow");
        version.text = "v " + Assets.HotUpdateVersion;
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
        if (TestAccountLoginPanel.InstExists)
        {
            TestAccountLoginPanel.Instance.Hide();
        }
    }

    //private void OnApplicationPause(bool paused)
    //{
    //    if (!paused) RestoreLandscape();
    //}

    //private void OnApplicationFocus(bool hasFocus)
    //{
    //    if (hasFocus) RestoreLandscape();
    //}

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }

#region Login Action

    private void onClickSignInBtn(AccountPlatform platform)
    {
        ShowLoading();
#if !PACKAGE_TYPE_US
        if (!PrivacyFlag)
        {
            TipPanel.ShowToast("请详细阅读并同意《用户协议》与《隐私政策》");
            return;
        }
#endif

        if (platform == AccountPlatform.AppleAudit)
        {
            var panel = UIManager.Inst.OpenPanel<AppleAuditInputPopupPanel>(PanelId.AppleAuditInputPopupPanel);
            panel.SuccessAction = s =>
            {
                HandleAppleDemoLogin(s);
            };
            return;
        }
        if (isNewPlayer)
        {
            LoadEvent.ReportPopupStatus(platform.ToString() , "register");
        }
        AnalyticsManager.Inst.Track(AnalyticsEventName.New_USER_LOGIN_IN_START);
#if UNITY_EDITOR
        userInfo.nickname = LocalizationManager.Inst.GetLocalizedText("未命名");
        DateTime now = DateTime.Now;
        long timestamp = now.Ticks / TimeSpan.TicksPerMillisecond;
        Debug.Log("timestamp: " + timestamp.ToString());
        OnSignInDerect(AccountPlatform.Tourists, timestamp.ToString(), channelInfo: new SignChannelInfo(), isFirstLogin: true);
        return;
#endif

        GetSignInAuth(platform);
    }

    private void GetSignInAuth(AccountPlatform platform)
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.GetSignInAuth, reciveAuthSuccessResult);
        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.GetSignInAuth, reciveAuthFailResult);
        var jb = new JObject
        {
            ["platform"] = (int)platform
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.GetSignInAuth, JsonConvert.SerializeObject(jb));
    }

    private void reciveAuthSuccessResult(string msg)
    {
        RestoreLandscape();
        AnalyticsManager.Inst.Track(AnalyticsEventName.New_USER_LOGIN_IN_SUCCESS);
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.GetSignInAuth);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.GetSignInAuth);
        if (isNewPlayer)
        {
            AccountAuthData authData = JsonConvert.DeserializeObject<AccountAuthData>(msg);
            LoadEvent.ReportPopupStatus("1", "realname_done");
        }
        HandleU8DataFromNative(msg, true);
    }

    private void reciveAuthFailResult(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.GetSignInAuth);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.GetSignInAuth);
        if (isNewPlayer)
        {
            LoadEvent.ReportPopupStatus("2", "realname_done");
        }
        HandleLoginFail();
    }

    public void OnSignInDerect(AccountPlatform platform, string openId, SignChannelInfo channelInfo, bool isFirstLogin)
    {
        ShowLoading();
        int seq = ++_loginSeq;
        AnalyticsManager.Inst.Track(AnalyticsEventName.USER_LOGIN_IN);
        AccountDataManager.Inst.SignIn(platform, openId, channelInfo, data =>
        {
            if (this == null || seq != _loginSeq)
            {
                return;
            }
            onSignInSuccess(data, isFirstLogin);

        }, fRes =>
        {
            if (this == null || seq != _loginSeq)
            {
                return;
            }
            HandleLoginFail(!isFirstLogin, fRes);
        });
    }

    private void onSignInSuccess(AccountData authData, bool isFirstLogin)
    {
        HideLoading();

        TapCoreManager.Inst.Login(authData.userInfo.uid);
        AnalyticsManager.Inst.Login(authData.userInfo.uid);
        AccountDataManager.Inst.isNewOpenld = authData.isNewOpenId;
        BuglyAgent.SetUserId(authData.userInfo.uid);
        LoggerUtils.Log($"onSignInSuccess uid: {authData.userInfo.uid},authData.isNewOpenld={authData.isNewOpenId}");
        LoggerUtils.Log($"ReplyUserInfo1: uid: {authData.userInfo.uid} ,token {authData.token}");
        userInfo = authData.userInfo;
        LoggerUtils.Log($"ReplyUserInfo12: uid: {authData.userInfo.uid} ,token {authData.token}");
        AccountDataManager.Inst.UpdateAccountId(authData.userInfo.uid, authData.token);
        AccountDataManager.Inst.SyncUserInfoToNative();

        BusinessLiveManager.Inst.GetBusinessConfig();

        //ActivityManager.Inst.ActivityReq();

        if (authData.IsNewUserRegister)
        {
            
            PlayerPrefs.SetString("PlayerLoginTime", DateTime.UtcNow.ToString("yyyyMMdd"));
            LoggerUtils.Log("玩家登录时间设置为:" + DateTime.UtcNow.ToString("yyyyMMdd"));
            PlayerPrefs.Save();
            //设置事件公共属性

            //设置监听
            //MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, SetEventPublicData);
            LoadEvent.ReportPopupStatus("1", "login_done");
            isNewPlayer = true;
            LoadEvent.RemoveNewPlayerStatus();
            GoGenderSelectPage();
            PreloadNewbeeUserCloth();
            //
            BudNewbieTaskV3Panel.bExitPopWindow = true;
        }
        else
        {
            Dictionary<string, object> trackData = new Dictionary<string, object>();
            trackData.Add("isFirstLogin",isFirstLogin);
            AnalyticsManager.Inst.Track(AnalyticsEventName.OLDUSER_GOTO_GAMEHALL,trackData);
            BootDataManager.Inst.SetPlayerIsOld(); //防止老玩家触发新手引导内容
            if (!isFirstLogin)
            { // 老用户登陆完后，等待用户点击再进去
                fullScreenBtn.interactable = true;
                TipPanel.ShowToast("登录成功");
                PreloadUserAvatar(userInfo);
                CheckUserBindState(authData);

                if (_signInButtons.Count == 1)
                {
                    _signInButtons[0].GetComponent<CButton>()?.onClick.RemoveAllListeners();
                    _signInButtons[0].GetComponent<CButton>()?.onClick.AddListener(OnClickScrren);
                }
                LoggerUtils.Log($"ReplyUserInfo2: uid: {authData.userInfo.uid} ,token {authData.token}");
                AccountDataManager.Inst.UpdateAccountId(authData.userInfo.uid, authData.token);
                AccountDataManager.Inst.SyncUserInfoToNative();
                return;
            }
            GoGameHallPage();
        }
        LoggerUtils.Log($"ReplyUserInfo3: uid: {authData.userInfo.uid} ,token {authData.token}");

    }
    //private void SetEventPublicData(CurrencyType _type)
    //{   

    //        var balanceInfo = AccountDataManager.Inst.BalanceInfo;
    //        var _userInfo = AccountDataManager.Inst.UserInfo;

    //    //if (AccountDataManager.Inst.GemCount == -1 || AccountDataManager.Inst.CoinCount == -1
    //    //    || AccountDataManager.Inst.PinkCoinCount == -1 || AccountDataManager.Inst.BadgeCount == -1
    //    //    || AccountDataManager.Inst.GemCount != balanceInfo.GetAccountCount(CurrencyType.Gem)
    //    //    || AccountDataManager.Inst.PinkCoinCount != balanceInfo.GetAccountCount(CurrencyType.PinkCoin)
    //    //    || AccountDataManager.Inst.BadgeCount != balanceInfo.GetAccountCount(CurrencyType.Badge)
    //    //    || AccountDataManager.Inst.CoinCount != balanceInfo.GetAccountCount(CurrencyType.Coin)
    //    // ) {
    //    //        AccountDataManager.Inst.GemCount = balanceInfo.GetAccountCount(CurrencyType.Gem);
    //    //        AccountDataManager.Inst.PinkCoinCount = balanceInfo.GetAccountCount(CurrencyType.PinkCoin);
    //    //        AccountDataManager.Inst.BadgeCount = balanceInfo.GetAccountCount(CurrencyType.Badge);
    //    //        AccountDataManager.Inst.CoinCount = balanceInfo.GetAccountCount(CurrencyType.Coin);

    //        Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
    //        superProperties["open_id"] = AccountDataManager.Inst.accountUnionid;//UID 
    //        superProperties["role_id"] = _userInfo.uid;//UID
    //        superProperties["role_name"] = _userInfo.nickname;//名字
    //        superProperties["diamond_amount"] = balanceInfo.GetAccountCount(CurrencyType.Gem);//剩余钻石数量
    //        superProperties["pinkcoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.PinkCoin);//剩余粉币数量
    //        superProperties["bluecoin_amount"] = balanceInfo.GetAccountCount(CurrencyType.Badge);//剩余徽章数量
    //        superProperties["coin_amount"] = balanceInfo.GetAccountCount(CurrencyType.Coin);//剩余金币数量
    //        superProperties["yoyocoin_amount"]       = balanceInfo.GetAccountCount(CurrencyType.YouYouCoin);
    //        superProperties["purplecoin_amount"]     = balanceInfo.GetAccountCount(CurrencyType.PurpleDreamCoin);
    //        superProperties["luckycoin_amount"]      = balanceInfo.GetAccountCount(CurrencyType.LuckyCoin);
    //        superProperties["creatorcoin_amount"]    = balanceInfo.GetAccountCount(CurrencyType.GreenCoin);
    //        superProperties["creator_energy_amount"] = balanceInfo.GetAccountCount(CurrencyType.EnergyCoin);
    //        superProperties["passcoin_amount"]       = balanceInfo.GetAccountCount(CurrencyType.SeasonPassCoin);
    //    superProperties["charge_amount"] = balanceInfo.GetAccountCount(CurrencyType.TotalRecharged);//累计充值金额
    //    superProperties["first_charge_amount"] = balanceInfo.GetAccountCount(CurrencyType.FirstRecharge);//首充金额
    //    AnalyticsManager.Inst.SetSuperProperties(superProperties);//设置公共事件属性
    //    //}
                
    //    return;
    //}

    private void onSignInFail(string msg)
    {
        LoggerUtils.Log("onSignInFail :" + msg);
    }

    private void RestoreLandscape()
    {
        // 先切到 AutoRotation（landscape-only），强制 iOS UIKit 感知到方向变化
        // 再设 LandscapeLeft，否则 Unity 认为状态未变而不重发旋转请求
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    private void GoGameHallPage()
    {
#if UNITY_IOS
        Dictionary<string, object> properties = new Dictionary<string, object>();
        properties["nick"] = userInfo.nickname; //昵称
        properties["birthday"] = userInfo.birthday;
        properties["$app_id"] = userInfo.uid;

        GravityManager.Inst.Track("$AppLogin", properties);
#endif
        RestoreLandscape();
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_EnterGame_A1);
        MessageHelper.Broadcast(MessageName.LoginSuccess);

        //TCP
        TcpLoginManager.Instance.Login(AccountDataManager.Inst.Uid);
        EventCenterDataManager.Inst.GetTaskInfo();
        EventCenterDataManager.Inst.GetActivityInfo();
        if (isNewPlayer)
        {
            LoadEvent.ReportPopupStatus("1", "load_begin");
        }
        CloseSelf();
        var hallPanel = UIManager.Inst.OpenPanel<GameHallPanel>(PanelId.GameHallPanel);
        AccountDataManager.Inst.RefreshUserInfo((resData) =>
        {
            hallPanel.OnIdleChange(resData.userInfo,resData.petInfo, resData.aibuddyInfo, resData.GetLobbyVehicleInfo());
        });
        DynamicAddSunshineNativeObj();
    }

    private void HideAllLoading()
    {
        foreach (var signInButton in _signInButtons)
        {
            signInButton.HideLoading();
        }
    }

    private void ReciveU8LoginFail(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.OldSignAuth);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.OldSignAuth);

        HandleLoginFail(true);
    }

    private void ReciveU8LoginSuccess(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.OldSignAuth);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.OldSignAuth);

        HandleU8DataFromNative(msg, false);
    }


    /// <summary>
    /// 成功接收u8登陆后回调
    /// </summary>
    /// <param name="msg">回调信息</param>
    /// <param name="isFirstLogin">是否走新用户流程， 老用户登陆需要用户点击在进入</param>
    private void HandleU8DataFromNative(string msg, bool isFirstLogin)
    {
        AccountAuthData authData = JsonConvert.DeserializeObject<AccountAuthData>(msg);
        if (string.IsNullOrEmpty(authData.unionid))
        {
            return;
        }

        userInfo.nickname = authData.username;
        var channel = authData.channelInfo;
        if (channel == null)
        {
            channel = new SignChannelInfo();
        }

        if (!string.IsNullOrEmpty(authData.username))
        {
            channel.sdkUsername = authData.username;
        }

        OnSignInDerect((AccountPlatform)authData.platform, authData.unionid, channel, isFirstLogin);
    }

    private void HandleLoginFail(bool isOldUser = false, HttpResponseFailDataStruct fRes = null)
    {
        RestoreLandscape();
        LoginViewUs.ResetAllLoader();
        HideLoading();
        HideAllLoading();

        if (fRes != null && fRes.result == HttpCodeDefine.AccountBan && !string.IsNullOrEmpty(fRes.rmsg))
        {
            TipPanel.ShowToast(fRes.rmsg);
            fullScreenBtn.interactable = true;
            banToastMsg = fRes.rmsg;
            return;
        }

        if (isOldUser)
        {
            fullScreenBtn.interactable = true;
            SetupSignInView();
        }

        var toast = fRes?.rmsg ?? "登录失败，请重试";
        if (!string.IsNullOrEmpty(toast))
        {
            TipPanel.ShowToast(toast);
        }

        // 登录失败，恢复测试登录入口以便重试（仅在开关开启时）
        if (IsTestAccountLoginEnabled() && TestAccountLoginPanel.InstExists)
        {
            TestAccountLoginPanel.Instance.Show(OnTestAccountSelected);
        }
    }

#endregion


#region Gender

    const string CharacterIconPath = "https://cdn.budapp.cn/Static/portrait/{0}.png?imageMogr2/thumbnail/256x256/Static/portrait/{0}.png";

    private void GoGenderSelectPage()
    {
        Action<SavingData.GenderType ,int, string> selectedFinsh = (SavingData.GenderType gender , int index, string avataJason) =>
        {
            userInfo.gender = (int)gender;
            userInfo.avatarJson = avataJason;
            userInfo.portraitUrl = string.Format(CharacterIconPath, index.ToString("00"));
            AvatarDataManager.Inst.InitSelfData((int)gender);
            AccountDataManager.Inst.CompleteNewUser(userInfo, OnSaveImageComplete);
        };

        UIManager.Inst.OpenPanel(PanelId.GenderSelectPanel, selectedFinsh);
    }

    private void OnSaveImageComplete(bool result)
    {
        if (UIManager.Inst.TryFindPanel<GenderSelectPanel>(PanelId.GenderSelectPanel, out var panel))
        {
            panel.HideLoading();
        }

        if (result)
        {
            var imgJson = AccountDataManager.Inst.UserInfo.avatarJson;
            if (!string.IsNullOrEmpty(imgJson))
            {
                AvatarDataManager.Inst.SelfCharacterData = CharacterData.DeserializeObject(imgJson);
            }
            SetAvatarJason();
            UIManager.Inst.ClosePanel(PanelId.GenderSelectPanel);
            AccountDataManager.Inst.SyncPetData(true); // 新用户默认隐藏宠物
            
            //TCP
            TcpLoginManager.Instance.Login(AccountDataManager.Inst.Uid);
            EventCenterDataManager.Inst.GetTaskInfo();
            EventCenterDataManager.Inst.GetActivityInfo();

            CloseSelf();

            Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();
            superProperties["open_id"] = AccountDataManager.Inst.accountUnionid;//UID 
            AnalyticsManager.Inst.SetSuperProperties(superProperties);//设置公共事件属性

            var prop = new Dictionary<string, object>();
            prop.Add("is_new_openid", AccountDataManager.Inst.isNewOpenld);
            Debug.Log("AccountDataManager.Inst.isNewOpenld =" + AccountDataManager.Inst.isNewOpenld);
            AnalyticsManager.Inst.Track(AnalyticsEventName.FINISHSIGNUP, prop);
           
            var dict = new Dictionary<string, object>();
            dict.Add("open_id", AccountDataManager.Inst.accountUnionid ?? "");
            AnalyticsManager.Inst.UserSet(dict);

            var hallPanel = UIManager.Inst.OpenPanel<GameHallPanel>(PanelId.GameHallPanel);
            hallPanel.OnIdleChange(AccountDataManager.Inst.UserInfo,AccountDataManager.Inst.PetInfo, AccountDataManager.Inst.AIBuddyInfo);
        }
    }
    private void SetAvatarJason()
    {
        SetImageReq req = new SetImageReq();
        req.userInfo = new AccountUserInfo
        {
            avatarJson = userInfo.avatarJson
        };
        req.setType = 4;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setImage,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {

            }, onFail: arg0 =>
            {
            }, retryCount: 3);
    }


#endregion

    private void DynamicAddSunshineNativeObj()
    {
        LoggerUtils.LogError("try DynamicAddSunshineNativeObj");
        if (DeviceInfoManager.Inst.DeviceBaseData.CompareVersion(
            DeviceInfoManager.Inst.DeviceBaseData.version, 
            AIGameHospitalConfig.shareNeedVersion) > 0)
        {
            try
            {
                GameObject sunshineNativeObj = new GameObject("NativeShareObj");
                
                // 添加基础组件
                AddComponentSafely<SunShineNativeShare>(sunshineNativeObj);
                sunshineNativeObj.AddComponent<DontDestroyGameObject>();

                // 根据平台添加对应组件
#if UNITY_EDITOR || UNITY_ANDROID
                    AddComponentSafely<AndroidShare>(sunshineNativeObj);
#elif UNITY_IPHONE || UNITY_IOS
                    AddComponentSafely<IosShare>(sunshineNativeObj);
#endif
            }
            catch (System.Exception e)
            {
                LoggerUtils.LogError($"DynamicAddSunshineNativeObj 添加组件失败: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    private void AddComponentSafely<T>(GameObject target) where T : Component
    {
        try
        {
            string componentName = typeof(T).Name;
            if (target.GetComponent<T>() == null)
            {
                target.AddComponent<T>();
                LoggerUtils.Log($"成功添加组件: {componentName}");
            }
            else
            {
                LoggerUtils.Log($"组件已存在: {componentName}");
            }
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"添加组件 {typeof(T).Name} 失败: {e.Message}");
        }
    }

    // 如果需要使用反射方式添加
    private void AddComponentByReflection(GameObject target, string componentName)
    {
        try
        {
            // 获取组件类型
            System.Type componentType = System.Type.GetType(componentName);
            if (componentType == null)
            {
                // 尝试在程序集中查找
                componentType = System.Reflection.Assembly.GetExecutingAssembly().GetType(componentName);
            }

            if (componentType == null)
            {
                LoggerUtils.LogError($"找不到组件类型: {componentName}");
                return;
            }

            // 检查组件是否已存在
            if (target.GetComponent(componentType) == null)
            {
                // 使用反射添加组件
                target.AddComponent(componentType);
                LoggerUtils.Log($"通过反射成功添加组件: {componentName}");
            }
            else
            {
                LoggerUtils.Log($"组件已存在: {componentName}");
            }
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"反射添加组件 {componentName} 失败: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ShowLoading()
    {
        if (LoadingObj != null)
        {
            LoadingObj.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("LoadingObj is NUll" + (this == null).ToString());
        }

    }

    private void HideLoading()
    {
        if (LoadingObj != null)
        {
            LoadingObj.gameObject.SetActive(false);
        }
    }

}
