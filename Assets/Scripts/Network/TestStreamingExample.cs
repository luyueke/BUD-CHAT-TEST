using System.Collections;
using System.IO;
using System.Text;
using BestHTTP;
using BestHTTP.WebSocket;
using UnityEngine;
using UnityEngine.Networking;
/// <summary>
/// S7可以去掉该脚本
/// </summary>
public class TestStreamingExample : MonoBehaviour
{
    private string url = "http://120.53.104.4:30005/aibuddy/chat/stream"; // 示例 URL
    // 请求完成后的回调函数
    private void OnRequestFinished(HTTPRequest req, HTTPResponse res)
    {
        if (res.IsSuccess)
        {
            Debug.Log("Response received: " + res.DataAsText);
            // 处理流数据
            // 你可以在这里根据需要分段解析流式数据
        }
        else
        {
            Debug.LogError("Request failed: " + res.StatusCode + " - " + res.Message);
        }
    }

    
    private BestHTTP.ServerSentEvents.EventSource eventSource;

    void Start()
    {
        // 建立 SSE 连接
        string sseUrl = "http://120.53.104.4:30005/aibuddy/chat/stream";
        eventSource = new BestHTTP.ServerSentEvents.EventSource(new System.Uri(sseUrl));
        eventSource.InternalRequest.MethodType = HTTPMethods.Post;
        eventSource.InternalRequest.MaxRetries = 0;
        // 创建 JSON 数据
        string jsonData = "{\"param1\":\"value1\", \"param2\":\"value2\"}";
        eventSource.InternalRequest.RawData =  Encoding.UTF8.GetBytes(jsonData);

        eventSource.OnOpen += OnOpen;
        eventSource.OnMessage += OnMessage;
        eventSource.OnClosed += OnClose;
        eventSource.Open();
    }
    

    void OnOpen(BestHTTP.ServerSentEvents.EventSource eventSource)
    {
        Debug.Log("SSE connection opened.");
    }

    void OnMessage(BestHTTP.ServerSentEvents.EventSource eventSource, BestHTTP.ServerSentEvents.Message message)
    {
        Debug.Log($"Received SSE message: {message.Data}");
    }

    void OnClose(BestHTTP.ServerSentEvents.EventSource eventSource)
    {
        Debug.LogError($"SSE Close");
    }

    void OnDestroy()
    {
        eventSource.Close();
    }
    
    // Start is called before the first frame update
  
}