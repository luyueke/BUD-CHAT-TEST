using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Game;
using UnityEngine;

namespace AIGame.Base
{
    public class AIParkNpcLocationSyncUtil : GlobalInstance<AIParkNpcLocationSyncUtil>
    {
        Transform selfTrans;
        private int layMask;
        Dictionary<string, LocationType> groundDic = new Dictionary<string, LocationType>();

        public LocationType curLocationType = LocationType.None;

        Dictionary<string, LocationType> npcId2LocationDic = new();
        BudTimer _updateTimer;
        public bool beginCheckPos = false;
        public void Init()
        {
            selfTrans = AvatarController.Inst.SelfController.transform; ;
            AvatarRaycast avtRaycast = AvatarController.Inst.SelfAvatarRaycast;
            // avtRaycast.OnRaycast.AddListener(OnAvatarRaycast);

            layMask = 1 << LayerMask.NameToLayer("Model") |
                    1 << LayerMask.NameToLayer("GameSurface") |
                    1 << LayerMask.NameToLayer("Prop");

            groundDic.Clear();
            groundDic.Add("ground_park", LocationType.Park);
            groundDic.Add("ground_stage", LocationType.Stage);
            groundDic.Add("ground_penquan", LocationType.Fountain);
            groundDic.Add("ground_muma1", LocationType.TrojanHorse);
            groundDic.Add("ground_muma2", LocationType.TrojanHorse);

            npcId2LocationDic.Clear();
            var npcDic = AIPark_CharacterManager.Inst.GetNpcDic();
            foreach (var npc in npcDic)
            {
                npcId2LocationDic.Add(npc.Value.GetNpcID(), LocationType.None);
            }

            _updateTimer = TimerManager.Inst.Run("AIParkNpcLocationSyncUtil_Update", 0, 0.2f, Update);
        }

        public void Release()
        {
            if (_updateTimer != null)
            {
                TimerManager.Inst.Stop(_updateTimer);
                _updateTimer = null;
            }
        }

        public void Update()
        {
            if (!beginCheckPos)
            {
                return;
            }
            var hits = Physics.RaycastAll(selfTrans.position, -selfTrans.up, 1, layMask);
            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (groundDic.ContainsKey(hit.collider.name) && curLocationType != groundDic[hit.collider.name])
                    {
                        curLocationType = groundDic[hit.collider.name];
                        SyncNpcLocation("0", (int)curLocationType, (int)AIParkPropsManager.Inst.currentMyActionType);
                        break;
                    }
                }
            }
        }
        public void SyncNpcLocation(string npcId, int location, int action)
        {
            if (npcId == "0")
            {
                MessageHelper.Broadcast(MessageName.OnSyncNpcLocation, npcId, location, action);
                return;
            }
            var locationType = npcId2LocationDic[npcId];
            if ((int)locationType != location)
            {
                npcId2LocationDic[npcId] = (LocationType)location;
                MessageHelper.Broadcast(MessageName.OnSyncNpcLocation, npcId, location, action);
            }
        }



    }
}