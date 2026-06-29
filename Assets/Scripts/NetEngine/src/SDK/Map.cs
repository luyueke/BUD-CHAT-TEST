/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-14 10:16:19
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-18 19:00:22
 * @ Description: 客户端的地图管理
 */

using System.Linq;
using NetEngine.src.SDK;
using NetEngine.src.Broadcast;
using Pb.Base;
using NetEngine.src.Util;

namespace NetEngine
{
    /********************************* SDK Map对象 *********************************/
    public class Map : MapBroadcastHandler
    {
        public MapBroadcast MapBroadcast { get; private set; }

        public MapInfoRsp ClientData { get; private set; } // 同步服务器的当前进入地图的信息

        /// <summary>
        /// 在第一次GetGameServer成功之后，客户端的房间就创建了
        /// </summary>
        public Map(string mapId) : base()
        {
            ClientData = new MapInfoRsp();
            ClientData.MapId = mapId;
        }

        public void InitMap(MapInfoRsp mapInfoRsp)
        {
            if (mapInfoRsp != null && ClientData!=null && mapInfoRsp.MapId != ClientData.MapId)
            {
                // 【异常】 当前已经存在一个不相同的地图。
                Debugger.LogError($"[Error]InitRoom Map[MapId={mapInfoRsp.MapId}]. Curren Map[MapId={ClientData.MapId}] Exist.");
                return;
            }
            ClientData = new MapInfoRsp(mapInfoRsp);
            MapBroadcast = new MapBroadcast(this);
        }
        
        /// <summary>
        /// 玩家是否在当前地图
        /// </summary>
        /// <param name="mapId">玩家所处地图</param>
        /// <param name="playerId">玩家Id</param>
        public bool IsPlayerInMap(string playerId)
        {
            return ClientData != null && ClientData.Players.Any(p=>p.PlayerInfo.Uid == playerId);
        }

        /// <summary>
        /// 是否当前地图
        /// </summary>
        public bool IsInMap(string mapId)
        {
            return ClientData != null && ClientData.MapId == mapId;
        }

        public void AddPlayer(PlayerEnterBst enterBst)
        {
            ClientData.Players.Add(enterBst.PlayerStatus);
        }

        public void RemovePlayer(string playerId)
        {
            var player = GetPlayer(playerId);
            if(player != null)
                ClientData.Players.Remove(player);
        }

        public PlayerStatus GetPlayer(string playerId)
        {
            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                if (ClientData.Players[i].PlayerInfo.Uid == playerId)
                {
                    return ClientData.Players[i];
                }
            }

            return null;
        }
    }
}