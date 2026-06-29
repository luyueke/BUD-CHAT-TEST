
using System.Collections.Generic;
using Basic;
using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.SurfaceDetection;
using Game.SurfaceDetection.Base;
using Game.Utils;
using GameData;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsManagers
{
	[NodeBehaviourAttribute(typeof(WaterCubeBehaviour))]
	public class WaterCubeManager : BaseNodeManager
	{
		private const string TAG = "WaterCubeManager";
		private Dictionary<PropModelShape, string> prefabPathDict = new Dictionary<PropModelShape, string>();
		public const float SlowSpeed = 0.2f;
		public const float MediumSpeed = 0.4f;
		public const float FastSpeed = 0.6f;
		
		//水方块相机遮罩逻辑
		private bool isShowPanel = false;
		private const float boxHalfLength = 0.55f;//比0.5大，防止出现死角
		private const float cylinderRadius = 0.6f;
		private Vector3 camLenOffect = new Vector3(0, 0, 0);
		private Vector3 boxRightTop = new Vector3(boxHalfLength, boxHalfLength, boxHalfLength);
		private Vector3 boxLeftBottom = new Vector3(-boxHalfLength, -boxHalfLength, -boxHalfLength);
		private Camera mainCamera;
		private const string WaterCubeTag = "WaterCube";
		
		
		public WaterCubeManager()
		{
			prefabPathDict.Add(PropModelShape.Cube,"Assets/Loadable/Model3D/Editor_Props/waterCube/gdgt_WaterCube_PREFAB.prefab");
			prefabPathDict.Add(PropModelShape.Cylinder,"Assets/Loadable/Model3D/Editor_Props/waterCube/gdgt_WaterCylinder_PREFAB.prefab");
			
			SurfaceDetectManager.Inst.AddSurfaceHandler(new WaterCubeSurfaceHandler());
		}

		public Es.WaterCubeConfig GetConfigDataByWaterId(int id)
		{
			var result = Es.DataTables.GetWaterCubeConfig(id);
			if (result == null)
			{
				result = Es.DataTables.GetWaterCubeConfig(1);
				LoggerUtils.LogError($"{TAG} GetConfigDataByWaterId is null id:"+id);
			}
			return result;
		}

		public List<Es.WaterCubeConfig> GetWaterCubeConfigList()
		{
			return Es.DataTables.GetWaterCubeConfigList();
		}

		public string GetPrefabPath(PropModelShape modelType)
		{
			if (prefabPathDict.ContainsKey(modelType))
			{
				return prefabPathDict[modelType];
			}

			return prefabPathDict[PropModelShape.Cube];
		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			var waterComp = nodeBehaviour.entity.AddComp<WaterCubeComponent>();
			CreateAssetObj(nodeBehaviour as WaterCubeBehaviour,waterComp.ModelShape);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			var waterComp = nodeBehaviour.entity.GetComp<WaterCubeComponent>();
			CreateAssetObj(nodeBehaviour as WaterCubeBehaviour,waterComp.ModelShape);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			WaterCubeBehaviour waterBehaviour = newBehaviour as WaterCubeBehaviour;
			waterBehaviour.Refresh();
		}


		private void CreateAssetObj(WaterCubeBehaviour waterBehaviour,PropModelShape modelShape)
		{
			var waterComp = waterBehaviour.entity.GetComp<WaterCubeComponent>();
			var assetId = waterBehaviour.GetAssetId();
			var prefabPath = GetPrefabPath(modelShape);
			var newAssetObj = ModelCachePool.Inst.Get(assetId,prefabPath);
			waterBehaviour.SetAssetObj(newAssetObj);
			waterBehaviour.SetConfig(waterComp.WaterId);
			waterBehaviour.SetSpeed(waterComp.Speed);
			waterBehaviour.SetTiling(waterComp.Tile);
		}

		public void UpdateAssetObj(WaterCubeBehaviour waterBehaviour,PropModelShape modelShape)
		{
			var waterComp = waterBehaviour.entity.GetComp<WaterCubeComponent>();
			var assetId = waterBehaviour.GetAssetId();
			if (waterBehaviour.assetObj != null)
			{
				ModelCachePool.Inst.Release(assetId, waterBehaviour.assetObj);
			}

			waterComp.ModelShape = modelShape;
			CreateAssetObj(waterBehaviour,modelShape);
		}


		private void AddAvatarRaycastListener()
		{
			//因为OnPlay调用顺序问题，延迟一帧监听
			TimerManager.Inst.RunOnce("", 0.1f, () =>
			{
				AvatarRaycast avtRaycast = AvatarController.Inst.SelfAvatarRaycast;
				avtRaycast.OnRaycast.AddListener(OnAvatarRaycast);
			});
		}

		private void RemovevatarRaycastListener()
		{
			AvatarRaycast avtRaycast = AvatarController.Inst.SelfAvatarRaycast;
			if (avtRaycast != null)
			{
				avtRaycast.OnRaycast.RemoveListener(OnAvatarRaycast);
				UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow,PanelId.WaterCameraEffectPanel);
				isShowPanel = false;
			}

		}


		public override void OnPlay()
		{
			base.OnPlay();
			AddAvatarRaycastListener();
		}

		public override void OnGuest()
		{
			base.OnGuest();
			AddAvatarRaycastListener();
		}

		public override void OnEdit()
		{
			base.OnEdit();
			RemovevatarRaycastListener();
		}

		protected override void OnNotifyRelease()
		{
			base.OnNotifyRelease();
			RemovevatarRaycastListener();
		}

		private void OnAvatarRaycast(Collider[] colliders)
		{
			SetCameraWaterEffect(colliders);
		}

		/// <summary>
		/// 判断相机是否在水方块里
		/// </summary>
		/// <param name="waterTransform"></param>
		/// <param name="shape"></param>
		/// <returns></returns>
		private bool IsCameraInWaterCube(Transform waterTransform,PropModelShape shape)
		{
			if (mainCamera == null)
			{
				mainCamera = GameCameraUtils.Inst.GetMainCamera();
			}
			var camSceneWorldPos = mainCamera.transform.TransformPoint(camLenOffect);
			var pos = waterTransform.transform.InverseTransformPoint(camSceneWorldPos);
			bool isCube = shape == PropModelShape.Cube;
			bool result = false;
			if (isCube)
			{
				result = pos.x < boxRightTop.x && pos.x > boxLeftBottom.x && 
				         pos.y < boxRightTop.y && pos.y > boxLeftBottom.y && 
				         pos.z < boxRightTop.z && pos.z > boxLeftBottom.z;
			}
			else
			{
				Vector2 vec2 = new Vector2(pos.x, pos.z);
				var dis = Vector2.Dot(vec2, vec2);
				result = dis < cylinderRadius && pos.y < boxHalfLength && pos.y > -boxHalfLength;
			}

			return result;
		}
		
		
		/// <summary>
		/// 判断相机是否入水，如果是则显示相机遮罩
		/// </summary>
		/// <param name="waterCubeHitList"></param>
		public void SetCameraWaterEffect(Collider[] waterCubeHitList)
	    {
	        if (waterCubeHitList.Length <= 0)
	        {
	            if(isShowPanel)
	            {
	                UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow,PanelId.WaterCameraEffectPanel);
	                isShowPanel = false;
	            }
	            return;
	        }
	        
	        for (int i = 0; i < waterCubeHitList.Length; i++)
	        {
		        if (waterCubeHitList[i].tag != WaterCubeTag)
		        {
			        continue;
		        }

		        var behav = waterCubeHitList[i].transform.GetComponentInParent<WaterCubeBehaviour>();
		        if (behav == null)
		        {
			        continue;
		        }

		        var waterComp = behav.entity.GetComp<WaterCubeComponent>();
	            int waterId = waterComp.WaterId;
	            
	            bool isIn =  IsCameraInWaterCube(waterCubeHitList[i].transform,waterComp.ModelShape);
	            if (isIn)
	            {
		            var config = GlobalNodeManager.Inst.Get<WaterCubeManager>().GetConfigDataByWaterId(waterId);
	                if(!isShowPanel)
	                {
		                UIAgentManager.Inst.OpenPanel(PanelId.WaterCameraEffectPanel,config.innerColor);
		                isShowPanel = true;
	                }
	                return;
	            }
	        }
	        if(isShowPanel)
	        {
		        UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow,PanelId.WaterCameraEffectPanel);
		        isShowPanel = false;
	        }
	    }
	}
}
        
