
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;

namespace Game.Props.PropsManagers
{
	public class PGCEffectConfig : BasePropConfigData
    {
	    public string defColor;
	    public string soundName;
    }


    [NodeBehaviourAttribute(typeof(PGCEffectBehaviour))]
	public class PGCEffectManager : BaseNodeManager
	{
		public List<PGCEffectConfig> PGCConfigs;
		public string lastChooseID = GameConsts.DefaultEffectId;//最后一次选择的Effect id
		public PGCEffectManager()
		{
			InitConfig();
		}

		private List<PGCEffectConfig> InitConfig()
		{
			PGCConfigs = new List<PGCEffectConfig> ();
			PGCConfigs.Add(new PGCEffectConfig{Id="20200201",defColor = "F8DB4E",soundName = "PGC_SFX_Star_Shine_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200202",defColor = "FFF592",soundName = "PGC_SFX_Star_Shine_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200203",defColor = "FFE89B",soundName = "PGC_SFX_LightBeam_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200204",defColor = "FFD555",soundName = "PGC_SFX_Aperture_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200205",defColor = "C6F6FF",soundName = "PGC_SFX_LightBeam_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200206",defColor = "F38E3D",soundName = "PGC_SFX_Ray_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200207",defColor = "83D0FF",soundName = "PGC_SFX_LightBeam_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200208",defColor = "99DCF1",soundName = "PGC_SFX_ElectricCurrent_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200209",defColor = "F2FDFF",soundName = "PGC_SFX_Ice_And_Snow_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200210",defColor = "FF811E",soundName = "PGC_SFX_Spark_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200211",defColor = "D0F5FF",soundName = "PGC_SFX_WaterWaves_Loop"});
			PGCConfigs.Add(new PGCEffectConfig{Id="20200212",defColor = "CBEDFF",soundName = "PGC_SFX_Wind_Loop"});

			foreach (var stoneConfig in PGCConfigs)
			{
				var gamePropData = GamePropDataHelper.GetPropDataByID(stoneConfig.Id);
				stoneConfig.IconName = gamePropData.IconName;
			}

			return PGCConfigs;
		}

		public PGCEffectConfig GetConfigDataById(string propId)
		{
			if (PGCConfigs == null)
			{

				PGCConfigs = InitConfig();
			}

			var config = PGCConfigs.Find(x => x.Id == propId);
			return config;
		}

		private void PlayAllSound()
		{
			foreach (var nodeBehav in entities)
			{
				var pgcBehav = nodeBehav as PGCEffectBehaviour;
				pgcBehav.PlaySound(true);
			}
		}

		public void StopAllSound()
		{
			foreach (var nodeBehav in entities)
			{
				var pgcBehav = nodeBehav as PGCEffectBehaviour;
				pgcBehav.PlaySound(false);
			}
		}


		private void PlayAllEffect()
		{
			foreach (var nodeBehav in entities)
			{
				var pgcBehav = nodeBehav as PGCEffectBehaviour;
				pgcBehav.PlayParticleEffect();
			}
		}

		public override void OnEdit()
		{
			base.OnEdit();
			StopAllSound();
		}

		public override void OnGuest()
		{
			base.OnGuest();
			PlayAllSound();
			PlayAllEffect();
		}

		public override void OnPlay()
		{
			base.OnPlay();
			PlayAllSound();
			PlayAllEffect();
		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
		{
			nodeBehaviour.entity.AddComp<PGCEffectComponent>();
			PGCEffectBehaviour pgcBehaviour = nodeBehaviour as PGCEffectBehaviour;
			var pgcComp = pgcBehaviour.entity.GetComp<PGCEffectComponent>();
			var config = GetConfigDataById(lastChooseID);
			pgcComp.Color = FormatUtils.StringToColor(config.defColor);
			CreateAssetObj(pgcBehaviour,lastChooseID);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			PGCEffectBehaviour pgcBehaviour = nodeBehaviour as PGCEffectBehaviour;
			var goComp =  pgcBehaviour.entity.GetComp<GameObjectComponent>();
			CreateAssetObj(pgcBehaviour,goComp.PropId);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			PGCEffectBehaviour pgcBehaviour = newBehaviour as PGCEffectBehaviour;
			var pgcComp = pgcBehaviour.entity.GetComp<PGCEffectComponent>();
			pgcBehaviour.SetColor(pgcComp.Color);
		}

		private void CreateAssetObj(PGCEffectBehaviour pgcBehaviour,string propId)
		{
            if (!PGCConfigs.Exists(tmp => tmp.Id == propId)) {
                propId = GameConsts.DefaultEffectId;
            }
			var pgcComp = pgcBehaviour.entity.GetComp<PGCEffectComponent>();
			var goComp =  pgcBehaviour.entity.GetComp<GameObjectComponent>();
			goComp.PropId = propId;
			var newAssetObj = ModelCachePool.Inst.Get(propId);
			pgcBehaviour.SetAssetObj(newAssetObj);
			pgcBehaviour.SetColor(pgcComp.Color);
		}

		public void UpdateAssetObj(PGCEffectBehaviour pgcBehaviour,string propId)
		{
			var pgcComp = pgcBehaviour.entity.GetComp<PGCEffectComponent>();
			var goComp =  pgcBehaviour.entity.GetComp<GameObjectComponent>();
			if (pgcBehaviour.assetObj != null)
			{
				ModelCachePool.Inst.Release(goComp.PropId, pgcBehaviour.assetObj);
			}

			goComp.PropId = propId;
			var newAssetObj = ModelCachePool.Inst.Get(propId);
			pgcBehaviour.SetAssetObj(newAssetObj);
			pgcBehaviour.SetColor(pgcComp.Color);
		}
	}
}

