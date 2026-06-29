
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using UnityEngine;

namespace Game.Props.PropsManagers
{
	
	public class PGCPlantConfig : BasePropConfigData
	{
		public string defColor;
		public float minIntensity;
		public float maxIntensity;
	}
	
	
    [NodeBehaviourAttribute(typeof(PGCPlantBehaviour))]
	public class PGCPlantManager : BaseNodeManager
	{
		public List<PGCPlantConfig> PGCPlantConfigs;
		public string lastChooseID = GameConsts.DefaultPlantId;//最后一次选择的岩石id
		public PGCPlantManager()
		{
			InitConfig();
		}
		
		private List<PGCPlantConfig> InitConfig()
		{
			PGCPlantConfigs = new List<PGCPlantConfig> ();
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200101",defColor = "16B91B"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200102",defColor = "16B91B"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200103",defColor = "16B91B"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200104",defColor = "FFE21C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200105",defColor = "FFE21C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200106",defColor = "FFE21C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200107",defColor = "00A84C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200108",defColor = "00A84C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200109",defColor = "00A84C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200110",defColor = "00A84C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200111",defColor = "00A84C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200112",defColor = "54B91D"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200113",defColor = "54B91D"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200114",defColor = "54B91D"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200115",defColor = "FF002D"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200116",defColor = "FFE70B"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200117",defColor = "E47EFF"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200118",defColor = "FFE80D"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200119",defColor = "FF2519"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200120",defColor = "B6FF1C"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200121",defColor = "FBFFC6"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200122",defColor = "FBFFC6"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200123",defColor = "00A86F"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200124",defColor = "15CA00"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200125",defColor = "008722"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200126",defColor = "00D445"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200127",defColor = "FF5A10"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200128",defColor = "FF5A10"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200129",defColor = "FF5A10"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200130",defColor = "FF5A10"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200131",defColor = "008C22"});
			PGCPlantConfigs.Add(new PGCPlantConfig(){Id="20200132",defColor = "008C22"});

			foreach (var config in PGCPlantConfigs)
			{
				var gamePropData = GamePropDataHelper.GetPropDataByID(config.Id);
				config.IconName = gamePropData.IconName;
			}
			
			return PGCPlantConfigs;
		}
		
		public PGCPlantConfig GetConfigDataById(string propId)
		{
			if (PGCPlantConfigs == null)
			{
				PGCPlantConfigs = InitConfig();
			}

			var config = PGCPlantConfigs.Find(x => x.Id == propId);
			return config;
		}
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<PGCPlantComponent>();
			PGCPlantBehaviour plantBehaviour = nodeBehaviour as PGCPlantBehaviour;
			var plantComp = plantBehaviour.entity.GetComp<PGCPlantComponent>();
			var config = GetConfigDataById(lastChooseID);
			plantComp.Color = FormatUtils.StringToColor(config.defColor);
			CreateAssetObj(plantBehaviour,lastChooseID);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			PGCPlantBehaviour plantBehaviour = nodeBehaviour as PGCPlantBehaviour;
			var goComp =  plantBehaviour.entity.GetComp<GameObjectComponent>();
			CreateAssetObj(plantBehaviour,goComp.PropId);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			PGCPlantBehaviour plantBehaviour = newBehaviour as PGCPlantBehaviour;
			var pgcComp = plantBehaviour.entity.GetComp<PGCPlantComponent>();
			plantBehaviour.SetColor(pgcComp.Color);
		}

		private void CreateAssetObj(PGCPlantBehaviour plantBehaviour,string propId)
		{
			var pgcComp = plantBehaviour.entity.GetComp<PGCPlantComponent>();
			var goComp =  plantBehaviour.entity.GetComp<GameObjectComponent>();
			goComp.PropId = propId;
			var newAssetObj = ModelCachePool.Inst.Get(propId);
			plantBehaviour.SetAssetObj(newAssetObj);
			plantBehaviour.SetColor(pgcComp.Color);
			plantBehaviour.SetIntensity(propId);
		}

		public void UpdateAssetObj(PGCPlantBehaviour plantBehaviour,string propId)
		{
			var pgcComp = plantBehaviour.entity.GetComp<PGCPlantComponent>();
			var goComp =  plantBehaviour.entity.GetComp<GameObjectComponent>();
			if (plantBehaviour.assetObj != null)
			{
				ModelCachePool.Inst.Release(goComp.PropId, plantBehaviour.assetObj);
			}

			goComp.PropId = propId;
			var newAssetObj = ModelCachePool.Inst.Get(propId);
			plantBehaviour.SetAssetObj(newAssetObj);
			plantBehaviour.SetColor(pgcComp.Color);
			plantBehaviour.SetIntensity(propId);
		}
	
		
		//TODO:@Jaywill 后续美术这里会修改实现方式
		public void OnDragMoveEnd(GameObject target)
		{
			var behavs = target.GetComponentsInChildren<PGCPlantBehaviour>();
			if (behavs !=null && behavs.Length > 0)
			{
				foreach (var behav in behavs)
				{
					var comp = behav.entity.GetComp<GameObjectComponent>();
					var scaleY = target.transform.localScale.y;
					behav.SetIntensity(comp.PropId);
				}
			}
		}
	}
}
        
