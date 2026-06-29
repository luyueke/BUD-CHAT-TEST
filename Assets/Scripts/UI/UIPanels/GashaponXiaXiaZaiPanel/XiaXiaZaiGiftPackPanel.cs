using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class XiaXiaZaiGiftPackPanel : MonoBehaviour
{
    [SerializeField] private List<XiaXiaZaiGiftPackItem> packItemList;

    [SerializeField] private CButton ShowBtn;

    public void Awake()
    {
        ShowBtn.onClick.AddListener(OnShowBtnClick);

        var datas = IAPDataManager.Inst.GetXiaXiaZaiCoinPackages();
        if (datas == null || datas.Count != packItemList.Count)
        {
            RefreshDataFromServer();
            return;
        }
        RefreshData(datas);
    }

    public void RefreshDataFromServer()
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            if(res != null && res.babyShrimpPackageList != null)
            {
                RefreshData(res.babyShrimpPackageList);
            }
        });
    }

    private void RefreshData(List<Y2KSkateboardingPackage> packageDatas)
    {
        for (int i = 0; i < packageDatas.Count; i++) 
        {
            packItemList[i].SetData(packageDatas[i]);
        }
    }

    private void OnShowBtnClick()
    {
        UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.XiaXiaZai);
    }


}
