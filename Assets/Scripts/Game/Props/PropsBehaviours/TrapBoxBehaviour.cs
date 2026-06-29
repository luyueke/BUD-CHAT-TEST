
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.KinematicCharacter;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using TMPro;
using UIAgent;
using UnityEngine;
using System.Collections;
using Game.Vehicle.PGCVehicle;
using Message;

namespace Game.Props.PropsBehaviours
{
    public class TrapBoxBehaviour : NodeBaseBehaviour
    {
        private GameObject boxGO;
        private MeshRenderer[] boxRenderers;
        private Color[] oldColor;

        private MeshRenderer textRenderer;
        private TextMeshPro textMesh;
        KinematicCharacterController selfPlayer => AvatarController.Inst.SelfController;
        PlayerStateController selfStateController => AvatarController.Inst.SelfStateController;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            boxGO = GameObjectEx.FindChildByName(transform,"box").gameObject;
            boxRenderers = boxGO.GetComponentsInChildren<MeshRenderer>(true);
     
            textRenderer = GameObjectEx.FindChildByName(transform,"Text").GetComponent<MeshRenderer>();
            textMesh = this.GetComponentInChildren<TextMeshPro>(true);
        }

        public void Reset()
        {
            textMesh.SetText("");
        }

        public void RefreshShowId()
        {
            var tComp = entity.GetComp<TrapBoxComponent>();
            textMesh.text = tComp.BoxIndex.ToString();
            // SetTextVisiable(tComp.TransType == (int)GameGlobalEnum.TrapBoxTrans.CustomSpawn);
        }
        
        public void SetBoxVisiable(bool state)
        {
            if (boxRenderers == null)
            {
                boxGO = GameObjectEx.FindChildByName(transform,"box").gameObject;
                boxRenderers = boxGO.GetComponentsInChildren<MeshRenderer>(true);
            }

            if (boxRenderers == null)
            {
                return;
            }

            foreach (var render in boxRenderers)
            {
                if (render != null)
                {
                    render.enabled = state;
                }
            }
        }
        
        public void SetTextVisiable(bool state)
        {
            if (textRenderer == null)
            {
                textRenderer = GameObjectEx.FindChildByName(transform,"Text").GetComponent<MeshRenderer>();
            }

            if (textRenderer != null)
            {
                textRenderer.enabled = state;
            }
            

            
        }

        public override void OnTrigEnter()
        {
            base.OnTrigEnter();
            if (AvatarController.Inst.SelfStateController.IsLinkPlayerB())
            {
                return;
            }

            //本地预测展示
            GlobalNodeManager.Inst.Get<TrapBoxManager>().DoHitTrap(this);
         
            var tComp = entity.GetComp<TrapBoxComponent>();
            if(tComp.TransType == (int)GameGlobalEnum.TrapBoxTrans.NoTrans)
            {
                DoTouchTrap();
            }
            else if (tComp.TransType == (int)GameGlobalEnum.TrapBoxTrans.CheckPoint)
            {
                //TODO:存档点逻辑
                // ArchivePointManager.Inst.DoBackPointLogic(()=>TouchTrapToast(tComp));
            }
            else
            {
                if (!UIAgentManager.Inst.FindPanel(WindowId.CommonWindow, PanelId.BlackPanel))
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.BlackPanel,true);
                }
                DoTouchTrap();
            }
        }
        
        private void DoTouchTrap()
        {
            var tComp = entity.GetComp<TrapBoxComponent>();
            TouchTrapToast(tComp);
            if(tComp.TransType != (int)GameGlobalEnum.TrapBoxTrans.NoTrans)
            {
                TouchTrapTransport(tComp);
            }  
        }
        
        //触发Toast
        private void TouchTrapToast(TrapBoxComponent boxComponent)
        {
            int hasTips = boxComponent.HasTips;
            if (hasTips == 0) return;

            string text = boxComponent.TipsStr;
            bool isDefault = hasTips == 0 || string.IsNullOrEmpty(text);

            string defText = "哎呀！你触发了一个陷阱！";
            string toast = isDefault ? defText : text;
            UIAgentManager.Inst.ShowToast(toast);
        }
        
        //触发传送
        private void TouchTrapTransport(TrapBoxComponent boxComponent)
        {
            int transType = boxComponent.TransType;
            
            if (transType == (int)GameGlobalEnum.TrapBoxTrans.MapSpawn) //回到出生点
            {
                var spManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
                var spawnPointTrans = spManager.GetRandomSpawnPoint().transform;
                if(selfStateController.PGCVehicleKinematicCtrl != null){
                    PGCVehicleManager.Inst.RemovePGCVehicle(selfStateController.PlayerID);
                    MessageHelper.Broadcast(MessageName.OnPlayerTryGetOutVehicle, selfStateController.PlayerID);
                }
                selfPlayer.Motor.SetPositionAndRotation(spawnPointTrans.position, spawnPointTrans.rotation);
                
            }
            else if(transType == (int)GameGlobalEnum.TrapBoxTrans.CustomSpawn) //自定义传送点
            {
                var point =  GlobalNodeManager.Inst.Get<TrapSpawnManager>().GetSpawnByUid(boxComponent.PointId);
                if (point == null)
                {
                    return;
                }
                if (point!= null)
                {
                    if(selfStateController.PGCVehicleKinematicCtrl != null){
                        PGCVehicleManager.Inst.RemovePGCVehicle(selfStateController.PlayerID);
                        MessageHelper.Broadcast(MessageName.OnPlayerTryGetOutVehicle, selfStateController.PlayerID);
                    }
                    Transform pointTransform = point.transform;
                    selfPlayer.Motor.SetPositionAndRotation(pointTransform.position, pointTransform.rotation);
                }
            }
        }
        
    }
}
        
