using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    public class TalkStateUtils : GlobalInstance<TalkStateUtils>
    {
        #region 交谈位置配置
        private Dictionary<LocationType, List<HospitalNpcTransData>> _talkTransDict =
            new Dictionary<LocationType, List<HospitalNpcTransData>>()
            {
                [LocationType.ConsultingRoom] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-15.955f,1.41f,-23.78f),
                        rot =  new Vector3(0,145f,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-15.29f,1.41f,-24.73f),
                        rot =  new Vector3(0,-34.5f,0),
                    },
                },
                
                [LocationType.DirectorsOffice] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-14.744f,1.41f,-15.637f),
                        rot =  new Vector3(0,180,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-14.79f,1.41f,-16.95f),
                        rot =  new Vector3(0, 0,0),
                    },
                },
                
                [LocationType.WaitingRoom] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-9.28f,1.41f,-25.47f),
                        rot =  new Vector3(0,0,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-9.23f,1.41f,-24.07f),
                        rot =  new Vector3(0,180f,0),
                    },
                },
                
                [LocationType.Toilet] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-6.05f,1.41f,18.73f),
                        rot =  new Vector3(0,180,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-6.21f,1.41f,16.3f),
                        rot =  new Vector3(0,0,0),
                    },
                },
                
                [LocationType.Ward] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(3.80f,1.41f,-20.67f),
                        rot =  new Vector3(0,234,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(2.765f,1.41f,-21.42f),
                        rot =  new Vector3(0,54,0),
                    },
                },
                
                [LocationType.Corridor] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-4.746f,1.41f,-12.674f),
                        rot =  new Vector3(0,0,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-4.611f,1.41f,-11.16f),
                        rot =  new Vector3(0,180,0),
                    },
                },
                
                [LocationType.Pharmacy] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-14.40f,1.41f,-0.11f),
                        rot =  new Vector3(0,42.534f,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-13.02f,1.41f,1.38f),
                        rot =  new Vector3(0,222.5f,0),
                    },
                },
                
                [LocationType.OutDoor] = new List<HospitalNpcTransData>()
                {
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-2.528f,1.41f,4.154f),
                        rot =  new Vector3(0,180,0),
                    },
                    new HospitalNpcTransData()
                    {
                        pos = new Vector3(-2.598f,1.41f,2.51f),
                        rot =  new Vector3(0,0,0),
                    },
                },
            };
        #endregion
        
        private Dictionary<LocationType, List<string>> _curTalkPlayers = new Dictionary<LocationType, List<string>>();
        
        public void InitData()
        {
            _curTalkPlayers.Clear();
        }

        public HospitalNpcTransData OnPlayerEnterTalkState(LocationType locationType, string playerId)
        {
            HospitalNpcTransData talkTansData = new HospitalNpcTransData();
            
            //说明有玩家正在前往 或者 到达交谈地点
            if(_curTalkPlayers.ContainsKey(locationType))
            {
                var playerList = _curTalkPlayers[locationType];
                playerList.Add(playerId);
                _curTalkPlayers[locationType] = playerList;
                
                talkTansData = _talkTransDict[locationType][1];
            }
            else
            {
                _curTalkPlayers.Add(locationType, new List<string>(){playerId});
                talkTansData = _talkTransDict[locationType][0];
            }

            return talkTansData;
        }

        public void OnPlayerExitTalkState(LocationType locationType, string playerId)
        {
            if (_curTalkPlayers.ContainsKey(locationType))
            {
                _curTalkPlayers.Remove(locationType);
            }
        }
    }
}
