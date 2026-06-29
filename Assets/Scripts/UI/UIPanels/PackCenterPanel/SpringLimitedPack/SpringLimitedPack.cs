using System.Collections.Generic;
using Message;
using UnityEngine;


public class SpringLimitedPack : MonoBehaviour
{
    [SerializeField] private Transform BG;
    [SerializeField] private NewYearFireworkPack newYearFireworkPack;
    [SerializeField] private GoldenSnakePack goldenSnakePack;
    [SerializeField] private FlySwordPack flySwordPack;
    [SerializeField] private GameObject contentView;


    private string spriteatlasPath = "";
    private List<NewYearLimitedPackageListItem> newYearLimitedPackageList;

    private void Start()
    {
       //InitBgView();
       GetDataByHttp();
       MessageHelper.AddListener(MessageName.OnPurchaseNewYearPackageSuccess, GetDataByHttp);
    }

    public void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnPurchaseNewYearPackageSuccess, GetDataByHttp);
    }

    private void GetDataByHttp()
    {
        contentView.SetActive(false);
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            contentView.SetActive(true);
            this.newYearLimitedPackageList = res.newYearLimitedPackageList;
            if (newYearLimitedPackageList == null)
            {
                LoggerUtils.LogError("SpringLimitedPack newYearLimitedPackageList is NUll");
                return;
            }
            //LoggerUtils.LogError("SpringLimitedPack newYearLimitedPackageList ="+ Newtonsoft.Json.JsonConvert.SerializeObject(newYearLimitedPackageList));
            foreach (var newYearLimitedPackageItem in newYearLimitedPackageList)
            {
                switch (newYearLimitedPackageItem.productId)
                {
                    case "android_newYearLimitedPack1":
                    case "ios_newYearLimitedPack1":
                    {
                        newYearFireworkPack.SetData(newYearLimitedPackageItem, isSuccess =>
                        {
                            if (isSuccess)
                            {
                                Refresh();
                            }
                        });
                    }
                        break;
                    case "android_newYearLimitedDancingPack":
                    case "ios_newYearLimitedDancingPack":
                    {
                        goldenSnakePack.SetData(newYearLimitedPackageItem, isSuccess =>
                        {
                            if (isSuccess)
                            {
                                Refresh();
                            }
                        });
                    }
                        break;
                    case "android_newYearLimitedPack3":
                    case "ios_newYearLimitedPack3":
                    {
                        flySwordPack.SetData(newYearLimitedPackageItem, isSuccess =>
                        {
                            if (isSuccess)
                            {
                                Refresh();
                            }
                        });
                    }
                        break;
                }
            }
        });
    }

    private void Refresh()
    {
        GetDataByHttp();
    }
    
    

    private void InitBgView()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
        {
            "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
        });
        item.gameObject.SetActive(true);
    }

    
}