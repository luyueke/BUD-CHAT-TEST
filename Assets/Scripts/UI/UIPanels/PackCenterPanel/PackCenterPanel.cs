using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using Message;

public class PackCenterPanel : BasePanel<PackCenterPanel>
{
    private Transform viewContent;
    private CButton closeBtn;
    
    [HideInInspector]public PaidPackageType PaidPackageType;
    private PaidPackageListItem paidPackageData;

    public static string ViewBasePath = "Assets/Loadable/UI/UIPanel/PackCenterPanel/";
    
    public override void OnCreate()
    {
        base.OnCreate();
        viewContent = GameObjectEx.FindChildByName(transform,"ViewContent");
        closeBtn =GameObjectEx.FindComponentByName<CButton>(transform,"BackBtn");
        closeBtn.onClick.AddListener(CloseSelf);
        
        CreateTestView();

    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args == null) return;
        PaidPackageType = (PaidPackageType)args[0];
        if (args.Length > 1)
        {
            paidPackageData = (PaidPackageListItem)args[1];
        }
        else
        {
            paidPackageData = null;
        }
        
        

        // InitView(PaidPackageType,paidPackageData);
        
        // CreateTestView();
    }

    //todo
    private void CreateTestView()
    {
        // string packName = paidPackageType.ToString();
        string viewPath =
            "Assets/Loadable/UI/RechargePanel/LimitedRechargeGiftPack/LimitedRechargeGiftPack.prefab";
        var view = Loader.Load<GameObject>(viewPath).Instantiate(viewContent);
        var limitedRechargeGiftPack = view.GetComponent<LimitedRechargeGiftPack>();
        limitedRechargeGiftPack.OnInitCreated();
    }

    private PackBaseView CreateView(PaidPackageType paidPackageType)
    {
        string packName = paidPackageType.ToString();
        string viewPath = ViewBasePath + packName + "/" + packName + ".prefab";
        var packViewNode = Loader.Load<GameObject>(viewPath).Instantiate(viewContent);
        PackBaseView packBaseView = packViewNode.GetComponent<PackBaseView>();
        packBaseView.SetMainPanel(this);
        packBaseView.OnCreate(paidPackageType);
        return packBaseView;
    }
    
    
    private void InitView(PaidPackageType paidPackageType,PaidPackageListItem packageListItem)
    {
        var packBaseView = CreateView(paidPackageType);
        if (packageListItem != null)
        {
            packBaseView.OnServerDataUpdate(packageListItem);
        }
        else
        {
            IAPDataManager.Inst.GetProductInfo(res =>
            {
                var paidPackageList = res.paidPackageList;
                if (paidPackageList != null && this != null)
                {
                    PaidPackageListItem packageListItem =
                        paidPackageList.Find(x => x.packageType == (int)paidPackageType);
                    if (packageListItem != null)
                    {
                        packBaseView.OnServerDataUpdate(packageListItem);
                    }
                    else
                    {
                        LoggerUtils.LogError("PackCenterPanel IAPDataManager.Inst.GetProductInfo Error:data is null");
                    }
                    
                }
            });
        }
    }

}