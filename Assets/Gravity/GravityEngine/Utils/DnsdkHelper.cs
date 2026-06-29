#if GRAVITY_WECHAT_GAME_MODE && ENABLE_TENCENT_SDK_TRACK
using System.Runtime.InteropServices;

/**
 * 小游戏 DN SDK 辅助类
 */
public class DnSdkHelper
{
    [DllImport("__Internal")]
    public static extern void SetOpenId(string openid);
    [DllImport("__Internal")]
    public static extern void onPurchase(int purchaseValue);
    [DllImport("__Internal")]
    public static extern void onRegister();
    [DllImport("__Internal")]
    public static extern void onReActive(int backFlowDay);
    [DllImport("__Internal")]
    public static extern void onAddToWishlist(string type);
    [DllImport("__Internal")]
    public static extern void onShare(string target);
    [DllImport("__Internal")]
    public static extern void onCreateRole(string roleName);
    [DllImport("__Internal")]
    public static extern void onTutorialFinish();
    [DllImport("__Internal")]
    public static extern void onUpdateLevel(int level, int power);
    [DllImport("__Internal")]
    public static extern void onViewContent(string item);
}
#endif