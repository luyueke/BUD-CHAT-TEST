
using System;
using DG.Tweening;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class InteractiveBoardBehaviour : NodeBaseBehaviour
    {
         private MeshRenderer[] meshRenderers;
        private Color[] orginColors;
        private static MaterialPropertyBlock mpb;
        public BoxCollider boxCollider;
        public Transform carryTran;

        private bool isFull;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            boxCollider = GetComponentInChildren<BoxCollider>();
            carryTran = transform.Find("carryNode");
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
        }


        public void SetRenderEnable(bool isEnable)
        {
            if(meshRenderers == null)
            {
                meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            }

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = isEnable;
            }
        }

         public void SetCurStatu(bool isFull)
        {
            this.isFull = isFull;
            boxCollider.enabled = !isFull;
        }

        public void ReSetCarryNode()
        {
            carryTran.localRotation = Quaternion.identity;
            carryTran.localPosition = Vector3.zero;
            carryTran.DOKill();
        }
        public void SetPlayerNode(bool isSelf)
        {
            if(isSelf)
            {
                carryTran.localPosition = new Vector3(0, 0.95f, 0);
            }
            else
            {
                carryTran.localPosition =  Vector3.zero;
            }
        }


        public override string GetTouchName() {
            return entity.GetComp<InteractiveBoardComponent>().ShowText;
        }

        public override void OnTouchClick()
        {
            if (AvatarController.Inst.SelfStateController.IsInLinkEmote() || AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
            {
                UIAgentManager.Inst.ShowToast("牵手状态下不可以点击地图中的交互道具哦");
                return;
            }
            AkSoundManager.Inst.PlayInteractable3DSound("Universal_Button",gameObject);
            var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
            if (!playerStateCtrl.CanEnterState(PlayerState.InteractiveBoard)) return;
            GlobalNodeManager.Inst.Get<InteractiveBoardManager>().PlayerSendOnBoard(this);


        }

        public void OnDisable()
        {
            SetCurStatu(false);
            GlobalNodeManager.Inst.Get<InteractiveBoardManager>().OnBoardDisable(carryTran,entity.GetComp<GameObjectComponent>().Uid);
        }


        //上板成功
        public void OnBoardSuccess()
        {
            SetCurStatu(true);
            ReSetCarryNode();
            // if (PortalPlayPanel.Instance != null
            //     && PortalPlayPanel.Instance.gameObject.activeSelf
            //     && PortalPlayPanel.Instance.GetCurTargetId() == entity.Get<GameObjectComponent>().uid.ToString())
            // {
            //     PortalPlayPanel.Hide();
            // }
        }

        //下板成功
        public void DownBoardSuccess()
        {
            SetCurStatu(false);
            ReSetCarryNode();
        }

        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            // HighLightUtils.HighLight(isHigh, mpb, ref orginColors, meshRenderers);
        }
    }
}

