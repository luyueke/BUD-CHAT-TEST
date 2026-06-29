using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Props.PropsComponents;
using Message;
using Pb.Base;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereSofaBehaviour: AIPropBaseBehaviour
    {
        private int tag;//1:沙发 2：马桶 3:床
        private string[] emoteIds = {"40200402","40200405","40200406"};
        private Vector3 pos;
        private Vector3 rot;
        private int index;
        private string curEmoteId;
        private Action<YandereAreaType,int> _sendPosAct;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIYandereCommonComponent>();
            pos = dataComp.Pos;
            rot = dataComp.Rot;
            tag = dataComp.Tag;
            index = dataComp.Index;
            MessageHelper.AddListener<string>(MessageName.AICancelEmote, CancelEmote);
        }


        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.AICancelEmote, CancelEmote);
        }

        protected Func<bool, bool> _checkStartGame;


        private void CancelEmote(string emoId)
        {
            IsCanClick = true;
        }
        

        public void SetSendPosAct(Action<YandereAreaType,int> act)
        {
            this._sendPosAct = act;
        }
        
        public void SetFunctions(Func<bool, bool> func)
        {
            this._checkStartGame = func;
        }
        
        public override void OnTouchClick()
        {
            if (IsCanClick && _checkStartGame(false))
            {
                curEmoteId = emoteIds[tag - 1];
                YandereAreaType areaType = YandereAreaType.Sofa;
                switch (tag)
                {
                    case 1:
                        areaType = YandereAreaType.Sofa;
                        break;
                    case 2:
                        areaType = YandereAreaType.Closestool;
                        break;
                    case 3:
                        areaType = YandereAreaType.Bed;
                        break;
                }
                
                var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == curEmoteId);
                if (emoAniDataList.Count > 0)
                {
                    if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
                    {
                        _sendPosAct?.Invoke(areaType,index);
                        int random = emoAniDataList[0].randomCount;
                        int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                        AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(pos,Quaternion.Euler(rot));
                        AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, curEmoteId, randomResult);
                        IsCanClick = false;
                    }
                }
            }
        }
    }
}