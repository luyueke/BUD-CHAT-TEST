using Basic;
using Basic.Utils;
using Game.Audio;
using Game.GameSetting;
using GameData;
using GameData.BindPropertyDefine;
using GameData.Manager;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;

public static class HotfixEntry
{
    private static bool DidLoadAsset = false;
    private static bool DidGetConfig = false;
    public static void Start()
    {
        Debug.Log("Bud 进入游戏");
        Assets.FastVerifyMode = false;
        if (GlobalConfigManager.Instance.IsGetConfigData)
        {
            DidGetConfig = true;
        }
        else
        {       
            GlobalConfigManager.Instance.Refresh();
            GlobalConfigManager.Instance.Complete = b =>
            {
                DidGetConfig = true;
                EnterGamePage();
            };
        }
        LoadAllModule();
       // CoroutineManager.Inst.StartCoroutine( LoadAllModule());
    }

    private static void LoadAllModule()
    {
        Debug.Log("Bud HotfixEntry LoadAllModule");
        InitBasicModule();
        NetworkManager.Inst.Init();
       AkSoundManager.Inst.Init();

        Es.DataTables _dataTables = new Es.DataTables();
        //UnityEngine.Profiling.Profiler.BeginSample("LoadTable");
        _dataTables.Load(new EsDataLoader());
        //UnityEngine.Profiling.Profiler.EndSample();
        //yield return _dataTables.LoadAsync(new EsDataLoader());

        DeviceInfoManager.Inst.RefreshDeviceInfo(() =>
        {
            NetworkManager.Inst.SetHttpUrl(DeviceInfoManager.Inst.Environment);
            NetworkManager.Inst.SetHotUpdateVersion(Assets.HotUpdateVersion);
            var reqHeader = DeviceInfoManager.Inst.DeviceBaseData.HttpRequestHeader();
            if (reqHeader != null)
            {
                NetworkManager.Inst.SetHttpTokenInfo(reqHeader);
            }
            
            BusinessLiveUtils.InitBusinessConfig();//因为涉及到网络请求，所以需要再判断环境后再请求

            GetAppIcon();
        });

        UIManager.Inst.Init();
        AIParkStoreManager.Inst.Init();
        RedDotManager.Inst.Init();
        EnterGame();
        GetAppSetting();
 
     //   yield return null;
    }

    private static int appIconIndex = 1;
    private static void GetAppIcon()
    {
        if(DeviceInfoManager.Inst.CheckVersion_1_0_18() == false) //低版本不请求
        {
            return;
        }
#if UNITY_ANDROID
        return;
#endif
        IAPDataManager.ChannelIdEnum channelId = (IAPDataManager.ChannelIdEnum) IAPDataManager.Inst.channelId;
        if ( channelId == IAPDataManager.ChannelIdEnum.BiliBili || channelId == IAPDataManager.ChannelIdEnum.KuaiShou || channelId == IAPDataManager.ChannelIdEnum.Tencent)
        { //这几个有角标的先不更换
            return; 
        }
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AppIconInfo, HttpMethod.GET,"",
        onReceive: arg0 =>
        {
            AppIconData data = JsonConvert.DeserializeObject<AppIconData>(arg0);
            if (data != null)
            {
                appIconIndex = int.Parse(data.index); //服务器返回的appicon索引

                MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.GetAppIcon, GetAppIconBack);
                // 调用原生接口，获取APPICON索引
                MobileInterface.Instance.SendMessage(MobileInterfaceDefine.GetAppIcon, "");  

            }
        },
        onFail: arg0 =>
        {
            Debug.LogError($"{HttpUrlDefine.AppIconInfo}  is request Fail");
        }
        , null, 0, 3);
    }

    private  static  void GetAppIconBack(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.GetAppIcon);
        var index = int.Parse(message); //原生端返回的索引
        if(index != appIconIndex) //与服务器返回的数据不一致，通知原生端修改appicon
        {
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.SetAppIcon, appIconIndex.ToString());
        }  
    }
        private static void GetAppSetting()
    {
        GameDataManager.Inst.appSetting.quality =
            (int) EquipmentSizingGame.CurEquipmentSizing.modelClassificationType;
        
        JObject deviceReq = new JObject()
        {
            ["deviceModel"] =  SystemInfo.deviceModel,
            ["graphicsDeviceName"] =  SystemInfo.graphicsDeviceName,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.appSetting, HttpMethod.GET, JsonConvert.SerializeObject(deviceReq),
            onReceive: arg0 =>
            {
                AppSettingData data = JsonConvert.DeserializeObject<AppSettingData>(arg0);
                if (data != null)
                {
                    GameDataManager.Inst.appSetting = data;
                    SetGPUInstance(data.closeGPU);
                    SetMobileQuality();
                }
            },
            onFail: arg0 =>
            {
                Debug.LogError($"{HttpUrlDefine.appSetting}  is request Fail");
            }
            ,null,0,3);
    }
    
    private static void SetGPUInstance(bool closeGPU)
    {
        EquipmentSizingGame.SetServerSupportGPU(closeGPU);
    }

    private static void SetMobileQuality()
    {
        if (GameDataManager.Inst.appSetting.quality == (int)ModelClassificationType.Default)
        {
            GameDataManager.Inst.appSetting.quality =
                (int) EquipmentSizingGame.CurEquipmentSizing.modelClassificationType;
        }
    }

    private static void InitBasicModule()
    {
        JsonHelper.Init();
        SaveGameUtil.Inst.Init();
        TimerManager.Inst.Init();
        GameMonoManager.Inst.Init();
        InputReceiver.Inst.Init();
        SandboxStorage.Init();
        GameTimeUtils.Inst.Init();
    }


    private static void DataModuleTest()
    {
        TestData1 data1 = new TestData1();
        data1.tip.Value = "11111111111";
        data1.number.Value = 1;

        // data1.tip.OnValueChanged.AddListener((oldS, newS) =>
        // {
        //     Debug.Log($"fsc-----tip valueChanged----oldv--{oldS}, newS:{newS}");
        // });
        // data1.number.OnValueChanged.AddListener((oldS, newS) =>
        // {
        //     Debug.Log($"fsc-----number valueChanged----oldv--{oldS}, newS:{newS}");
        // });
        data1.pos.OnValueChanged.AddListener((oldS, newS) =>
        {
            Debug.Log($"fsc-----pos valueChanged----oldv--{oldS}, newS:{newS}");
        });
        data1.OnValueChanged.AddListener((oldV, newV) =>
        {
            Debug.Log($"fsc---------oldv--{JsonConvert.SerializeObject(oldV)}");
            Debug.Log($"fsc---------newV--{JsonConvert.SerializeObject(newV)}");
        });


        // data1.tip.Value = "222222222222222222222222"; //触发 data1.tip.OnValueChanged
        // data1.number.Value = 5555555; //触发 data1.number.OnValueChanged

        data1.SetData(new TestData1() //触发 data1.OnValueChanged
        {
            number = new BindInt(100),
            tip = new BindString("lalalala"),
            pos = new BindVector3(666,555,444)
        });
    }

    private static void EnterGame()
    {
        LoggerUtils.Log("Enter Game");

        var frameworkObj = GameObject.Find("Framework");
        if (frameworkObj != null)
        {
            Object.DontDestroyOnLoad(frameworkObj);
        }

        LoggerUtils.Log("start LoadAsync");
        var loadRequest = xasset.Scene.LoadAsync("Assets/Arts/Scenes/GameHall.unity");
        Debug.Log("loadRequest: " + (loadRequest == null));
        loadRequest.completed += request1 =>
        {
            DidLoadAsset = true;
            EnterGamePage();
        };
    }

    private static void EnterGamePage()
    {
        if (!DidLoadAsset || !DidGetConfig)
        {
            Debug.LogError("[HotfixEntry] EnterGamePage fail");
            return;
        }
         
        Debug.Log("completed EnterGame:");
        ClearCanvasPanel();
        GlobalCameraManager.Inst.Init();
        UIManager.Inst.OpenPanel(PanelId.SignInPanel);
        DidGetConfig = false;
        DidLoadAsset = false;
        GlobalConfigManager.Instance.Complete = null;
        GameGlobalMgr.Inst.Init();

        _ = ActivityManager.Inst;
    }
    
    private static void ClearCanvasPanel()
    {
        var appCamera = GameObject.Find("Camera");
        
        GameObject.Destroy(appCamera);
  
    }
}
