using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;


public class CatGiftPackPanel : MonoBehaviour
{
    [SerializeField] private List<CatGiftPackItem> packItemList;

    [SerializeField] private CButton ShowBtn;

    public void Awake()
    {
        ShowBtn.onClick.AddListener(OnShowBtnClick);

        var datas = IAPDataManager.Inst.GetMiaoCoinPackages();
        if (datas == null || datas.Count != packItemList.Count)
        {
            RefreshDataFromServer();
            return;
        }
        RefreshData(datas);
    }

    private void RefreshDataFromServer()
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            if(res != null && res.miaoCoinPackage != null)
            {
                RefreshData(res.miaoCoinPackage);
            }
        });
    }

    private void RefreshData(List<MiaoCoinPackageData> packageDatas)
    {
        for (int i = 0; i < packageDatas.Count; i++) 
        {
            packItemList[i].SetData(packageDatas[i]);
        }
    }

    private void OnShowBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CatGiftSkinShowPanel);
    }

}
