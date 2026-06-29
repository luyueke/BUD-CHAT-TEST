using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.Props.PropsComponents;
using Game.Utils;
using UnityEngine;

namespace Game.MapSetting
{
    public struct WeatherCofig
    {
        public int envMusicId;//天气对应的环境音
        public string effectPath;//天气对应的粒子路径
    }

    public class WeatherManager : BaseMapSettingManager<WeatherManager>,IModeManager,IGameMono
    {
        private WeatherComponent weatherComp;
        private Dictionary<GameGlobalEnum.WeatherType, WeatherCofig> weatherConfig;
        private Dictionary<GameGlobalEnum.WeatherType, GameObject> weatherCache;
        private GameObject curShowing;
        
        private bool rotationGet = false;
        private Quaternion oriRotation;
        private Transform followTarger;
        public WeatherManager()
        {
            InitConfig();
            weatherCache = new Dictionary<GameGlobalEnum.WeatherType, GameObject>();
        }
    
        /// <summary>
        /// 初始化配置，数据较少，暂时不用配置表
        /// </summary>
        private void InitConfig()
        {
            weatherConfig = new Dictionary<GameGlobalEnum.WeatherType, WeatherCofig>();
            
            weatherConfig[GameGlobalEnum.WeatherType.Rain] = new WeatherCofig
            {
                envMusicId = 20003,
                effectPath = "Assets/Loadable/Model3D/Editor_Props/weather/gdgt_weather_particle_rain_300_PREFAB.prefab"
            };
            
            weatherConfig[GameGlobalEnum.WeatherType.Snow] = new WeatherCofig
            {
                envMusicId = 20008,
                effectPath = "Assets/Loadable/Model3D/Editor_Props/weather/gdgt_weather_particle_snow_PREFAB.prefab"
            };
        }

        //地图还原数据
        public override void OnCreateByData()
        {
            base.OnCreateByData();
            weatherComp = GameMapSettingManager.Inst.settingEntity.GetComp<WeatherComponent>();
            if (weatherComp == null)
            {
                //空模板
                weatherComp = GameMapSettingManager.Inst.settingEntity.AddComp<WeatherComponent>();
                SetDefault();
            }
        }

        public void SetDefault()
        {
            weatherComp.WeatherType = GameGlobalEnum.WeatherType.None;
        }

        public WeatherComponent GetSettingComp()
        {
            return weatherComp;
        }

        public void SetWeatherType(GameGlobalEnum.WeatherType weatherType)
        {
            weatherComp.WeatherType = weatherType;
        }

        public void ShowWeather()
        {
            var effectId = weatherComp.WeatherType;
            if (weatherCache.ContainsKey(effectId) && weatherCache[effectId] == curShowing && curShowing.activeSelf)
            {
                return;
            }
            
            if (curShowing != null && curShowing.activeSelf)
            {
                curShowing.SetActive(false);
                curShowing = null;
            }
            
            if (effectId == GameGlobalEnum.WeatherType.None)
            {
                return;
            }
            
            if (weatherCache.ContainsKey(effectId))
            {
                weatherCache[effectId].SetActive(true);
                curShowing = weatherCache[effectId];
                return;
            }

            var mainCamera = GameCameraUtils.Inst.GetMainCamera();
            string effectPath = weatherConfig[effectId].effectPath;
            GameObject effectPrefab = XAssetLoaderMgr.Inst.LoadResource<GameObject>(effectPath, mainCamera.gameObject);
            if (effectPrefab == null)
            {
                LoggerUtils.LogError($"Load Weather Effect Error effectId = {effectId}");
                return;
            }
            
          
            GameObject effect = Object.Instantiate(effectPrefab, mainCamera.transform);
            oriRotation = effectPrefab.transform.rotation;
            followTarger = mainCamera.transform;
            rotationGet = true;
            curShowing = effect;
            weatherCache[effectId] = effect;
            
            

        }
        
        public void PauseWeather()
        {
            if (curShowing != null && curShowing.activeSelf)
            {
                curShowing.SetActive(false);
            }
        }

        private void SetOriRotation()
        {
            
        }

        public void OnEdit()
        {
            PauseWeather();
        }

        public void OnPlay()
        {
            ShowWeather();
        }

        public void OnGuest()
        {
            ShowWeather();
        }

        public void Update()
        {
            float offset = 5;
            if (followTarger != null && curShowing !=null && curShowing.activeSelf == true)
            {
                Vector3 forward = followTarger.transform.forward;
                curShowing.transform.position = followTarger.position + forward.normalized * offset;
                
                if (rotationGet)
                {
                    curShowing.transform.rotation = oriRotation;
                }
            }
        }

        public void FixedUpdate()
        {
            
        }

        public override void Release()
        {
            base.Release();
            if (weatherCache != null && weatherCache.Count > 0)
            {
                foreach (var weaObj in weatherCache.Values)
                {
                    if (weaObj)
                    {
                        GameObject.Destroy(weaObj);
                    }
                }
            }
        }
    }
}
