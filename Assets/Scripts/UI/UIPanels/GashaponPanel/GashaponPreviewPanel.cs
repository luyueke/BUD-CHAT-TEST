using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


public class GashaponPreviewParam
{
    public string bgPath;
    public string title;
    public GashaponData gashaponData;
    public List<GashaponPonyPreviewItemData> gashaponPonyPreviewItemDataList; //多奖池预览时 要传的奖池列表
    public GashaponInfoRsp gashaponInfoRsp;
    public CurrencyType rewardCurrency;
    public string rulePath;
    public Action onBackCallBack;
    public bool isPonyPreview;
}
public class GashaponPreviewPanel : BasePanel<GashaponPreviewPanel>
{
    [SerializeField] private GashaponPreviewHandleView PreviewHandleView;
    [SerializeField] private CButton BackBtn;

    private Action onBackCallBack;

    public override void OnCreate()
    {
        base.OnCreate();
        BackBtn.onClick.AddListener(OnBackBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            var param = (GashaponPreviewParam)args[0];
            onBackCallBack = param.onBackCallBack;
            PreviewHandleView.IsPonyPreview = param.isPonyPreview;
            if(param.isPonyPreview)
            {
                PreviewHandleView.SetPonyData(param.gashaponData, param.gashaponPonyPreviewItemDataList, param.gashaponInfoRsp);
            }
            else
            {
                PreviewHandleView.SetData(param.gashaponData, param.gashaponInfoRsp);
            }
            SetAccountWidgetType(param.rewardCurrency);
            SetRulePath(param.rulePath);
            SetBg(param.bgPath);
            SetTitle(param.title);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public void SetData(GashaponData gashaponData, GashaponInfoRsp gashaponInfoRsp)
    {
        PreviewHandleView.SetData(gashaponData, gashaponInfoRsp);
    }

    public void Turn2Preview(string bundleId)
    {
        PreviewHandleView.Turn2Preview(bundleId);
    }

    public void SetBg(string path)
    {
        PreviewHandleView.SetBg(path);
    }

    public void SetTitle(string title)
    {
        PreviewHandleView.SetTitle(title);
    }

    public void SetAnimPreviewBtnColor(Color outlineColor, Color selectColor, Color normalColor) {
        PreviewHandleView.SetAnimPreviewBtnColor(outlineColor, selectColor, normalColor);
    }

    public void SetBundleViewBgClolr(string colorStr)
    {
        PreviewHandleView.SetBundleViewBgClolr(colorStr);
    }

    public void SetAccountWidgetType(CurrencyType type)
    {
        PreviewHandleView?.SetAccountWidgetType(type);
    }

    public void SetRulePath(string path)
    {
        PreviewHandleView?.SetRulePath(path);
    }

    // 供虾虾崽扭蛋面板调用：开启顺序加载并在部件加载完后自动播待机
    public void EnableIdleSync() => PreviewHandleView?.EnableIdleSync();

    // 供 huhu/wuwu 等普通套装预览调用：卸下角色身上的特殊皮肤，避免玩家自己装备的云在普通套装预览里冒出来
    public void TakeOffSpecialSkin()
    {
        if (PreviewHandleView != null) PreviewHandleView.TakeOffSpecialSkin();
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
        onBackCallBack?.Invoke();

    }
}
