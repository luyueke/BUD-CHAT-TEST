using System.Collections.Generic;
using Game.Avatar;
using NetEngine;
using Pb.Base;

namespace GameSync.Manager
{
    public class GamePlayerInfoManager
    {
        private const string TAG = "GamePlayerInfoManager";
        public List<PlayerInfo> PlayerInfos = new List<PlayerInfo>();

        public List<string> GetPlayerInfoIds()
        {
            List<string> playerinfoIds = new List<string>();
            var players = Global.Map?.ClientData?.Players;
            if (players == null)
            {
                playerinfoIds.Add(Player.Id);
                return playerinfoIds;
            }
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].PlayerInfo != null)
                {
                    playerinfoIds.Add(players[i].PlayerInfo.Uid);
                }
            }
            return playerinfoIds;
        }


        public void Clear()
        {
            PlayerInfos.Clear();
        }

        public void InitPlayerInfos(List<PlayerInfo> playerInfos)
        {
            LoggerUtils.Log(@$"{TAG} InitPlayerInfos: playerInfos.Count:{playerInfos.Count}");
            PlayerInfos = playerInfos;
        }

        public void AddPlayerInfo(PlayerInfo pInfo)
        {
            if (ContainsPlayer(pInfo.Uid))
            {
                RemovePlayerInfo(pInfo.Uid);
            }
            PlayerInfos.Add(pInfo);
        }

        public void RemovePlayerInfo(string uid)
        {
            var pInfo = GetPlayerInfoById(uid);
            if (pInfo != null)
            {
                PlayerInfos.Remove(pInfo);
            }
        }

        public bool ContainsPlayer(string uid)
        {
            return PlayerInfos.Exists((x)=>x.Uid == uid);
        }

        public PlayerInfo GetPlayerInfoById(string uid)
        {
            foreach(var info in PlayerInfos)
            {
                if(info.Uid == uid)
                {
                    return info;
                }
            }
            return null;
        }
        public List<FeatureItem> GetTitleData(string uid)
        {   
            var playerInfo = GetPlayerInfoById(uid);
            if(playerInfo == null || playerInfo.AvatarJson == null)
            {
                LoggerUtils.LogError("无法找到玩家信息!");
                return null;
            }
            var avatarData = CharacterData.DeserializeObject(playerInfo.AvatarJson);

            if (avatarData == null || avatarData.featureItems == null)
            {
                LoggerUtils.LogError("CharacterData解包失败！");
                return null;
            }

            return avatarData.featureItems;

            
        }
    }
}
