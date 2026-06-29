using Message;
using Network;
using Network.Http;
using Network.Message;
using Network.Tcp.Core;
using Newtonsoft.Json;
using System.Text;
using UIAgent;

public class TcpLoginManager
{
    public static TcpLoginManager Instance = new TcpLoginManager();

    private const string _master_ip = "tcp-master.budapp.cn";
    private static string _alpha_ip = "tcp-alpha.budapp.cn";
    private static string _prod_ip = "tcp.budapp.cn";
    private static string _prod_ip_us = "tcp.joinbudapp.com";
    private const int _port = 8999;
    private string _curTcpIp = "tcp.budapp.cn";
    private string _curUid = "";
    private MsgListener _onLoginRsp;
    private MsgListener _onChatRsp;
    private MsgListener _onBusinessLiveUpdate;
    private MsgListener _onUpdateTaskRsp;
    private MsgListener _onMutePopUpRsp;
    private MsgListener _onAccountSuspension;
    private MsgListener _onTimeRsp;
    public void Init()
    {
        _onLoginRsp = new MsgListener((bytes) =>
        {
            HandleLoginRsp();
            MessageHelper.Broadcast(MessageName.TcpLoginSuccess);
        });
        _onChatRsp = new MsgListener((bytes) =>
        {
            ChatRsp chatRsp = ChatRsp.Parser.ParseFrom(bytes);
            TcpChatData tcpChatData = new TcpChatData()
            {
                data = chatRsp.Data,
                timeStamp = chatRsp.Timestamp,
                toUid = chatRsp.Uid,
                type = (int)chatRsp.Type,
                totalUnReadCount = chatRsp.TotalUnReadCount
            };
            MessageHelper.Broadcast(MessageName.ChatMessage, tcpChatData);
        });

        _onBusinessLiveUpdate = new MsgListener((bytes) =>
        {
            MessageHelper.Broadcast(MessageName.BusinessLiveConfigUpdate);
        });

        _onUpdateTaskRsp = new MsgListener((bytes) =>
        {
            MessageHelper.Broadcast(MessageName.UpdateHallTask);
        });

        _onMutePopUpRsp = new MsgListener((bytes) =>
        {
            ChatBroadcastRsp rsp = ChatBroadcastRsp.Parser.ParseFrom(bytes);
            string msg = rsp.Text;// Encoding.UTF8.GetString(bytes);
            LoggerUtils.Log($"收到消息 ID ={NetMsg.MSG_MUTE_POPUP},Msg={msg}");
            MessageHelper.Broadcast(MessageName.ChatMutePopup, msg);

        });
        _onAccountSuspension = new MsgListener((bytes) =>
        {
            ChatBroadcastRsp rsp = ChatBroadcastRsp.Parser.ParseFrom(bytes);
            string msg = rsp.Text;//Encoding.UTF8.GetString(bytes);
            LoggerUtils.Log($"收到消息 ID ={NetMsg.MSG_ACCOUNT_SUSPENSION},Msg={msg}");
            MessageHelper.Broadcast(MessageName.AccountSuspension, msg);
        });
        _onTimeRsp = new MsgListener((bytes) =>
        {
            TimestampRsp rsp = TimestampRsp.Parser.ParseFrom(bytes);
            long msg = rsp.Time;//Encoding.UTF8.GetString(bytes);
            LoggerUtils.Log($"收到消息 ID ={NetMsg.MSG_TIMESTAMP},Msg={msg}");
            TcpTimeSystem.Inst.RspTime(msg);
            //MessageHelper.Broadcast(MessageName.AccountSuspension, msg);
        });

        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_LOGIN, _onLoginRsp);
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_CHAT, _onChatRsp);
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_UPDATE_TASK, _onUpdateTaskRsp);
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_CONFIGURATION_UPDATE, _onBusinessLiveUpdate);
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_MUTE_POPUP, _onMutePopUpRsp);
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_ACCOUNT_SUSPENSION, _onAccountSuspension);
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_TIMESTAMP, _onTimeRsp);
        NetworkManager.Inst.AddNetStatusEventListener(OnConnectChanged);
    }

    public void Destroy()
    {
        NetworkManager.Inst.RemoveNetStatusEventListener(OnConnectChanged);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_LOGIN, _onLoginRsp);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_CHAT, _onChatRsp);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_UPDATE_TASK, _onUpdateTaskRsp);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_CONFIGURATION_UPDATE, _onBusinessLiveUpdate);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_MUTE_POPUP, _onMutePopUpRsp);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_ACCOUNT_SUSPENSION, _onAccountSuspension);
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_TIMESTAMP, _onTimeRsp);

        _onChatRsp = null;
        _onLoginRsp = null;
        _onBusinessLiveUpdate = null;
        _onUpdateTaskRsp = null;
    }

    public void Login(string uid)
    {
        this._curUid = uid;
        var environment = DeviceInfoManager.Inst.Environment;
        switch (environment)
        {
            case GameEnvironment.MASTER:
                _curTcpIp = _master_ip;
                break;
            case GameEnvironment.ALPHA:
                _curTcpIp = _alpha_ip;
                break;
            case GameEnvironment.PROD:
                _curTcpIp = _prod_ip;
                break;
            default:
                _curTcpIp = _prod_ip;
                break;
        }
#if PACKAGE_TYPE_US
        _curTcpIp = _prod_ip_us;
#endif
        ConnectServer(_curTcpIp, _port);
    }

    public void Reconnect()
    {
        Login(this._curUid);
    }
    
    private void ConnectServer(string ip, int port)
    {
        if (NetworkManager.Inst.IsDisconnect() == false)
            NetworkManager.Inst.OnConnectLost();

        // 连接服务器
        LoggerUtils.Log("Network: start connect ip:"+ip + " port:"+port);
        NetworkManager.Inst.ConnectServer(ip, port);
    }

    private void OnConnectChanged(NetworkStatus status)
    {
        // 连接成功
        if (status == NetworkStatus.Connect)
        {
            LoggerUtils.Log("Network: connect success");
            SendLoginReq();
        }
        // 连接断开
        else if (status == NetworkStatus.Disconnect)
        {
            NetworkManager.Inst.Reconnect();
        }
    }

    // 发送登录请求
    private void SendLoginReq()
    {
        LoggerUtils.Log("###Network 发送登录1");
        var req = new LoginReq();
        req.Metadata.Add("uid", _curUid);
        req.Metadata.Add("version", DeviceInfoManager.Inst.DeviceBaseData.version);
        req.Metadata.Add("platform", DeviceInfoManager.Inst.DeviceBaseData.platform);
        req.Metadata.Add("environment", DeviceInfoManager.Inst.DeviceBaseData.environment);
        req.Metadata.Add("mobile", DeviceInfoManager.Inst.DeviceBaseData.mobile);
        req.Metadata.Add("requestId",  "");
        req.Metadata.Add("ip", _curTcpIp);
        req.Metadata.Add("hotUpdateVersion", NetworkManager.Inst.HotUpdateVersion);
        req.Metadata.Add(HeaderDefine.feature, HeaderDefine.CUR_FEATURE);
#if PACKAGE_TYPE_US
        //TODO:@Jaywill 暂时写死语言码为en
        req.Metadata.Add("lang",LangCode.en.ToString());
#endif
        LoggerUtils.Log("###Network 发送登录2:"+JsonConvert.SerializeObject(req.Metadata));
        NetworkManager.Inst.SendMessage(NetCmd.CMD_LOGIN, req);

        LoggerUtils.Log($"Network: send login req(metadata = {req.Metadata}).");
    }

    // 处理登录消息
    private void HandleLoginRsp()
    {
        LoggerUtils.Log("Network: recv login rsp.");
        NetworkManager.Inst.SendPing();
        NetworkManager.Inst.EnableKeepAlive(true);
        TcpTimeSystem.Inst.ReqTime();
    }
}