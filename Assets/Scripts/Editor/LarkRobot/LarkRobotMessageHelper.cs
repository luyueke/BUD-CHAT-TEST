using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 飞书机器人工具
/// Shaocheng
/// 2023-8-29 13:49:48
/// </summary>
public class LarkRobotMessageHelper
{
    #region 消息结构 文档：https: //open.feishu.cn/document/client-docs/bot-v3/add-custom-bot#756b882f

    public class LarkMsgContent
    {
        public string text;
    }

    public class LarkMsg
    {
        public string msg_type = "text";
        public LarkMsgContent content;
    }

    #endregion

    //test
    // [MenuItem("BudTools/TestSendTextMessage")]
    // public static void TestSendTextMessage()
    // {
    //     SendTextMessage("https://open.feishu.cn/open-apis/bot/v2/hook/9676c89c-6499-46b5-a15c-789bb9f3fcab", "国服热更包构建完成，构建环境:Master, 请重启游戏查看");
    // }

    public static void SendTextMessage(string robotUrl, string textContent)
    {
        var newMsg = CreateNewMessage(content: textContent);
        SendLarkPost(robotUrl, newMsg);
    }

    public static string CreateNewMessage(string msgType = "text", string content = "")
    {
        var newMsg = new LarkMsg
        {
            msg_type = msgType,
            content = new LarkMsgContent()
            {
                text = content
            }
        };

        var result = JsonConvert.SerializeObject(newMsg);
        return result;
    }

    #region Post请求

    private static void SendLarkPost(string url, string jsonContent)
    {
        Debug.Log($"SendLarkPost url:{url}, json:{jsonContent}");
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
        request.Method = "PUT";
        request.ContentType = "application/json";
 
        // Fill body.
        byte[] contentBytes = new UTF8Encoding().GetBytes(jsonContent);
        request.ContentLength = contentBytes.LongLength;
        request.GetRequestStream().Write(contentBytes, 0, contentBytes.Length);
 
        try
        {
            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Debug.Log("SendLarkPost Publish Response: " + (int)response.StatusCode + ", " + response.StatusDescription);
                if((int)response.StatusCode == 200)
                {
                    Debug.Log("SendLarkPost 飞书消息推送成功");
                }
            }
        }
        catch(Exception e)
        {
            Debug.LogError("SendLarkPost : " + e.ToString());
        }
    }

    #endregion
}