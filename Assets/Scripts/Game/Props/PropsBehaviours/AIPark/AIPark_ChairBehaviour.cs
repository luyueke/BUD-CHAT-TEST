using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Message;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_ChairBehaviour : AIPark_BaseInteractiveBehaviour
    {
        public Park_ChairType _chairType;
        public Vector3 pos;
        public Vector3 rot;
        private string _curEmoteId;
        public IList<int> _neighbor;

        public override bool AllowClickInEmoteLink => _chairType == Park_ChairType.WaitingRoom;


        public enum Park_ChairType
        {
            Default = 0,
            Injections = 1, // 候诊室
            WaitingRoom = 2, //候诊室
            Dean = 3, //院长
        }

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIGameCommonComponent>();
            pos = dataComp.Pos;
            rot = dataComp.Rot;
            _chairType = (Park_ChairType)dataComp.Index;
            _curEmoteId = GetInteractiveEmoteId();
            _neighbor = dataComp.neighbor;
            
            MessageHelper.AddListener<string>(MessageName.AICancelEmote, OnCancelEmote);
        }
        
        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.AICancelEmote, OnCancelEmote);
        }
        
        private void OnCancelEmote(string emoId)
        {
            if (_curEmoteId == emoId)
            {
                IsCanClick = true;
            }
        }

        private string GetInteractiveEmoteId()
        {
            switch (_chairType)
            {
                case Park_ChairType.Injections:
                    return "40200469";
                case Park_ChairType.WaitingRoom:
                    return "40200402";
                case Park_ChairType.Dean:
                    return "40200464";
            }
            return "";
        }


        public override void OnInteractive()
        {
            base.OnInteractive();
            // 椅子的交互逻辑
            if (!IsCanClick)
                return;

            IsCanClick = false;
            if (AllowClickInEmoteLink&&BLinkEmoteState)
            {
                LinkEmoteSit();
            }
            else
                DirectInto();
        }

        private void DirectInto()
        {
            SelfPlayer_PlayInteractiveAnim(_curEmoteId, pos, rot);
            
            switch (_chairType)
            {
                case Park_ChairType.Injections:
                    var InjectNpcState = AIPark_CharacterManager.Inst.FindNpcWithState<InjectionsToPatientsState>();
                    if (InjectNpcState != null)
                    {
                        InjectNpcState.DoInjections();
                    }
                    break;
                case Park_ChairType.WaitingRoom:
                    break;
                case Park_ChairType.Dean:
                    break;
            }
        }

        public void ForceEnterInjectChair()
        {
            SelfPlayer_PlayInteractiveAnim("40200481", pos, rot);
            var InjectNpcState = AIPark_CharacterManager.Inst.FindNpcWithState<InjectionsToPatientsState>();
            if (InjectNpcState != null)
            {
                InjectNpcState.DoInjections();
            }
        }

        public void LinkEmoteSit()
        {
            if (CheckNeighborOccupied())
            {
                //自己坐下
                SelfPlayer_PlayInteractiveAnim(_curEmoteId, pos, rot);
            }
            else
            {
                UIAgentManager.Inst.OpenPanel(PanelId.TipPanel, "这里坐不下两个人呢，换一个吧");
                IsCanClick = true;
            }
            //MessageHelper.Broadcast(MessageName.ShowToast, "这个椅子已经有人坐下了，换一个吧");
        }

        /// <summary>
        /// 检查是否旁边的椅子被占用
        /// </summary>
        public bool CheckNeighborOccupied()
        {
            foreach (var uid in _neighbor)
            {
                var chair = GlobalNodeManager.Inst.Get<AIPark_ChairManager>().GetBevsByUID(uid);
                if (chair != null&&chair.IsCanClick)
                {
                    //todo 让伙伴坐到这个椅子上
                    AIBuddy_PlayerInteractiveAnim("40200402", chair.pos,chair.rot);
                    chair.IsCanClick = false;
                    LoggerUtils.Log($"伙伴准备坐到椅子上 {uid} ");  
                    return true;
                }
            }
            return false;
        }
    }
} 