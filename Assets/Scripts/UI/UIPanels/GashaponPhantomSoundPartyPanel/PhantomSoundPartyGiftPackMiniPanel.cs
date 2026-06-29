using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PhantomSoundPartyGiftPackMiniPanel : BasePanel<PhantomSoundPartyGiftPackMiniPanel>
{
    [SerializeField] private List<PhantomSoundPartyGiftPackItem> packItemList;
[SerializeField] private Button closeBtn;
[SerializeField] private Text dic;

       public override void OnCreate()
        {
            base.OnCreate();
    
        
        closeBtn.onClick.AddListener(CloseSelf);
        var datas = IAPDataManager.Inst.GetPhantomSoundPartyCoinPackages();
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
            if(res != null && res.zzzPackageList != null)
            {
                if(res.zzzPackageList.Count >= 7)
                {
                    res.zzzPackageList.RemoveAt(0);
                 res.zzzPackageList.RemoveAt(0);
                }
                RefreshData(res.zzzPackageList);
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

    public void SetDic(int count)
    {
        dic.text = $"需要额外<color=#F899DC>{count}绒币</color>进行扭蛋抽取";
    }



}
