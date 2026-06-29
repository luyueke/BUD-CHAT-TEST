using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using System.Collections.Generic;
using Game.Config;
using UI.UIPanels.GameEdit.SettingView;

/// <summary>
/// Author: Shaocheng
/// Desc: 通关设置面板
/// Date: 2023年8月3日16:59:07
/// </summary>
public class GameGlobalSettingPanel : BasePanel<GameGlobalSettingPanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private TabView bannerTabView;


    private BaseSettingView gameSettingView;
    private BaseSettingView envirSettingView;
    private BaseSettingView soundSettingView;


    class BannerTabItemConfig
    {
        public GameGlobalEnum.BannerType BannerType;
        public string Name;

        public BaseSettingView SettingView;
    }

    List<BannerTabItemConfig> BannerConfig;

    Color NormalColor = Color.white;
    Color SelectColor = new Color32(255, 211, 54, 255);
    public override void OnCreate()
    {
        gameSettingView = GameObjectEx.FindChildByName(transform, "GameSetting").GetComponent<BaseSettingView>();
        envirSettingView = GameObjectEx.FindChildByName(transform, "EnvirSetting").GetComponent<BaseSettingView>();
        soundSettingView = GameObjectEx.FindChildByName(transform, "SoundSetting").GetComponent<BaseSettingView>();

        BannerConfig = new List<BannerTabItemConfig>()
        {
            new() { BannerType = GameGlobalEnum.BannerType.GameSetting, Name = "通关", SettingView = gameSettingView },
            new() { BannerType = GameGlobalEnum.BannerType.Environment, Name = "环境", SettingView = envirSettingView },
            new() { BannerType = GameGlobalEnum.BannerType.Sound, Name = "声音", SettingView = soundSettingView },
        };

        closeBtn.onClick.AddListener(CloseSelf);
        // 类型选项
        for (int i = 0; i < BannerConfig.Count; i++)
        {
            var itemCF = BannerConfig[i];
            var bannerItem = bannerTabView.CreateItem($"BannerType_{itemCF.BannerType.ToString()}", itemCF.Name);
            bannerItem.AddValueChangeCallListener(isOn => { bannerItem.ItemNameText.color = isOn ? SelectColor : NormalColor; });
        }

        bannerTabView.RemoveAllSelectCallBack();
        bannerTabView.AddItemSelectCallBack(OnBannerItemClick);
    }

    void OnBannerItemClick(TabItem item, int index)
    {
        foreach (var config in BannerConfig)
        {
            config.SettingView.gameObject.SetActive(false);
        }
        var bannerCF = BannerConfig[index];
        bannerCF.SettingView.gameObject.SetActive(true);
        bannerCF.SettingView.OnTabSelected();
    }

    public override void OnShow(params object[] args)
    {
        // 默认选择一个Type栏
        var bannerTypeArg = (GameGlobalEnum.BannerType)args[0];
        var bannerTabIndex = BannerConfig.FindIndex(x => x.BannerType == bannerTypeArg);
        bannerTabView.SetSelect(bannerTabIndex);
        BannerConfig[bannerTabIndex].SettingView.OnTabSelected();
    }
}
