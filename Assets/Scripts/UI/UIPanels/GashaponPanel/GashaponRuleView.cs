using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class GashaponRuleView : MonoBehaviour
{
    [SerializeField] private Button BG;
    [SerializeField] private CButton CloseBtn;
    
    [SerializeField] private Text sText;
    [SerializeField] private Text aText;
    [SerializeField] private Text bText;
    [SerializeField] private GameObject go_Rule_Limit;
    [SerializeField] private GameObject go_s4bRule;
    
    // Start is called before the first frame update
    void Start()
    {
        BG?.onClick.AddListener(OnClose);
        CloseBtn?.onClick.AddListener(OnClose);
    }
    
    public void OnClose()
    {
        gameObject.SetActive(false);
    }

    public void SetData(float probabilityS, float probabilityA, float probabilityB)
    {
        sText.text = $"金色物品：{probabilityS}%";
        aText.text = $"紫色物品：{probabilityA}%";
        bText.text = $"蓝色物品：{probabilityB}%";
    }

    public void SetRuleEnable(string curGashaponId, bool isEnable, CurrencyType currencyType)
    {
        gameObject.SetActive(isEnable);
        SetLimitRuleEnable(currencyType != CurrencyType.GreenCoin 
                           && currencyType != CurrencyType.Coin);

        if (curGashaponId == "lottery.consumeCarnival")
        {
            go_s4bRule.SetActive(true);
        }
    }

    public void SetLimitRuleEnable(bool isEnable)
    {
#if PACKAGE_TYPE_US
        isEnable = false;
#endif
        go_Rule_Limit.SetActive(isEnable);
    }
}
