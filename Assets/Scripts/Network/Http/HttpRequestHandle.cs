

using System;
using Newtonsoft.Json;


/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-26 18:50:56
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-26 18:53:01
 * @ Description: 对Http请求的一个封装
 */
namespace Network.Http
{
    public enum HttpSenderStatus
    {
        Sending,
        Cancel,
    }

    public class HttpSender<T>
    {
        public HttpSenderStatus Status;
        public Action<T> OnSuccess;
        public Action<HttpResponseFailDataStruct> OnFail;

        public void Cancel()
        {
            OnSuccess = null;
            OnFail = null;
            Status = HttpSenderStatus.Cancel;
        }
    }

    public class HttpRequestHandle<T>
    {
        protected string m_Url;
        protected HttpMethod m_ReqMethod;

        public HttpRequestHandle(string url, HttpMethod reqMethod = HttpMethod.GET)
        {
            this.m_Url = url;
            this.m_ReqMethod = reqMethod;
        }

        public HttpSender<T> Send(object param)
        {
            HttpSender<T> sender = new HttpSender<T>();
            sender.Status = HttpSenderStatus.Sending;
            NetworkManager.Inst.SendHttpRequest(m_Url,m_ReqMethod,JsonConvert.SerializeObject(param),
            onReceive: content=>{
                if (sender.Status != HttpSenderStatus.Cancel)
                {
                    T retObj = JsonConvert.DeserializeObject<T>(content);
                    sender.OnSuccess?.Invoke(retObj);
                }
            }, onFail: error =>{
                if (sender.Status != HttpSenderStatus.Cancel)
                {
                    HttpResponseFailDataStruct errorObj = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(error);
                    sender.OnFail?.Invoke(errorObj);
                }
            });
            return sender;
        }
    }
}