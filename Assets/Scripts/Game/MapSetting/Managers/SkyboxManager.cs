
using System;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Base;
using Game.MapSetting;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using GameData.Manager;
using UnityEngine;

namespace Game.MapSetting
{
	public class SkyboxConfig : BasePropConfigData
    {
    }


	public class SkyboxManager : BaseMapSettingManager<SkyboxManager>
	{
		private Light globalLight;
		private Material normalSkyboxMat;
		private Material cubeMapSkyMat;
		private SkyboxComponent skyboxComponent;
		
		
		public override void OnCreateByData()
		{
			if(GameDataManager.Inst?.mapGlobalData?.GetCurInfo<MapInfo>()?.gameSetting?.aiGameId == 3){
				return;
			}
			globalLight = GameObject.Find("MapSceneDirectional Light")?.GetComponent<Light>();
			if (globalLight == null)
			{
				globalLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
			}
			normalSkyboxMat = XAssetLoaderMgr.Inst.LoadResource<Material>("Assets/Arts/Game/Skybox/Material/Skybox.mat", globalLight.gameObject);
			cubeMapSkyMat =  XAssetLoaderMgr.Inst.LoadResource<Material>("Assets/Arts/Game/Skybox/Material/Skybox_cubemap.mat", globalLight.gameObject);
			base.OnCreateByData();
			skyboxComponent = GameMapSettingManager.Inst.settingEntity.GetComp<SkyboxComponent>();
			if (skyboxComponent == null)
			{
				skyboxComponent = GameMapSettingManager.Inst.settingEntity.AddComp<SkyboxComponent>();
				// 添加默认数据
				SetSkyboxData(() =>
				{
					SetSkyboxLight();
				});
			}
			else
			{
				SetSkyboxData();
			}

		}
		public void SetSkyboxColor(SkyboxColor skyboxColor)
		{
			if (skyboxColor == null)
			{
				return;
			}

			if (skyboxComponent == null)
			{
				return;
			}

			skyboxComponent.skyboxColor = skyboxColor;
			SetSkyboxColor();
		}
		
		public void SetSkyboxColor()
		{
			if (skyboxComponent == null || skyboxComponent.skyboxColor == null)
			{
				return;
			}

			RenderSettings.ambientSkyColor = skyboxComponent.skyboxColor.sky;
			RenderSettings.ambientEquatorColor = skyboxComponent.skyboxColor.equator;
			RenderSettings.ambientGroundColor =  skyboxComponent.skyboxColor.ground;
		}
		
		public void SetSkyboxData(Action complete = null)
		{
			var skyboxData =  DataTables.GetNormalSkyboxData(skyboxComponent.skyboxId);
			// 设置 环境光
			RenderSettings.ambientMode = (UnityEngine.Rendering.AmbientMode) skyboxData.GradientType;
			RenderSettings.ambientLight = skyboxData.Sky;
			RenderSettings.reflectionIntensity = skyboxData.ReflectionIntensity;

			if (skyboxComponent.skyboxColor == null)
			{
				skyboxComponent.skyboxColor = new SkyboxColor()
				{
					sky = skyboxData.Sky,
					equator = skyboxData.Equator,
					ground = skyboxData.Dirctional,
				};
			}
			SetSkyboxColor();
			// 设置天空盒
			switch (skyboxData.skyType)
			{
				case 1:
					SetNormalSkyboxTexture(skyboxData,complete);
					break;
				case 2:
					SetCubeMapSkyboxTexture(skyboxData,complete);
					break;
				
			}
		}
		public void SetSkyboxLight()
		{
			var skyboxData =  DataTables.GetNormalSkyboxData(skyboxComponent.skyboxId);
			DirLightManager.Inst.SetIntensity(skyboxData.Intensity);
			DirLightManager.Inst.SetElevationAngle(skyboxData.Angle.x);
			DirLightManager.Inst.SetDirectionAngle(skyboxData.Angle.y);
			DirLightManager.Inst.SetLightColor(skyboxData.Dirctional);
		}
		
		public void SetNormalSkyboxTexture(NormalSkyboxData data,Action complete)
		{
			var textureNames = data.textures;
			BatchLoadTextures(textureNames, (suc, textures) =>
			{
				if (suc)
				{
					normalSkyboxMat.SetTexture("_FrontTex", textures[0]);
					normalSkyboxMat.SetTexture("_BackTex", textures[1]);
					normalSkyboxMat.SetTexture("_LeftTex", textures[2]);
					normalSkyboxMat.SetTexture("_RightTex", textures[3]);
					normalSkyboxMat.SetTexture("_UpTex", textures[4]);
					normalSkyboxMat.SetTexture("_DownTex", textures[5]);
					RenderSettings.skybox = normalSkyboxMat;
				}
				complete?.Invoke();
			});
			
			
		}
		
		protected void BatchLoadTextures(List<string> texNames, Action<bool,List<Texture2D>> complete)
		{
		
			int downCount = 0;
			var textures = new List<Texture2D>(6) {null, null, null, null, null, null};
			for (var i = 0; i < texNames.Count; i++)
			{
				int index = i;
				var wrapper = Loader.LoadAsync<Texture2D>("Assets/Arts/Game/Skybox/Texture/" + texNames[i] + ".png");
				wrapper.completed = success =>
				{
					if (success && globalLight != null)
					{
						var tex = wrapper.RetainAsset(globalLight.gameObject);
						if (tex != null)
						{
							textures[index] = tex;
						}
					}

					downCount++;
					if (downCount == texNames.Count)
					{
						bool isAllLoaded = (textures.Count == texNames.Count);
						complete?.Invoke(isAllLoaded,textures);
					}
				};
			}
		}


		private void SetCubeMapSkyboxTexture(NormalSkyboxData data,Action complete)
		{
			var wrapper = Loader.LoadAsync<Cubemap>("Assets/Arts/Game/Skybox/Texture/" + data.textures[0] + ".png");
			wrapper.completed = success =>
			{
				if (success)
				{
					var tex = wrapper.RetainAsset(globalLight.gameObject);
					cubeMapSkyMat.SetTexture("_Tex", tex);
					RenderSettings.skybox = cubeMapSkyMat;
				}
				complete?.Invoke();
			};
		}

		public SkyboxComponent GetComp()
		{
			return skyboxComponent;
		}

	}
}
        
