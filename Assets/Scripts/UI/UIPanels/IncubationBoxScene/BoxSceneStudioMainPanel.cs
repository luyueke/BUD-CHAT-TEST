using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// Box 场景工作室主面板，展示用户的草稿和已发布列表。
    /// 通过子面板 BoxSceneStudioInfoPanel 管理数据加载和条目渲染，
    /// 支持草稿和已发布两个 Tab 的切换。
    /// 使用方式：
    ///   UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel)
    ///   UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel, BoxSceneStudioType.Published)
    /// </summary>
    public class BoxSceneStudioMainPanel : BasePanel<BoxSceneStudioMainPanel>
    {
        /// <summary>背景装饰容器</summary>
        [SerializeField] private Transform _trans_Bg;

        /// <summary>顶部 Tab 导航栏，包含返回按钮和 Tab 切换</summary>
        [SerializeField] private NavigationBarTabs navigationBarTabs;

        /// <summary>列表子面板，通过 StudioType 动态切换草稿/已发布两种模式</summary>
        [SerializeField] private BoxSceneStudioInfoPanel InfoPanel;

        /// <summary>右侧详情视图，展示选中条目的操作按钮</summary>
        [SerializeField] private BoxSceneDetailView DetailView;

        /// <summary>当前选中的 Tab 类型</summary>
        private StudioSubType _curSelectType;

        /// <summary>当前面板打开时指定的默认类型（草稿 / 已发布）</summary>
        private BoxSceneStudioType _boxSceneStudioType;

        /// <summary>Tab 配置项，包含显示名称和对应的 StudioSubType</summary>
        public class AnimStudioConfig
        {
            public string name;
            public StudioSubType studioType;
        }

        /// <summary>Tab 列表配置：草稿列表（index 0）/ 已发布列表（index 1）</summary>
        private List<AnimStudioConfig> rtConfig = new()
        {
            new() { name = "草稿箱", studioType = StudioSubType.Drafts },
            new() { name = "已发布", studioType = StudioSubType.Published },
        };

        /// <summary>
        /// 面板创建时调用，订阅 Cabin 草稿/发布列表变更消息，初始化背景装饰。
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();

            // 订阅 cabin 草稿/发布列表变更消息
            MessageHelper.AddListener(MessageName.OnCabinDraftListChange, OnDraftListChanged);
            MessageHelper.AddListener(MessageName.OnCabinPublishListChange, OnPublishedListChanged);

            DetailView.InitUI();
            //InitBG();
        }

        /// <summary>
        /// 面板显示时调用。
        /// 可通过 args[0] 传入 BoxSceneStudioType 指定默认打开草稿或已发布 Tab。
        /// </summary>
        /// <param name="args">args[0]（可选）：BoxSceneStudioType，默认为 Draft</param>
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (args != null && args.Length > 0)
            {
                _boxSceneStudioType = (BoxSceneStudioType)args[0];
            }
            else
            {
                // 默认打开草稿列表
                _boxSceneStudioType = BoxSceneStudioType.Draft;
            }

            // Draft → index 0（草稿列表），Published → index 1（已发布列表）
            int defaultTabIndex = _boxSceneStudioType == BoxSceneStudioType.Published ? 1 : 0;

            InitBottomPanel(defaultTabIndex);
        }

        /// <summary>
        /// 面板销毁时调用，取消所有消息监听。
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            MessageHelper.RemoveListener(MessageName.OnCabinDraftListChange, OnDraftListChanged);
            MessageHelper.RemoveListener(MessageName.OnCabinPublishListChange, OnPublishedListChanged);
        }

        /// <summary>
        /// 初始化底部子面板，配置点击回调，以及 Tab 导航栏的事件绑定。
        /// StudioType 在 OnSelectView 切换 Tab 时动态赋值，无需在此处预设。
        /// </summary>
        /// <param name="defaultTabIndex">默认选中的 Tab 索引（0=草稿，1=已发布）</param>
        private void InitBottomPanel(int defaultTabIndex = 0)
        {
            // 统一点击回调，内部通过 _curSelectType 区分草稿/已发布逻辑
            InfoPanel.SetItemOnClickAct(OnItemClick);

            navigationBarTabs.AddBackBtnClickListener(CloseSelf);

            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.studioType.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.AddItemSelectCallBack(RTClick);
            navigationBarTabs.SetSelect(defaultTabIndex);
        }

        /// <summary>
        /// 初始化背景装饰，加载通用背景预制体和图标。
        /// </summary>
        private void InitBG()
        {
            if (_trans_Bg == null)
                return;

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_trans_Bg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "animStudio_icon1", "animStudio_icon2", "animStudio_icon3"
            });
            item.gameObject.SetActive(true);
        }

        /// <summary>
        /// Tab 被点击时的回调，根据索引切换当前视图。
        /// </summary>
        /// <param name="item">被点击的 Tab 项</param>
        /// <param name="index">Tab 索引</param>
        private void RTClick(TabItem item, int index)
        {
            var data = rtConfig[index];
            OnSelectView(data.studioType);
        }

        /// <summary>
        /// 切换当前视图，更新 InfoPanel 的列表类型并触发数据加载。
        /// </summary>
        /// <param name="studioType">目标视图类型（Drafts=草稿，Published=已发布）</param>
        public void OnSelectView(StudioSubType studioType)
        {
            _curSelectType = studioType;

            // 切换前更新 StudioType，InfoPanel.OnSelectView 内部会据此加载对应数据
            InfoPanel.SetStudioType(studioType);
            InfoPanel.OnSelectView();
        }

        /// <summary>
        /// 收到草稿列表变更消息时的处理：若当前在草稿 Tab 则刷新，否则切换到草稿 Tab。
        /// </summary>
        private void OnDraftListChanged()
        {
            if (_curSelectType == StudioSubType.Drafts)
            {
                OnSelectView(StudioSubType.Drafts);
            }
            else
            {
                // 切换到草稿 Tab（index=0）
                navigationBarTabs.SetSelect(0);
            }
        }

        /// <summary>
        /// 收到已发布列表变更消息时的处理：若当前在已发布 Tab 则刷新，否则切换到已发布 Tab。
        /// </summary>
        private void OnPublishedListChanged()
        {
            if (_curSelectType == StudioSubType.Published)
            {
                OnSelectView(StudioSubType.Published);
            }
            else
            {
                // 切换到已发布 Tab（index=1）
                navigationBarTabs.SetSelect(1);
            }
        }

        /// <summary>
        /// 条目被点击时的统一回调，根据当前 Tab 类型（_curSelectType）区分处理逻辑：
        /// - 草稿模式：id 为空表示"去创作"入口，打开 Box 场景编辑器；否则显示右侧详情视图。
        /// - 已发布模式：打开 AssetDetailPanel 展示公开详情页（与 AvatarStudioPublishView 保持一致）。
        /// </summary>
        /// <param name="item">被点击的数据包（统一封装为 CharacterBoxPublishItem）</param>
        private void OnItemClick(CharacterBoxPublishItem item)
        {
            // 草稿模式下，id 为空表示"去创作"入口
            if (_curSelectType == StudioSubType.Drafts && string.IsNullOrEmpty(item?.characterBoxInfo?.id))
            {
                // 打开编辑器新建场景，参数与 AICompanionPanel 保持一致：UGCBoxSceneData + BoxSceneInfo（使用默认模板）
                // VIP 用户使用 64×64 格子，非 VIP 使用 32×32 格子（参考 UGCResourceEditPanel 链路）
                UIManager.Inst.OpenPanel(PanelId.UGCBoxSceneEditorPanel,
                    new UGCBoxSceneData { id = "ugcBreedingFarm_1" },
                    new BoxSceneInfo
                    {
                        canvasType = VipDataManager.Inst.isVip ? (int)CanvasType.Canvas_64 : (int)CanvasType.Canvas_32
                    });
                return;
            }

            if (_curSelectType == StudioSubType.Published)
            {
                // 已发布条目打开公开详情页，参考 AvatarStudioPublishView.OnStudioItemClick
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CharacterBox, item.characterBoxInfo.id);
                return;
            }

            DetailView.UpdateInfo(item.characterBoxInfo, _curSelectType);
        }

        public override void CloseSelf()
        {
            base.CloseSelf();
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
            //UIManager.Inst.BackToLastWindow();
        }
    }

    /// <summary>
    /// Box 场景工作室面板的打开类型，决定默认显示哪个 Tab。
    /// </summary>
    public enum BoxSceneStudioType
    {
        /// <summary>默认打开草稿列表</summary>
        Draft = 1,

        /// <summary>默认打开已发布列表</summary>
        Published = 2,
    }
}
