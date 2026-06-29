/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-01 16:42:27
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-01 17:31:26
 * @ Description: 道具设置和选择侧边栏
 */

using UnityEngine;
using UI.BaseWidgets;
using Game.ECS;
using UI.Manager;
using Game.Base;
using Game.Config;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Game.Props.PropsBehaviours;
using UndoSystem;
using Game.Utils;

public class GameEditSettingView : MonoBehaviour
{
    [SerializeField] private CButton settingBtn;
    [SerializeField] private CButton shapeBtn;
    [SerializeField] private CButton basicBtn;
    [SerializeField] private CButton advancedBtn;
    [SerializeField] private CButton aiBuddyBtn;
    [SerializeField] private CButton inventoryBtn;
    [SerializeField] private CButton theatreBtn;

    private void Awake()
    {
        settingBtn.onClick.AddListener(OnGlobalSettingClick);
        shapeBtn.onClick.AddListener(() => { OnPropSelectClick(GameGlobalEnum.BannerType.BasicModel); });
        basicBtn.onClick.AddListener(() => { OnPropSelectClick(GameGlobalEnum.BannerType.BasicProp); });
        advancedBtn.onClick.AddListener(() => { OnPropSelectClick(GameGlobalEnum.BannerType.LogicProp); });
        aiBuddyBtn.onClick.AddListener(() => { OnCreateAIClick(); });
        theatreBtn.onClick.AddListener(OnTheatreClick);
    }

    void OnGlobalSettingClick()
    {
        InputHandlerManager.Inst.UnSelectAll();
        UIManager.Inst.ClosePanel(PanelId.GamePropListPanel);
        UIManager.Inst.OpenPanel(PanelId.GameGlobalSettingPanel, GameGlobalEnum.BannerType.GameSetting);
    }

    void OnPropSelectClick(GameGlobalEnum.BannerType bannerType)
    {
        var gController = GizmoManager.Inst.CurGizmoCtrl;
        var curTarget = gController.GetCurrentTarget();
        string propId = "";
        if (curTarget != null)
        {
            SceneEntity entity = curTarget.GetComponent<NodeBaseBehaviour>()?.entity;
            if (entity != null)
            {
                propId = entity.GetComp<GameObjectComponent>().PropId;
            }
        }

        // 旧版文字道具已下线，选中旧版节点时映射到新版
        if (propId == Game.Config.GameConsts.OldDTextPropId)
            propId = Game.Config.GameConsts.DTextPropId;
        var args = new GamePropListPanel.PanelArg();
        args.BannerType = bannerType;
        args.PropId = propId;
        UIManager.Inst.ClosePanel(PanelId.GameGlobalSettingPanel);
        UIManager.Inst.OpenPanel(PanelId.GamePropListPanel, args);
    }

    void OnCreateAIClick()
    {
        NodeBaseBehaviour nBehav;
        var opReason = GamePropNodeManager.Inst.TryCreateInEdit("20100068", out nBehav);
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
            var propConfig = GamePropDataHelper.GetPropDataByID("20100068");
            TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
        }
    }

    void OnTheatreClick()
    {
        NodeBaseBehaviour nBehav;
        var opReason = GamePropNodeManager.Inst.TryCreateInEdit("20100070", out nBehav);
        if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess)
        {
            var mulitBehvSelectable = nBehav is MultiChildBehaviour mBehv && mBehv.ChildSelectable;
            if (!mulitBehvSelectable)
            {
                InputHandlerManager.Inst.SelectEntity(nBehav.entity);
            }
            UndoRecordUtils.AddCreateRecord(nBehav.gameObject);
        }
        else if (opReason == GameGlobalEnum.NodeOpReason.CreateFail_MaxNum)
        {
            var propConfig = GamePropDataHelper.GetPropDataByID("20100070");
            TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
        }
    }

}