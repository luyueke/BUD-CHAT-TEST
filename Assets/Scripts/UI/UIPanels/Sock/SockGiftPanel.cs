using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;


public class SockGiftPanel : MonoBehaviour
{
    [SerializeField] private List<SockGiftPanelItem> packItemList;

    [SerializeField] private CButton ShowBtn;

    public void Awake()
    {
        ShowBtn.onClick.AddListener(OnShowBtnClick);

        var datas = IAPDataManager.Inst.GetWaCoinPackages();
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
            if (res != null && res.waCoinPackage != null)
            {
                RefreshData(res.waCoinPackage);
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
        UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.Sock);
    }

}
