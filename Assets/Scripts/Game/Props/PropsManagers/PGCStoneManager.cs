
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Config;
using Game.ECS;
using Game.Utils;

namespace Game.Props.PropsManagers
{
	public class PGCStoneConfig : BasePropConfigData
	{
		public string defColor;
	}


	[NodeBehaviourAttribute(typeof(PGCStoneBehaviour))]
	public class PGCStoneManager : BaseNodeManager
	{
		public List<PGCStoneConfig> PGCStoneConfigs;
		public string lastChooseID = GameConsts.DefaultStoneId;//最后一次选择的岩石id
		
		
		public PGCStoneManager()
		{
			InitConfig();
		}
		
		private List<PGCStoneConfig> InitConfig()
		{
			PGCStoneConfigs = new List<PGCStoneConfig> ();
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200001",defColor = "BCADA6"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200002",defColor = "BCADA6"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200003",defColor = "BCADA6"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200004",defColor = "BCADA6"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200005",defColor = "BCADA6"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200006",defColor = "F8B382"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200007",defColor = "F8B382"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200008",defColor = "F8B382"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200009",defColor = "49C4FF"});
			PGCStoneConfigs.Add(new PGCStoneConfig(){Id="20200010",defColor = "49C4FF"});

			foreach (var stoneConfig in PGCStoneConfigs)
			{
				var gamePropData = GamePropDataHelper.GetPropDataByID(stoneConfig.Id);
				stoneConfig.IconName = gamePropData.IconName;
			}
			
			return PGCStoneConfigs;
		}
		
		
		public PGCStoneConfig GetConfigDataById(string propId)
		{
			if (PGCStoneConfigs == null)
			{
				PGCStoneConfigs = InitConfig();
			}

			var config = PGCStoneConfigs.Find(x => x.Id == propId);
			return config;
		}
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<PGCStoneComponent>();
			PGCStoneBehaviour stoneBehaviour = nodeBehaviour as PGCStoneBehaviour;
			var pgcStoneComp = stoneBehaviour.entity.GetComp<PGCStoneComponent>();
			var config = GetConfigDataById(lastChooseID);
			pgcStoneComp.Color = FormatUtils.StringToColor(config.defColor);
			CreateAssetObj(stoneBehaviour,lastChooseID);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			PGCStoneBehaviour stoneBehaviour = nodeBehaviour as PGCStoneBehaviour;
			var goComp =  stoneBehaviour.entity.GetComp<GameObjectComponent>();
			CreateAssetObj(stoneBehaviour,goComp.PropId);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			PGCStoneBehaviour stoneBehaviour = newBehaviour as PGCStoneBehaviour;
			var pgcStoneComp = stoneBehaviour.entity.GetComp<PGCStoneComponent>();
			stoneBehaviour.SetColor(pgcStoneComp.Color);
		}

		private void CreateAssetObj(PGCStoneBehaviour stoneBehaviour,string propId)
		{
			var pgcStoneComp = stoneBehaviour.entity.GetComp<PGCStoneComponent>();
			var goComp =  stoneBehaviour.entity.GetComp<GameObjectComponent>();
			goComp.PropId = propId;
			var newAssetObj = ModelCachePool.Inst.Get(propId);
			stoneBehaviour.SetAssetObj(newAssetObj);
			stoneBehaviour.SetColor(pgcStoneComp.Color);
		}

		public void UpdateAssetObj(PGCStoneBehaviour stoneBehaviour,string propId)
		{
			var pgcStoneComp = stoneBehaviour.entity.GetComp<PGCStoneComponent>();
			var goComp =  stoneBehaviour.entity.GetComp<GameObjectComponent>();
			if (stoneBehaviour.assetObj != null)
			{
				ModelCachePool.Inst.Release(goComp.PropId, stoneBehaviour.assetObj);
			}

			goComp.PropId = propId;
			var newAssetObj = ModelCachePool.Inst.Get(propId);
			stoneBehaviour.SetAssetObj(newAssetObj);
			stoneBehaviour.SetColor(pgcStoneComp.Color);
		}
	}
}
        
