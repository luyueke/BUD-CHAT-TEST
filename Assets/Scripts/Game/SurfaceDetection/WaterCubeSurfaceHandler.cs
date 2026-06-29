using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.SurfaceDetection.Base;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Avatar.FSM.DataStructure;
using Game.SurfaceDetection.Base;
using Message;
using UnityEngine;

namespace Game.SurfaceDetection
{
    public class WaterCubeSurfaceHandler : BaseSurfaceHandler
    {
        
        private readonly Vector3 waterCubeTriggerSize = new Vector3(0.4f, 1.3f, 0.4f);
        private readonly Vector3 waterCubeTriggerCenter = new Vector3(0, -0.65f, 0);
        private Collider activeWaterCubeCollider = null;
        private SwimKCC.PlayerWaterStatus playerWaterStatus = SwimKCC.PlayerWaterStatus.AllOutWater;
        
   
        
        
        public WaterCubeSurfaceHandler() : base()
        {
            Tag = "WaterCube";
            Priority = SurfaceDetectPriority.WaterCube;
        }

		public override bool IsCanSurfaceDetect()
        {
            //TODO:自定义是否能检测
            return true;
        }



        public override bool HandleOverlapRaycastResult(Collider[] colliders)
        {
            if (colliders == null || colliders.Length == 0)
            {
                return false;
            }
            bool containsInWater = false;
            var waterColliders = colliders.Where(tmp => tmp != null && tmp.gameObject.CompareTag(Tag))
                .ToArray();
            if (waterColliders.Length > 0)
            {
                var tmpPlayerWaterStatus  = ContainsPlayer(waterColliders, playerWaterStatus != SwimKCC.PlayerWaterStatus.AllOutWater);
                if (tmpPlayerWaterStatus != playerWaterStatus)
                {
                    Debug.Log("WaterCubeSurfaceHandler tmpPlayerWaterStatus:" + tmpPlayerWaterStatus);
                    MessageHelper.Broadcast(StateMessage.StateWaterChange, tmpPlayerWaterStatus);
                    CheckShowOxygen(HitGameObject, tmpPlayerWaterStatus);
                }
                playerWaterStatus = tmpPlayerWaterStatus;
                containsInWater = playerWaterStatus != SwimKCC.PlayerWaterStatus.AllOutWater;
            }
            
            //TODO:二次检测
            return containsInWater;
        }

        public override void OnEnter()
        {
            Debug.Log("WaterCubeSurfaceHandler OnEnter");
            //TODO:切换互斥状态
            AvatarController.Inst.SelfStateController.EnterState(PlayerState.Swim);
            MessageHelper.Broadcast(StateMessage.StateEnterWater);
            CheckShowOxygen(HitGameObject);
        }

        public override void OnChange(GameObject oldGo, GameObject newGo)
        {
            Debug.Log("WaterCubeSurfaceHandler OnChange");
            CheckShowOxygen(newGo);
        }

        public override void OnExit()
        {
            Debug.Log("WaterCubeSurfaceHandler OnExit");
            OxygenManager.Inst.RecoverOxygen();
            AvatarController.Inst.SelfStateController.ExitState(PlayerState.Swim);
            MessageHelper.Broadcast(StateMessage.StateExitWater);
            activeWaterCubeCollider = null;
            playerWaterStatus = SwimKCC.PlayerWaterStatus.AllOutWater;

        }

        private void CheckShowOxygen(GameObject hitObj)
        {
            var waterCubeBehaviour = hitObj?.GetComponentInParent<WaterCubeBehaviour>();
            if (waterCubeBehaviour != null)
            {
                var waterComp = waterCubeBehaviour.entity.GetComp<WaterCubeComponent>();
                if (waterComp.OxygenType == 1)
                {
                    OxygenManager.Inst.OpenOxygen();
                }
                else
                {
                    OxygenManager.Inst.RecoverOxygen();
                }
            }
        }

        private void CheckShowOxygen(GameObject hitObj, SwimKCC.PlayerWaterStatus waterStatus)
        {
            var waterCubeBehaviour = hitObj?.GetComponentInParent<WaterCubeBehaviour>();
            if (waterCubeBehaviour != null)
            {
                var waterComp = waterCubeBehaviour.entity.GetComp<WaterCubeComponent>();
                if (waterComp.OxygenType == 1)
                {
                    if (waterStatus == SwimKCC.PlayerWaterStatus.HeadOutWater || waterStatus == SwimKCC.PlayerWaterStatus.AllOutWater)
                    {
                        OxygenManager.Inst.RecoverOxygen();
                    }
                    else
                    {
                        OxygenManager.Inst.OpenOxygen();
                    }
                }
            }
        }

        private SwimKCC.PlayerWaterStatus ContainsPlayer(Collider[] colliders, bool isInWater)
        {
            // TODO: 死亡状态不进入水状态
    
            SwimKCC.PlayerWaterStatus status;
            
            if (!isInWater)
            {
                var verts = GetCornersForBoxCollider();
                foreach (var collider in colliders)
                {
                    for (var i = verts.Count - 1; i >= 0; i--)
                    {
                        if (IsContains(collider, verts[i]))
                        {
                            verts.RemoveAt(i);
                        }
                    }
                }

                activeWaterCubeCollider = verts.Count == 0 ? colliders[0] : null;
                status = verts.Count == 0 ? SwimKCC.PlayerWaterStatus.AllInWater : SwimKCC.PlayerWaterStatus.AllOutWater;
            }
            else
            {
                var upVerts = GetUpCornersForBoxCollider();
                var downVerts = GetDownloadCornersForBoxCollider();
                foreach (var collider in colliders)
                {
                    for (var i = upVerts.Count - 1; i >= 0; i--)
                    {
                        if (IsContains(collider, upVerts[i]))
                        {
                            upVerts.RemoveAt(i);
                        }
                    }

                    for (var i = downVerts.Count - 1; i >= 0; i--)
                    {
                        if (IsContains(collider, downVerts[i]))
                        {
                            downVerts.RemoveAt(i);
                        }
                    }
                }

                if (downVerts.Count == 0 && upVerts.Count != 0)
                {
                    //头部露出
                    status = SwimKCC.PlayerWaterStatus.HeadOutWater;
                } else if (upVerts.Count == 0 && downVerts.Count != 0)
                {
                    status = SwimKCC.PlayerWaterStatus.FootOutWater;
                    //脚部露出
                } else if (downVerts.Count == 0 && upVerts.Count == 0)
                {
                    status = SwimKCC.PlayerWaterStatus.AllInWater;
                    //全身浸入
                } else 
                {
                    status = SwimKCC.PlayerWaterStatus.AllOutWater;
                }
                activeWaterCubeCollider = upVerts.Count == 0 || downVerts.Count == 0 ? colliders[0] : null;
                
            }
            return status;
        }

        private List<Vector3> GetCornersForBoxCollider()
        {
            var verts = new List<Vector3>();
            
            var selfControllerTrans = AvatarController.Inst.SelfController.transform.Find("Center");
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, -waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, -waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, -waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, -waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            return verts;
        }

        private List<Vector3> GetUpCornersForBoxCollider()
        {
            var verts = new List<Vector3>();
            var selfControllerTrans = AvatarController.Inst.SelfController.transform.Find("Center");
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            return verts;
        }

        private List<Vector3> GetDownloadCornersForBoxCollider()
        {
            var verts = new List<Vector3>();
            var selfControllerTrans = AvatarController.Inst.SelfController.transform.Find("Center");
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, -waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, -waterCubeTriggerSize.y, -waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(waterCubeTriggerSize.x, -waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            verts.Add(selfControllerTrans.TransformPoint(waterCubeTriggerCenter + new Vector3(-waterCubeTriggerSize.x, -waterCubeTriggerSize.y, waterCubeTriggerSize.z)*0.5f));
            return verts;
        }

        private bool IsContains(Collider collider, Vector3 point)
        {
            var originPos = collider.transform.position;
            var dir = originPos - point;
            var dis = Vector3.Distance(point, originPos);
            var ray = new Ray(point, dir);
            var hits = Physics.RaycastAll(ray, dis, LayerMask.GetMask("GameSurface"));
            return hits.Length == 0 || hits.All(hit => hit.collider != collider);
        }

    }
}