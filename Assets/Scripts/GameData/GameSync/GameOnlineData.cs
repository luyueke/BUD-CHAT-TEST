using Pb.Base;

namespace GameData.GameSync
{
    public class GameOnlineData
    {
        public RoomType roomType = RoomType.Normal;
        public RoomSubType roomSubType = RoomSubType.Public;
        public string roomCode = "";


        public void Clear()
        {
            roomType = RoomType.Normal;
            roomSubType = RoomSubType.Public;
            roomCode = "";
        }

        public void CreatePrivateData()
        {
            Clear();
            roomSubType = RoomSubType.Private;
        }

        public void CreateJoinCodeData(GetServerInfoByCodeResp resp)
        {
            Clear();
            roomCode = resp.roomCode;
            roomType = resp.roomType;
            roomSubType = resp.roomSubType;
        }

        public override string ToString()
        {
            return $"roomCode:{roomCode}, roomType:{roomType}, roomSubType:{roomSubType}";
        }
    }
}
