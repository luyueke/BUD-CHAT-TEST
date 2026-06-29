using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PlantWaterPanel : BasePanel<PlantWaterPanel>
{
    public Image Icon;

    public Image Icon2;

    public Image Icon11;

    public Image Icon22;

    public Text Name;

    public Button CloseBtn;

    public Button BuyBtn;

    public Button ReduceBtn;

    public Button AddBtn;

    public Text BuyTxt;

    public Text FillTxt;

    public Image Fill;

    int level;

    bool isWater;

    string uid;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        BuyBtn.onClick.AddListener(OnBuy);
        ReduceBtn.onClick.AddListener(OnReduce);
        AddBtn.onClick.AddListener(OnAdd);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        uid = (string)args[0];
        isWater = (bool)args[1];
        if (isWater)
        {
            Icon.gameObject.SetActive(true);
            Icon2.gameObject.SetActive(false);
            Icon11.gameObject.SetActive(true);
            Icon22.gameObject.SetActive(false);
            Name.text = "水滴";
        }
        else
        {
            Icon.gameObject.SetActive(false);
            Icon2.gameObject.SetActive(true);
            Icon11.gameObject.SetActive(false);
            Icon22.gameObject.SetActive(true);
            Name.text = "肥料";
        }
        level = 1;
        UpdateList();
    }

    void UpdateList()
    {
        var c = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater);
        if (!isWater)
        {
            c = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingFertilizer);
        }

        if (c > 0)
        {
            Fill.fillAmount = (float)level / c;
        }

        FillTxt.text = $"数量<color=#FFD441>{level}</color>";

        BuyTxt.text = $"{level}";
    }

    void OnBuy()
    {
        var energyCoinCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater);
        if (!isWater)
        {
            energyCoinCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingFertilizer);
        }
        var count = level;
        if (count > energyCoinCount)
        {
            TipPanel.ShowToast("数量不足");
            return;
        }
        PlantTreeSystem.Inst.RequestWater(uid, level, isWater ? 0 : 1, () => {
            CloseSelf();
        });
    }
    void OnAdd()
    {
        var c = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater);
        if (!isWater)
        {
            c = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingFertilizer);
        }
        if (level >= c)
        {
            return;
        }
        level++;
        UpdateList();
    }

    void OnReduce()
    {
        if (level <= 1)
        {
            return;
        }
        level--;
        UpdateList();
    }
}