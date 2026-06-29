namespace Network.Http
{
    public class RequestHeader
    {
        
    }
    
    public class RenderHeader : RequestHeader
    {
        public string platform;
    }

      public class VersionHeader : RequestHeader
    {
        public string version;
    }
    
    
    /// <summary>
    /// 支付相关的Http请求 会用到一个签名头部
    /// </summary>
    public class SignHeader : RequestHeader
    {
        public string appId;
        public string sign;
        public string timestamp;
    }

    public class NpcReadVoiceHeader : RequestHeader
    {
        public string accept;
    }
    
    public class HttpPageBaseData
    {
        public string cookie;
        public int IsEnd;
    }
}