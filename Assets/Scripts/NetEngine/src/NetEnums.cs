
namespace NetEngine.src
{
    public enum SocketState
    {
        Connecting = 0,
        Open = 1,
        Closing = 2,
        Closed = 3,
    }
    
    public enum ProtoErrCode {
        ///系统框架错误
        EC_OK						= 0,  /// 返回成功

        ///90000～99999 预留给客户端
        //////客户端错误
        EcSdkSendFail			= 90001, /// 消息发送失败
        EcSdkUninit				= 90002, /// SDK 未初始化
        EcSdkResTimeout		= 90003, /// 消息响应超时
        EcSdkNoLogin			= 90004, /// 登录态错误
        EcSdkNoCheckLogin		= 90005, /// 帧同步鉴权错误
    }
}