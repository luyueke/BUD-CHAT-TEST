using GameData.Base;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;
using UnityEngine;

public class UpdateTipsPanel : BasePanel<UpdateTipsPanel>
{
    public CButton Btn_Close;
    public Text Txt_Content;
    public CButton Btn_Confirm;
    public Text Txt_BtnConfirm;

    private ForceUpdate _curType = ForceUpdate.Default;

    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _curType = (ForceUpdate)args[0];
        switch (_curType)
        {
            case ForceUpdate.AppUpdate:
                Txt_Content.SetLocalText("有新的客服端版本可以用，\n请到应用商店进行更新，体验新内容游玩更顺畅");
                Txt_BtnConfirm.SetLocalText("更新");
                break;

            case ForceUpdate.HotUpdate:
                Txt_Content.SetLocalText("检测到你的版本过旧，\n重新启动游戏进行热更新即可查看该作品");
                Txt_BtnConfirm.SetLocalText("重新启动");
                break;

            case ForceUpdate.NeedUpdateFeature:
                Txt_Content.SetLocalText("该功能需要更新到最新的客户端版本才能使用，\n请前往您下载的渠道进行更新哦");
                Txt_BtnConfirm.SetLocalText("更新");
                break;
            case ForceUpdate.CustomUpdate:
                if (args.Length == 2)
                {
                    Txt_Content.SetLocalText(args[1].ToString());
                }
                Txt_BtnConfirm.SetLocalText("更新");
                break;
            case ForceUpdate.Default:

                break;
        }
    }

    private void OnBtnConfirmClick()
    {
        switch (_curType)
        {
            case ForceUpdate.NeedUpdateFeature:
            case ForceUpdate.AppUpdate:
            case ForceUpdate.CustomUpdate:

#if UNITY_ANDROID
        #if PACKAGE_TYPE_US
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openNativeStore, "");
        #else
             if (DeviceInfoManager.Inst.DeviceBaseData.version == "1.0.1" || DeviceInfoManager.Inst.DeviceBaseData.version == "1.0.0")
            {
                //S1
                switch (IAPDataManager.Inst.channelId)
                {

                    case 2: //官服
                    case 14: //TapTap
                        Application.OpenURL("https://www.budapp.cn/");
                        break;
                    default:
                        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openNativeStore, "");
                        break;
                }
            }
            else
            {
                switch ((IAPDataManager.ChannelIdEnum)IAPDataManager.Inst.channelId)
                {
                    case IAPDataManager.ChannelIdEnum.Tencent:    // 应用宝
                        Application.OpenURL("https://a.app.qq.com/o/simple.jsp?pkgname=com.tencent.tmgp.budapp&fromcase=70051&g_f=1182517&scenevia=XQYFX");
                        break;
                    case IAPDataManager.ChannelIdEnum.BiliBili:   // B站
                        Application.OpenURL("https://app.biligame.com/page/detail_share2.html?id=111855&sourceFrom=23006&_1733378618816");
                        break;
                    case IAPDataManager.ChannelIdEnum.GameCenter: // 4399
                        Application.OpenURL("http://a.4399.cn/mobile/221228.html?from=yxh");
                        break;
                    case IAPDataManager.ChannelIdEnum.Douyin:     // 抖音
                        TipPanel.ShowToast("您的账号为抖音渠道服账号，请前往抖音进行包体更新");
                        break;
                    case IAPDataManager.ChannelIdEnum.KuaiShou:   // 快手
                        TipPanel.ShowToast("您的账号为快手渠道服账号，请前往快手进行包体更新");
                        break;
                    case IAPDataManager.ChannelIdEnum.ChongChong: // 虫虫游戏
                        Application.OpenURL("https://wap.ccplay.com/package/226544.html");
                        break;
                    case IAPDataManager.ChannelIdEnum.HaoyouKuaiBao: // 好游快爆
                        Application.OpenURL("https://www.3839.com/a/170822.htm?from=hykb");
                        break;
                    case IAPDataManager.ChannelIdEnum.Vivo:       // Vivo
                    case IAPDataManager.ChannelIdEnum.Oppo:       // Oppo
                    case IAPDataManager.ChannelIdEnum.Honor:      // 荣耀
                    case IAPDataManager.ChannelIdEnum.Huawei:     // 华为
                    case IAPDataManager.ChannelIdEnum.Xiaomi:     // 小米
                    case IAPDataManager.ChannelIdEnum.Official: // 官服
                    case IAPDataManager.ChannelIdEnum.TapTap:   // TapTap
                    case IAPDataManager.ChannelIdEnum.Unknown: // 未知渠道
                    default:
                        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openNativeStore, "");
                        break;
                }
                }
        #endif
               
#elif UNITY_IOS
                var appStore = "itms-apps://itunes.apple.com/app/apple-store/id6450975322";
        #if PACKAGE_TYPE_US
                appStore = "itms-apps://itunes.apple.com/app/apple-store/id1590291415";
        #endif
                Application.OpenURL(appStore);
#endif
                break;

            case ForceUpdate.HotUpdate:
                MobileInterface.Instance.SendMessage(MobileInterfaceDefine.killApp, "");
                break;

            case ForceUpdate.Default:
                break;
        }
    }
}
