using Message;
using Network;
using Network.Tcp.Core;
using UIAgent;
using UnityEngine;


public class TcpFriendListMgr : MonoBehaviour
{
    public static TcpFriendListMgr Instance = new TcpFriendListMgr();
    private MsgListener _onReFriendOnlineRsp;
    
    public void Init()
    {
        _onReFriendOnlineRsp = new MsgListener((bytes) =>
        {
            HandleFriendOnlineRsp();
        });

        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_SYNC_USER_FRIEND_REFRESH, _onReFriendOnlineRsp);
    }

    public void Destroy()
    {
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_SYNC_USER_FRIEND_REFRESH, _onReFriendOnlineRsp);
        _onReFriendOnlineRsp = null;
    }
    
    private void HandleFriendOnlineRsp()
    {
        LoggerUtils.Log("Network: recv HandleFriendOnlineRsp");
        MessageHelper.Broadcast(MessageName.OnTcpNotifyRefreshFriendList);
    }
}
