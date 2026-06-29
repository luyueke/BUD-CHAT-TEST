using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;


public class AIBuyResourcePanel : BasePanel<AIBuyResourcePanel>
{
    [SerializeField] internal Transform itemParent;
    [SerializeField] internal AIBuyResourceItem itemPrefab;
    [SerializeField] internal AIBuyResourceItem selectItem;
    [SerializeField] internal GameObject selectRoot;
    [SerializeField] internal GameObject buyRoot;
    [SerializeField] internal Button buyButton;

    [SerializeField] internal Button closeButtonl;

  

    public static AIResourcePayListData ocPayData;

    private AIResourcePayData curData;
    private Action<int> onBuySuccess;
    private Action updateFree;
    private List<AIBuyResourceItem> buyItems;
    private AIResType _aiType = AIResType.AIYandere;

    public override void OnCreate()
    {
        base.OnCreate();
        closeButtonl.onClick.AddListener(CloseSelf);
        buyButton.onClick.AddListener(OnConfirmBuyOc);
    }

    public override void OnShow(params object[] args)
    {
        if (args.Length >0)
        {
            _aiType = (AIResType) args[0];
            InitBuyItems();
            GetPay();
            if (ocPayData != null)
            {
                UpdateView(ocPayData);
            }
        }
    }

    public void SetOnBuySuccessAct(Action<int> act,Action freeCard)
    {
        onBuySuccess = act;
        updateFree = freeCard;
    }

    private void GetPay()
    {
        var param = new JObject()
        {
            ["type"] = (int) _aiType
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.aiLimitProducts, HttpMethod.GET, JsonConvert.SerializeObject(param),
            onReceive: arg0 =>
            {
                var data = JsonConvert.DeserializeObject<AIResourcePayListData>(arg0);
                ocPayData = data;
                UpdateView(data);
            },
            onFail: arg0 => { }
        );
    }

    private void InitBuyItems()
    {
        buyItems = new List<AIBuyResourceItem>();
        for (int i = 0; i <3; i++)
        {
            var item = GameObject.Instantiate(itemPrefab, itemParent);
            item.OnBuyOcAction = OnBuyCardClick;
            item.gameObject.SetActive(false);
            buyItems.Add(item);
        }
        itemPrefab.gameObject.SetActive(false);
    }

    public void UpdateView(AIResourcePayListData listData)
    {
        var tempList = listData.list;
        if (tempList.Count != 3)
        {
            LoggerUtils.LogError("服务端数据异常");
            return;
        }
        for (int i = 0; i < tempList.Count; i++)
        {
            var item = buyItems[i];
            var data = tempList[i];
            item.gameObject.SetActive(true);
            item.SetData(data, _aiType);
        }
    }

    private void OnBuyCardClick(AIResourcePayData payData)
    {
        BuyOc(payData);
    }

    private void BuyOc(AIResourcePayData data)
    {
        curData = data;
        selectRoot.gameObject.SetActive(false);
        buyRoot.gameObject.SetActive(true);
        selectItem.SetData(data,_aiType);
        selectItem.DisableButton();
    }

    private void OnConfirmBuyOc()
    {
        int aiProductType = (int)_aiType;
        AssetsDataManager.BuyAIRes(curData.productId, aiProductType, CurrencyType.Gem, curData.gem, (success, result, needCount) =>
        {
            if (success)
            {
                var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                panel?.InitData(_aiType,curData);
                onBuySuccess?.Invoke(result.curAILimitProductCnt);
                CloseSelf();
            }
            else
            {
                LoggerUtils.LogError("购买AI资源失败",curData.productId);
            }
        },UpdateFreeByNewDay,NeedMoney);
    }

    private void UpdateFreeByNewDay()
    {
        string content = _aiType == AIResType.AIYandere ? "免费次数已刷新，返回继续游玩" : "免费次数已刷新，返回继续聊天";
        TipPanel.ShowToast(content);
        updateFree?.Invoke();
        CloseSelf();
    }

    private void NeedMoney(int needValue)
    {
        CloseSelf();
        UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needValue);
    }
}

public class AIResourcePayListData
{
    public List<AIResourcePayData> list;
}
[Serializable]
public class AIResourcePayData
{
    public string productId;
    public int amount;
    public int discount;
    public int gem;
}

