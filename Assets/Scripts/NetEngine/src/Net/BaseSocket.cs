using System;
using Pb.Base;

namespace NetEngine.src.Net
{
    public abstract class BaseSocket
    {
        public SocketState ReadyState { get; set; } = SocketState.Closed;

        public abstract void Connect();
        public abstract void Send(byte[] data, Action<ErrorCode> fail, Action success);

        public Action onClose;
        public Action onOpen;
        public Action<SocketEvent> onMessage;
        public Action<SocketEvent> onError;

        public virtual void Close(Action success, Action fail)
        {
        }
    }
}
