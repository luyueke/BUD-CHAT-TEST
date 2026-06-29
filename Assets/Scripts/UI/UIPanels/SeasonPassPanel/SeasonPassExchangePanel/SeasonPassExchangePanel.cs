using Fsbm.Runtime;
using Game.Store;
using GameData.Gashapon;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassExchangePanel : BasePanel<SeasonPassExchangePanel>
{
    [SerializeField] private GashaponCharacterPreview characterPreview;
    [SerializeField] private SeasonPassExchangeItem itemPrefab;
    [SerializeField] private CButton BackBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private CButton HelpBtn;

    [SerializeField] private AccountWidget AccountWidget;
    [SerializeField] private Text RewardName;

    [SerializeField] private LoadingButton BuyBtn;
    [SerializeField] private GameObject BuyGray;
    [SerializeField] private Text BuyText;

    [SerializeField] private CButton PassBtn;
    [SerializeField] private GameObject Pass_Lock;
    [SerializeField] private GameObject Pass_UnLock;

    [SerializeField] private CButton LevelBtn;
    [SerializeField] private GameObject Level_Lock;
    [SerializeField] private GameObject Level_UnLock;

    [SerializeField] private CButton TryOnBtn;
    [SerializeField] private GameObject RewardGroup;
    [SerializeField] private ScrollList RewardList;
    [SerializeField] private Text RewardTitle;

    [SerializeField] private GameObject Title;
    [SerializeField] private GameObject Title_s15;
    [SerializeField] private GameObject Image_s14;
    [SerializeField] private GameObject Image_s15;
    [SerializeField] private GameObject BGSizeToFit;
    [SerializeField] private GameObject BGSizeToFit_s15;
    private List<SeasonPassExchangeItem> items = new List<SeasonPassExchangeItem>();

    SeasonExchangeConfig _info;
    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBackBtnClick);
        TryOnBtn?.onClick.AddListener(OnTryOnBtnClick);
        BuyBtn?.onClick.AddListener(OnBuyBtnClick);

        PassBtn.onClick.AddListener(OnPass);
        LevelBtn.onClick.AddListener(OnLevel);

        HelpBtn.onClick.AddListener(OnHelpBtn);


        MessageHelper.AddListener(MessageName.SeasonPassDataUpdate, UpdateLock);
        MessageHelper.AddListener(MessageName.SeasonExchangeGiftUpdate, UpdateItems);

        SeasonPassExchangeSystem.Inst.ReqProductInfo();
        switch (SeasonPassDataManager.Inst.CurrentSeasonPassType)
        {
            case SeasonPassType.S14SeasonPass:
                Title.SetActive(true);
                Title_s15.SetActive(false);
                Image_s14.SetActive(true);
                Image_s15.SetActive(false);
                BGSizeToFit.SetActive(true);
                BGSizeToFit_s15.SetActive(false);
                break;
            case SeasonPassType.S15SeasonPass:
                Title.SetActive(false);
                Title_s15.SetActive(true);
                Image_s14.SetActive(false);
                Image_s15.SetActive(true);
                BGSizeToFit.SetActive(false);
                BGSizeToFit_s15.SetActive(true);
                break;
            default:
                Title.SetActive(false);
                Title_s15.SetActive(true);
                Image_s14.SetActive(false);
                Image_s15.SetActive(true);
                BGSizeToFit.SetActive(false);
                BGSizeToFit_s15.SetActive(true);
                break;
        }
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.SeasonPassDataUpdate, UpdateLock);
        MessageHelper.RemoveListener(MessageName.SeasonExchangeGiftUpdate, UpdateItems);
        base.OnDestroy();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        PassBtn.gameObject.SetActive(false);
        LevelBtn.gameObject.SetActive(false);
        BuyBtn.gameObject.SetActive(false);
        RewardName.text = "";
        SetData();

        AccountWidget?.ChangeType(CurrencyType.Gem);
    }

    void OnHelpBtn() {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, "Assets/Loadable/UI/UIPanel/SeasonPassExchange/SeasonPassExchangeRule.json");
    }

    public void UpdateLock()
    {
        if (_info.Gift)
        {
            var data = SeasonPassDataManager.Inst.GetCurSeasonData();
            if (data != null)
            {
                var pass = data.isPaid == 1;
                Pass_Lock.gameObject.SetActive(!pass);
                Pass_UnLock.gameObject.SetActive(pass);
                if (data.progressInfo != null)
                {
                    var level = data.progressInfo.currentTier >= 25;
                    Level_Lock.gameObject.SetActive(!level);
                    Level_UnLock.gameObject.SetActive(level);
                }
            }

            if (Pass_Lock.gameObject.activeSelf || Level_Lock.gameObject.activeSelf)
            {
                PassBtn.gameObject.SetActive(true);
                LevelBtn.gameObject.SetActive(true);
                BuyBtn.gameObject.SetActive(false);
            }
            else
            {
                PassBtn.gameObject.SetActive(false);
                LevelBtn.gameObject.SetActive(false);
                BuyBtn.gameObject.SetActive(true);
            }
        }
        else
        {
            PassBtn.gameObject.SetActive(false);
            LevelBtn.gameObject.SetActive(false);
            BuyBtn.gameObject.SetActive(true);
        }
        BuyGray.gameObject.SetActive(SeasonPassExchangeSystem.Inst.GetTime(_info.ExchangeId) >= _info.Count);
    }

    public void UpdateItems()
    {
        foreach (var item in items)
        {
            item.UpdateTimes();
        }
    }

    public void SetData()
    {
        var rewardList = SeasonPassExchangeSystem.Inst.rewardList;
        if (rewardList != null && rewardList.Count > 0)
        {
            for (int i = 0; i < rewardList.Count; i++)
            {
                SeasonExchangeConfig priceData = rewardList[i];
                SeasonPassExchangeItem itemScript = Instantiate(itemPrefab, Content);
                itemScript.Init(priceData, OnItemClick);
                items.Add(itemScript);
            }
        }

        Invoke("Def", 0.2f);
    }

    void Def() {
        characterPreview.StopAllEmoteSound();
        if (items.Count > 0)
        {
            items[0].OnClickItem();
        }
    }

    private void OnItemClick(SeasonPassExchangeItem itemNode, SeasonExchangeConfig info)
    {
        _info = info;
        BuyText.text = _info.Price.ToString();
        RewardTitle.text = _info.Name;
        RewardList.datas = _info.RewardList;
        RewardGroup.gameObject.SetActive(_info.RewardList.Count != 1);
        UpdateLock();
        if (info.Gift)
        {
            var ls = new List<string>();
            for (int i = 0; i < info.RewardList.Count; i++)
            {
                if (i > 0 && info.RewardList[i].RewardType == BUDRewardType.RewardPgcResource)
                {
                    ls.Add(info.RewardList[i].PgcID.ToString());
                }
            }
            ls.Add(info.RewardList[0].PgcID.ToString());
            characterPreview.StartPreview(ls, null);
        }
        else
        {
            characterPreview.ShowCurrencyIcon(info.RewardList.First().RewardType, info.RewardList.First().Count);
        }
    }


    private void OnBackBtnClick()
    {
        CloseSelf();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void OnBuyBtnClick()
    {
        if (BuyGray.gameObject.activeSelf)
        {
            return;
        }
        UIManager.Inst.OpenPanel(PanelId.SeasonPassGiftPanel, _info);
    }

    void OnPass()
    {
        if (Pass_Lock.gameObject.activeSelf)
        {
            UIManager.Inst.OpenPanel(PanelId.SeasonPurchaseView);
        }
    }

    void OnLevel()
    {
        if (Level_Lock.gameObject.activeSelf)
        {
            UIManager.Inst.OpenPanel(PanelId.SeasonPassUpPanel);
        }
    }

    private void OnTryOnBtnClick()
    {

    }

    #region 网络请求

    private void RequestExchange(string gashaId, int exchangeId, Action<ExchangeDataRsp> onSuccess, Action<string> onFail = null)
    {
        JObject jObject = new JObject()
        {
            ["lotteryId"] = gashaId,
            ["productId"] = exchangeId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GashaponReddem, HttpMethod.POST, JsonConvert.SerializeObject(jObject), (content) =>
        {
            ExchangeDataRsp response = JsonConvert.DeserializeObject<ExchangeDataRsp>(content);
            onSuccess?.Invoke(response);
        },
            (error) =>
            {
                HttpResponseRawData rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(error);
                if (rsp != null && !string.IsNullOrEmpty(rsp.rmsg))
                {
                    TipPanel.ShowToast(rsp.rmsg);
                    onFail?.Invoke(rsp.rmsg);
                }
                else
                {
                    onFail?.Invoke("");
                }
            });
    }

    private void OnExchangeSuccess(ExchangeDataRsp dataRsp)
    {
        AccountDataManager.Inst.RequestAvatarFrameOrChat();
        if (dataRsp != null)
        {
            List<CommonRewardData> redeemClaimRewardsItems = dataRsp.rewardList;
            var pairList = dataRsp.backpackData?.pairList;
            if ((redeemClaimRewardsItems == null || redeemClaimRewardsItems.Count <= 0) && (pairList == null || pairList.Count <= 0))
            {
                return;
            }
            GashaponDataManager.Inst.ShowGashaponReward(this.gameObject, redeemClaimRewardsItems, pairList, null);
        }

        //foreach (var itemNode in items)
        //{
        //    var exchangeInfo = itemNode.GetBindData();
        //    if (exchangeInfo != null)
        //    {
        //        bool isOwned = GashaponUtils.IsOwnedReward(exchangeInfo);
        //        if (isOwned)
        //        {
        //            itemNode.SetOwnedUI(true);
        //            if (curSelectItem == itemNode)
        //            {
        //                BuyBtn.gameObject.SetActive(false);
        //            }
        //        }
        //    }
        //}

    }


    #endregion



}
