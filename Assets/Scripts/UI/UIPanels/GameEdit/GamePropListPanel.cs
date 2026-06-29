/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-01 11:02:35
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-30 16:33:44
 * @ Description: 道具列表
 */

using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Config;
using Game.Utils;
using UnityEngine.U2D;
using Game.Base;
using UndoSystem;
using Basic.UndoRedo;
using Es;
using UI.Manager;
using Game.Props.PropsBehaviours;

/// <summary>
/// Author:
/// Desc:
/// Date:23-08-01 11:02:35
/// </summary>
public class GamePropListPanel : BasePanel<GamePropListPanel>
{
    [SerializeField]private CButton closeBtn;
    [SerializeField]private TabView bannerTabView;
    [SerializeField]private Transform listContentTF;
    [SerializeField]private GameIconSelectItem iconItemTmpl;

    // 面板参数
    public class PanelArg
    {
        public string PropId;
        public GameGlobalEnum.BannerType BannerType;
    }

    class BannerTabItemConfig
    {
        public GameGlobalEnum.BannerType BannerType;
        public string Name;
    }

    List<BannerTabItemConfig> BannerConfig = new List<BannerTabItemConfig>()
    {
        new BannerTabItemConfig(){BannerType=GameGlobalEnum.BannerType.BasicModel, Name="基础模型"},
        new BannerTabItemConfig(){BannerType=GameGlobalEnum.BannerType.BasicProp, Name="基础道具"},
        new BannerTabItemConfig(){BannerType=GameGlobalEnum.BannerType.LogicProp, Name="逻辑道具"},
    };

    List<Es.GamePropData> gamePropDatas;
    List<GameIconSelectItem> items = new List<GameIconSelectItem>();
    private Dictionary<string, GameIconSelectItem> itemDic = new Dictionary<string, GameIconSelectItem>();
    int curPropSelectIndex = -1; // 默认选中的道具
    string curPropSelectId = null;
    Color NormalColor = Color.white;
    Color SelectColor = new Color32(255, 211, 54, 255);

    public override void OnCreate()
    {
        closeBtn.onClick.AddListener(OnCloseClick);
        // 类型选项
        for (int i = 0; i < BannerConfig.Count; i++)
        {
            var itemCF = BannerConfig[i];
            var bannerItem = bannerTabView.CreateItem($"BannerType_{itemCF.BannerType.ToString()}", itemCF.Name);
            bannerItem.AddValueChangeCallListener(isOn => {
                bannerItem.ItemNameText.color = isOn ? SelectColor : NormalColor;
            });
        }
        bannerTabView.AddItemSelectCallBack(OnBannerItemClick);

        // 道具列表
        gamePropDatas = GamePropDataHelper.GetPropDataList();
        // 若新版文字道具已存在，则从列表中隐藏旧版（旧版数据仍可正常加载，不受影响）
        if (gamePropDatas.Exists(x => x.Id == Game.Config.GameConsts.DTextPropId))
            gamePropDatas = gamePropDatas.FindAll(x => x.Id != Game.Config.GameConsts.OldDTextPropId);
        gamePropDatas.Sort((a,b)=> a.Order - b.Order);
        iconItemTmpl.gameObject.SetActive(false);
        var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.EditorPropSprite);
        var spriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
        for (int i = 0; i < gamePropDatas.Count; i++)
        {

            var index = i;
            var iconConfig = gamePropDatas[i];
            if (!iconConfig.Enable) {
                items.Add(null);
                continue;
            }
            var iconItem = Instantiate(iconItemTmpl, listContentTF);
            iconItem.AddOnSelectListener(()=> OnPropClick(iconConfig));
            iconItem.SetText(iconConfig.ShowName);
            iconItem.SetIcon(spriteAtlas.GetSprite(iconConfig.IconName));
            iconItem.SetSelectWithNoNotify(false);
            var itemKey = $"{(GameGlobalEnum.BannerType)iconConfig.BannerType}_{iconConfig.Id}";
            itemDic.Add(itemKey, iconItem);
        }
    }

    public override void OnShow(params object[] args)
    {
        int bannerTabIndex = 0;
        GameGlobalEnum.BannerType bannerType = GameGlobalEnum.BannerType.BasicModel;

        if (args.Length > 0)
        {
            // 默认选择道具
            PanelArg argData = args[0] as PanelArg;
            if (!string.IsNullOrEmpty(argData.PropId))
            {
                curPropSelectIndex = gamePropDatas.FindIndex(x=>x.Id == argData.PropId);
                curPropSelectId = argData.PropId;
            }

            // 默认选择一个Type栏
            bannerType = argData.BannerType;
            bannerTabIndex = BannerConfig.FindIndex(x=>x.BannerType == bannerType);
        }
        bannerTabView.SetSelect(bannerTabIndex);
        if (string.IsNullOrEmpty(curPropSelectId)) {
            curPropSelectId = gamePropDatas.FirstOrDefault(x => (GameGlobalEnum.BannerType)x.BannerType == bannerType && x.Enable)?.Id;
        }
        var itemKey = $"{bannerType}_{curPropSelectId}";
        if (itemDic.TryGetValue(itemKey, out var item)) {
            item.SetSelectWithNoNotify(true);
        }

    }

    void DoCreateProp(string propId)
    {
        NodeBaseBehaviour nBehav;
        var opReason = GamePropNodeManager.Inst.TryCreateInEdit(propId, out nBehav);
        if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess)
        {
            // 子节点可选择的节点不默认选中
            var mulitBehvSelectable = nBehav is MultiChildBehaviour mBehv && mBehv.ChildSelectable;
            if (!mulitBehvSelectable)
            {
                InputHandlerManager.Inst.SelectEntity(nBehav.entity);
            }
            UndoRecordUtils.AddCreateRecord(nBehav.gameObject);
        } else if (opReason == GameGlobalEnum.NodeOpReason.CreateFail_MaxNum){
            var propConfig = GamePropDataHelper.GetPropDataByID(propId);
            TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
        }
    }

    void OnBannerItemClick(TabItem item, int index)
    {
        var bannerCF = BannerConfig[index];
        foreach (var keyValue in itemDic) {

            keyValue.Value.gameObject.SetActive(keyValue.Key.StartsWith(bannerCF.BannerType.ToString()));
        }
    }

    void OnPropClick(GamePropData config)
    {
        if (!string.IsNullOrEmpty(curPropSelectId)) {
            var itemKey = $"{config.BannerType}_{curPropSelectId}";
            if (itemDic.TryGetValue(itemKey, out var item)) {
                item.SetSelectWithNoNotify(false);
            }
        }
        curPropSelectId = config.Id;

        if (WinConditionManager.Inst.IsWinConditionProp(config.ModelType, out var winConfig))
        {
            WinConditionManager.Inst.SetWinConditionBySelectIcon(winConfig.Id);
        }
        else
        {
            DoCreateProp(config.Id);
        }

        UIManager.Inst.ClosePanel(this);
    }

    private GameIconSelectItem GetSelectItem(string id) {

        var config = gamePropDatas.FirstOrDefault(tmp => tmp.Id == id);
        if (config == null) {
            return null;
        }
        var itemKey = $"{(GameGlobalEnum.BannerType)config.BannerType}_{config.Id}";
        return itemDic.GetValueOrDefault(itemKey);
    }

    void OnCloseClick()
    {
        UIManager.Inst.ClosePanel(this);
    }
}
