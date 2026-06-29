using Game.Config;
using Game.MapSetting;
using Game.Props.PropsComponents;
using UnityEngine.UI;

/// <summary>
/// Author:Jaywill
/// Desc:天气系统选择面板
/// Date:23-08-07 17:22:46
/// </summary>
namespace UI.UIPanels.GameEdit
{
    public class WeatherSubView : BasePropertyEditSubView
    {
        private ToggleGroup toggleGroup;
        private Toggle rainToggle;
        private Toggle snowToggle;
        private WeatherComponent weatherComp;

        protected override void OnInit()
        {
            toggleGroup = GameObjectEx.FindChildByName(transform,"ToggleGroup").GetComponent<ToggleGroup>();
            rainToggle = GameObjectEx.FindChildByName(transform,"RainToggle").GetComponent<Toggle>();
            snowToggle = GameObjectEx.FindChildByName(transform,"SnowToggle").GetComponent<Toggle>();
            rainToggle.onValueChanged.AddListener(OnWeatherChange);
            snowToggle.onValueChanged.AddListener(OnWeatherChange);
        }

        protected override void OnStart()
        {
            base.OnStart();
            weatherComp = WeatherManager.Inst.GetSettingComp();
            InitByData();
        }

        private void InitByData()
        {
            var weatherType = weatherComp.WeatherType;
            rainToggle.SetIsOnWithoutNotify(weatherType == GameGlobalEnum.WeatherType.Rain);
            snowToggle.SetIsOnWithoutNotify(weatherType == GameGlobalEnum.WeatherType.Snow);
        }

        private void OnWeatherChange(bool isOn)
        {
            var activeToggle = toggleGroup.GetFirstActiveToggle();
            if (activeToggle == null)
            {
                WeatherManager.Inst.SetWeatherType(GameGlobalEnum.WeatherType.None);
            }
            else
            {
                if (activeToggle == rainToggle)
                {
                    WeatherManager.Inst.SetWeatherType(GameGlobalEnum.WeatherType.Rain);
                }
                else if(activeToggle == snowToggle)
                {
                    WeatherManager.Inst.SetWeatherType(GameGlobalEnum.WeatherType.Snow);
                }
            }
        }
    }
}