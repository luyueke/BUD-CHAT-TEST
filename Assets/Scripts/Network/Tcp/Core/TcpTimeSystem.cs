using Message;
using NetCoreServer;
using Network;
using Network.Message;
using Network.Tcp.Core;
using System;
using System.Collections;
using UnityEngine;

public class TcpTimeSystem : GlobalInstance<TcpTimeSystem>
{
    private long serverTime; //服务器时间戳

    private float reqTime;  //客户端请求时

    private float rspTime;  //客户端返回时

    public long ServerTime 
    {
        get 
        {
            var interval = (Time.realtimeSinceStartup - reqTime) - (rspTime - reqTime) / 2;
            return serverTime + (int)interval;
        }
    }

    public DateTime ServerDataTime {
        get 
        {
            return TimeTools.SecondsToDateTime(ServerTime);
        }
    }

    public void ReqTime() {
        reqTime = Time.realtimeSinceStartup;
        LoggerUtils.Log("###Network 发送时间戳");
        var req = new TimestampReq();

        NetworkManager.Inst.SendMessage(NetCmd.CMD_TIMESTAMP, req);
    }

    public void RspTime(long server)
    {
        rspTime = Time.realtimeSinceStartup;
        LoggerUtils.Log("###Network 收到时间戳 " + server);

        serverTime = server;

     // Debug.LogError(ServerDataTime.ToString());

        MessageHelper.Broadcast(MessageName.TcpTimeUpdate);
    }

    public bool IsInit() {
        return rspTime > 0;
    }
}