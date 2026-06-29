using System;
using System.Collections;
using Es;
using Game.Avatar;
using Game.Props.PropsComponents;
using GameData.PgcData;
using Message;
using Pb.Base;
using UnityEngine;
using UnityEngine.Video;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereBathRoomBehaivour: AIPropBaseBehaviour
    {
        private string emoteId = "40200404";
        private Vector3 pos;
        private Vector3 rot;
        private string nodeName;
        private string curJsonContent;
        private Action<YandereAreaType> _sendPosAct;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIYandereCommonComponent>();
            pos = dataComp.Pos;
            rot = dataComp.Rot;
            MessageHelper.AddListener<string>(MessageName.AICancelEmote, CancelEmote);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.AICancelEmote, CancelEmote);
        }
        

        public void SetSendPosAct(Action<YandereAreaType> act)
        {
            this._sendPosAct = act;
        }
        
        private void CancelEmote(string emoId)
        {
            if (emoteId == emoId)
            {
                TimerManager.Inst.RunOnce("ChangeClothes", 0.5f, () =>
                {
                    var data = CharacterData.DeserializeObject(curJsonContent);
                    AvatarController.Inst.SelfWrap.SetCharacterData(data);
                    var playerCtrl = AvatarController.Inst.SelfStateController;
                    playerCtrl.EnterState(PlayerState.ChangeClothesAni);
                    IsCanClick = true;
                });
            }
        }

        protected Func<bool, bool> _checkStartGame;

        public void SetFunctions(Func<bool, bool> func)
        {
            this._checkStartGame = func;
        }
        
        public override void OnTouchClick()
        {
            if (IsCanClick && _checkStartGame(false))
            {
                IsCanClick = false;
                PlayerChangeClothes();
                TimerManager.Inst.RunOnce("ChangeClothes", 2, EnterBathRoom);
            }
        }


        private void PlayerChangeClothes()
        {
            var subType = UniqueType.GetAvatar(AvatarSubType.Hats);
            AvatarController.Inst.SelfWrap.ChangePart(subType,"10900064", () =>
            {
                AvatarController.Inst.SelfWrap.ChangeColor(subType,"#FFFFFF");
            });
            var subType1 = UniqueType.GetAvatar(AvatarSubType.Clothes);
            AvatarController.Inst.SelfWrap.ChangePart(subType1,"10400425");
            var playerCtrl = AvatarController.Inst.SelfStateController;
            playerCtrl.EnterState(PlayerState.ChangeClothesAni);
        }

        private void EnterBathRoom()
        {
            curJsonContent = AccountDataManager.Inst.UserInfo.avatarJson;
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
            if (emoAniDataList.Count > 0)
            {
                if (AvatarController.Inst.SelfStateController.CanEnterState(PlayerState.SingleEmote))
                {
                    _sendPosAct?.Invoke(YandereAreaType.Bathtub);
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : UnityEngine.Random.Range(1, random + 1);
                    AvatarController.Inst.SelfController.Motor.SetPositionAndRotation(pos,Quaternion.Euler(rot));
                    AvatarController.Inst.SelfStateController.EnterState(PlayerState.SingleEmote, emoteId, randomResult);
                }
            }
        }
    }
}