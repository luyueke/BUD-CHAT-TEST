using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class CatRechargePanel : BasePanel<CatRechargePanel>
{

    [SerializeField] private Button BackBtn;
    [SerializeField] private Text TipsText;
    [SerializeField] private Text TitleText;
    [SerializeField] private List<CatGiftPackItem> packItemList;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        BackBtn.onClick.AddListener(CloseSelf);
        TipsText.gameObject.SetActive(true);
        if (args.Length > 0)
        {
            TipsText.text = string.Format("需要额外<color=#da3894>{0}喵币</color>进行扭蛋抽取", (int)args[0]);
            TitleText.text = "喵币不足";
        }
        else
        {
            TipsText.text = "";
            TitleText.text = "喵币充值";
        }
        var datas = IAPDataManager.Inst.GetMiaoCoinPackages();
        if (datas != null && datas.Count > 0)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                if(i >= packItemList.Count)
                {
                    return;
                }
                packItemList[i].SetData(datas[i]);
            }
        }
    }
}
