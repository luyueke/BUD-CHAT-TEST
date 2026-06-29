using Pb.Base;
using NetEngine.src.Util;

namespace NetEngine.src.Broadcast
{
    public class RoomBroadcast
    {
        private readonly NetEngine.Room _room;
        public FrameBroadcast FrameBroadcast { get; }
        public int FrameBroadcastFrameId { get; } = 0;

        public RoomBroadcast(NetEngine.Room room)
        {
            this._room = room;
            //TODO:帧率先固定为66
            // var frameRate = this._room.RoomInfo.FrameRate != 0 ? 1000 / this._room.RoomInfo.FrameRate : 66;
            var frameRate = NetConfig.FrameRate;

            void Callback(BroadcastEvent eve)
            {
                var bst = (RecvFrameBst)eve?.Data;
                _room.OnBstFrameData?.Invoke(bst);
            }

            this.FrameBroadcast = new FrameBroadcast(frameRate, Callback);
        }

        /**
         * 本地网络状态变化
         */
        public void OnNetwork(string tag, ResponseEvent eve)
        {
            _room.OnUpdate(this._room,tag, eve);
        }

        /**
         * 玩家加入某个地图的广播
         */
        public void OnPlayerEnter(BroadcastEvent eve)
        {
            var enterBst = (PlayerEnterBst)eve.Data;
            _room.OnPlayerEnter?.Invoke(enterBst);

            Global.Map?.MapBroadcast?.OnPlayerEnter(enterBst);
        }

        /**
         * 玩家退出某个地图的广播
         */
        public void OnPlayerLeave(BroadcastEvent eve)
        {
            var leaveBst = (PlayerLeaveBst)eve.Data;
            _room.OnPlayerLeave?.Invoke(leaveBst);
            
            Global.Map?.MapBroadcast?.OnPlayerLeave(leaveBst);
        }

        /// <summary>
        /// 房间通用广播
        /// </summary>
        /// <param name="eve"></param>
        public void OnCommonSyncBst(BroadcastEvent eve)
        {
            var syncBst = (CommonSyncBst)eve.Data;
            _room.CommonSyncBstListener?.Invoke(syncBst);
            Global.Map?.MapBroadcast?.OnCommonSyncBst(syncBst);
        }

        /**
         * 帧数据广播
         */
        public void OnBstFrameData(BroadcastEvent eve)
        {
            this.FrameBroadcast.Push(eve, _room);

            var bst = (RecvFrameBst)eve?.Data;
            _room?.OnBstEveryFrameData(bst);
        }

       
        public void Error(BroadcastEvent eve)
        {
            // this.room.
        }
    }
}