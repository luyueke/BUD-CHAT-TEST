using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileInterfaceDefine
{
    /// <summary>
    /// 获取设备的基本信息。 eg: 环境、设备型号等
    /// </summary>
    public const string GetBaseInfo = "getBaseInfo";

    /// <summary>
    /// 获取端上登录授权
    /// </summary>
    public const string GetSignInAuth = "getSignInAuth";

    public const string OldSignAuth = "oldSignAuth";
    
    public const string showKeyboard = "showKeyboard";

    public const string hideKeyboard = "hideKeyboard";

    public const string getMediaFromNative = "getMediaFromNative";
    
    public const string replyUserInfo = "replyUserInfo";

    public const string openWebview = "openWebview";

    public const string openSystemAlbum = "openSystemAlbum";
    
    public const string saveMediaToLocal = "saveMediaToLocal";
    
    public const string startBillingFlow = "startBillingFlow"; //58. Unity调用购买开启购买流程
    public const string startBillingFlowCallback = "startBillingFlowCallback"; //58. 回调
    
    
    public const string refreshIconBadgeNumber = "refreshIconBadgeNumber";
    
    public const string logout = "logout";

    public const string getAvailableFreeSpace = "getAvailableFreeSpace";

    /// <summary>
    /// 退出App, kill App
    /// </summary>
    public const string killApp = "killApp";
    
    /// <summary>
    /// 获取渠道id
    /// </summary>
    public const string getChannelId = "getChannelId"; 
    
    /// <summary>
    /// 华为渠道自动补单成功
    /// </summary>
    public const string checkOrderSuccess = "checkOrderSuccess"; 
    
    //打开应用商店 - 只有安卓
    public const string openNativeStore = "openNativeStore";
    //打开应用商店评分 - 只有安卓
    public const string openNativeMarketReview = "openNativeMarketReview";
    
    public const string forceLogout = "forceLogout"; 
    
    public const string getPriceList = "getPriceList";//获取商品本地化价格

    public const string bindThirdAccount = "bindThirdAccount";//绑定账号
    
    /// <summary>
    ///  IOS Only. 获取universalLink透传信息
    /// </summary>
    public const string universalLinkActivityInfo = "universalLinkActivityInfo";

    public const string quickEmote = "quickEmote";

    /// <summary>
    /// 初始化巨量引擎
    /// </summary>
    public const string initBDConvert = "initBDConvert";

    public const string onBDEventRegister = "onBDEventRegister"; //巨量引擎 投放深度转化须上报register：注册、purchase：支付两个埋点事件，由SDK预定义，

    public const string onBDEventPurchase = "onBDEventPurchase"; //巨量引擎 投放深度转化须上报register：注册、purchase：支付两个埋点事件，由SDK预定义，

    public const string onBDEventV3 = "onBDEventV3"; //- 巨量引擎自定义埋点事件


    /// <summary>
    /// 实名认证失败
    /// </summary>

    public const string realNameRegFailed = "realNameRegFailed";

    public const string jumpVivoGameCenter = "jumpVivoGameCenter"; //跳转到vivo游戏中心

    public const string vivoStartFromGameCenter = "vivoStartFromGameCenter"; //vivo 从游戏中心启动 状态变化推给unity
    public const string isStartFromVivoGameCenter = "isStartFromVivoGameCenter"; //vivo 从游戏中心启动 主动获取

    public const string taptapSetUserId = "taptapSetUserId";//taptap设置用户ID
    public const string taptapClearUser = "taptapClearUser";//taptap清除id

    /// <summary>
    /// app icon设置
    /// </summary>
    public const string SetAppIcon = "SetAppIcon";
    public const string GetAppIcon = "GetAppIcon";
    public const string openLocationSettings = "openLocationSettings"; //1.0.20新增

    // 屏幕旋转：旋转前添加黑色覆盖层，旋转后移除覆盖层（底包 >= 1.0.20）
    public const string SOH_DisableUIKitAnimation = "SOH_DisableUIKitAnimation";
    public const string SOH_EnableUIKitAnimation  = "SOH_EnableUIKitAnimation";
}
