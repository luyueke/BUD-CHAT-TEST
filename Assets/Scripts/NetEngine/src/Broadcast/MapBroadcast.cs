using Game.Utils;
using GameData.GameSync;
using Pb.Base;
using NetEngine.src.Util;

namespace NetEngine.src.Broadcast
{
    public class MapBroadcast
    {
        private readonly NetEngine.Map _map;

        public MapBroadcast(NetEngine.Map map)
        {
            this._map = map;
        }

        /**
         * 玩家加入某个地图的广播
         */
        public void OnPlayerEnter(PlayerEnterBst enterBst)
        {
            if (!this._map.IsInMap(enterBst.MapId))
                return;
            var playerId = enterBst.PlayerStatus.PlayerInfo.Uid;
            if (this._map.IsPlayerInMap(playerId))
            {
                Debugger.LogError($"OnPlayerEnter Player[PlayerId={playerId}] Is In Current Map.");
                return;
            }

            _map.AddPlayer(enterBst);
            _map.OnPlayerEnter?.Invoke(enterBst);
        }

        /**
         * 玩家退出某个地图的广播
         */
        public void OnPlayerLeave(PlayerLeaveBst leaveBst)
        {
            if (!this._map.IsInMap(leaveBst.MapId))
                return;
            var playerId = leaveBst.PlayerId;
            if (!this._map.IsPlayerInMap(playerId))
            {
                Debugger.LogError($"OnPlayerLeave Player[PlayerId={playerId}] Is Not In Current Map.");
                return;
            }

            _map.RemovePlayer(leaveBst.PlayerId);
            _map.OnPlayerLeave?.Invoke(leaveBst);
        }


        public void OnCommonSyncBst(CommonSyncBst eve)
        {
            if (!this._map.IsInMap(eve.MapId))
                return;
            _map.CommonSyncBstListener?.Invoke(eve);
        }
    }
}