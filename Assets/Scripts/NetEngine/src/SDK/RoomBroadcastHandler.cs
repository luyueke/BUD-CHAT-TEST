using System;
using Pb.Base;


namespace NetEngine.src.SDK
{
    public abstract class RoomBroadcastHandler
    {
        public Action<PlayerEnterBst> OnPlayerEnter { get; set; }
        public Action<PlayerLeaveBst> OnPlayerLeave { get; set; }
        public Action<RecvFrameBst> OnBstFrameData { get; set; } // 每固定帧率回调
        public Action<RecvFrameBst> OnBstEveryFrameData { get; set; } // 每帧回调
        public Action<CommonSyncBst> CommonSyncBstListener { get; set; }

    }
}