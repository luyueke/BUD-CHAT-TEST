using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class SockRechargePanel : BasePanel<SockRechargePanel>
{
    [SerializeField] private Button BackBtn;
    [SerializeField] private Text TipsText;
    [SerializeField] private Text TitleText;
    [SerializeField] private List<SockGiftPanelItem> packItemList;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        BackBtn.onClick.AddListener(CloseSelf);
        TipsText.gameObject.SetActive(true);
        if (args.Length > 0)
        {
            TipsText.text = string.Format("需要额外<color=#da3894>{0}袜币</color>进行扭蛋抽取", (int)args[0]);
            TitleText.text = "袜币不足";
        }
        else
        {
            TipsText.text = "";
            TitleText.text = "袜币充值";
        }
        var datas = IAPDataManager.Inst.GetWaCoinPackages();

        if (datas != null && datas.Count > 0)
        {
            for (int i = 2; i < datas.Count; i++)
            {
                if((i - 2) >= packItemList.Count)
                {
                    return;
                }
                packItemList[i - 2].SetData(datas[i]);
            }
        }
    }
}
