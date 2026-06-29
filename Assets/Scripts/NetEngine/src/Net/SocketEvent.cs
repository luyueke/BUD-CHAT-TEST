using System;

namespace NetEngine.src.Net
{
    public class SocketEvent
    {
        public Action callback;

        public SocketEvent()
        {
        }

        public SocketEvent(string msg)
        {
            this.Msg = msg;
        }

        public string Tag { get; set; }

        public string Msg { get; set; }

        public byte[] Data { get; set; }
    }
}
