using System;

namespace Network.Http
{
    [Serializable]
    public struct HttpRequestData
    {
        public string path;
        public string paramStr;
    }

    [Serializable]
    public class HttpResponseRawData
    {
        public int result;
        public string rmsg;
        public object data;
    }
    
    [Serializable]
    public class HttpResponseData<T>
    {
        public int result;
        public string rmsg;
        public T data;
    }
    
    public class HttpResponseFailDataStruct
    {
        public int result = 0;
        public string rmsg = "";
    }


}