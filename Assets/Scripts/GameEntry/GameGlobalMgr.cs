
using Message;
using Network.Tcp.Core;

/// <summary>
/// 游戏全局事件监听
/// </summary>
public  class GameGlobalMgr
{
    private static GameGlobalMgr instance;

    private GameGlobalMgr() { }

    public static GameGlobalMgr Inst
    {
        get
        {
            if (instance == null)
            {
                instance = new GameGlobalMgr();
            }
            return instance;
        }
    }

    public void Init()
    {
        MessageHelper.AddListener<string>(MessageName.ChatMutePopup, ChatMutePopup);
        MessageHelper.AddListener<string>(MessageName.AccountSuspension, AccountSuspension);
    }

    //禁言
    public void ChatMutePopup(string msg)
    {
        LoggerUtils.Log($"响应消息 ID ={NetMsg.MSG_MUTE_POPUP},Msg={msg}");
        UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
        {
            CanClose = true,
            ConfirmString = "确定",
            ContextString = msg,
            TopTitleString = "提示",
        });
    }
    //封号
    public void AccountSuspension(string msg)
    {
        LoggerUtils.Log($"响应消息 ID ={NetMsg.MSG_ACCOUNT_SUSPENSION},Msg={msg}，CurWindID={UIManager.Inst.GetCurWindowId()}");
        UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
        {
            CanClose = false,
            ConfirmString = "确定",
            ContextString = msg,
            TopTitleString = "提示",
            ConfirmClickAction = () => {
                if (UIManager.Inst.GetCurWindowId() != (int)PanelId.SignInPanel)
                {
                    DeviceInfoManager.Inst.RemoveOldUserInfo();
                    GameInstanceManager.Release();
                    AccountDataManager.Inst.DeleteCache();
                    UIManager.Inst.ClosePanel(PanelId.GameHallPanel);
                    UIManager.Inst.OpenPanel(PanelId.SignInPanel);
                    MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout, "");
                }
            },
        });
    }

}
