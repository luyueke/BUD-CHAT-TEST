using System;
using Pb.Base;


namespace NetEngine.src.SDK
{
    public abstract class MapBroadcastHandler
    {
        public Action<PlayerEnterBst> OnPlayerEnter { get; set; }
        public Action<PlayerLeaveBst> OnPlayerLeave { get; set; }
        public Action<CommonSyncBst> CommonSyncBstListener { get; set; }
    }
}