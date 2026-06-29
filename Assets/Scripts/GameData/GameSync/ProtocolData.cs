using Google.Protobuf;
using System.Collections.Generic;
using Pb.Base;

namespace GameData.GameSync
{
    public class GetGameServerReq
    {
        public string mapId = "";
        public string roomCode = "";
        public int roomType;
        public int roomSubType;
        public int maxPlayers = 16;
    }

    public class GameServerInfo
    {
        public string ip = "";
        public int roomPort = 0;
        public int framePort = 0;
        public string roomCode = "";
        public string messages = "";
        public int spawnId = 0;
    }
    
    public class GetServerInfoByCodeReq
    {
        public string roomCode = "";
    }

    public class GetServerInfoByCodeResp
    {
        public string roomCode = "";
        public string mapId = "";
        public RoomType roomType;
        public RoomSubType roomSubType;
        public int maxPlayers;
    }

    public class GetServerPlayersReq
    {
        public string roomCode = "";
    }

    public class GetServerPlayersRsp
    {
        public List<PlayerInfo> players;
    }

	public class CommonSyncClientData
    {
        public string PalyerId;
        public string MapId;
        public SyncArea SyncArea;
        public IMessage Body;


    }
}
