using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.Account;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyGiftView : MonoBehaviour
{
    [SerializeField] private CButton backBtn;
    [SerializeField] private AIBuddyGiftAdapter listAdapter;
    [SerializeField] private CButton sendBtn;
    [SerializeField] private CButton gfitBackBtn;
    [SerializeField] private GameObject discountNode;
    [SerializeField] private Text discountText;
    [SerializeField] private Text srcPriceText;
    [SerializeField] private Text priceText;

    private AIBuddyGiftHandler dataHandler;
    private List<GoodsData> giftList;
    private GoodsData curSelectData;
    private CharacterWrap _characterWrap;
    private PlayerAnimationCtrl _playerAnimationCtrl;
    private AIBuddyInfo _aiBuddyInfo;
    private Action<AIBuddyInfo> sendGiftSuccesListener;
    private void Start()
    {
        listAdapter.Init();
        InitListeners();
        dataHandler = AssetsDataManager.GetData<AIBuddyGiftHandler>();
        giftList = dataHandler.GetAIBuddyGiftList();
        giftList.Sort((a, b) => a.OriginalPrice.Value.CompareTo(b.OriginalPrice.Value));
        listAdapter.Data.ResetItems(giftList);
        listAdapter.DefaultSelect();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public void InitData(AIBuddyInfo aiBuddyInfo,CharacterWrap characterWrap)
    {
        _aiBuddyInfo = aiBuddyInfo;
        SetCharacterWarp(characterWrap);
    }

    private void SetCharacterWarp(CharacterWrap characterWrap)
    {
        _characterWrap = characterWrap;
        if (_characterWrap != null)
        {
            _playerAnimationCtrl = _characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>(true);
        }
    }

    public void AddSendGiftSuccessListener(Action<AIBuddyInfo> callback)
    {
        sendGiftSuccesListener += callback;
    }

    private void InitListeners()
    {
        backBtn.onClick.AddListener(OnBackBtnClick);
        gfitBackBtn.onClick.AddListener(OnBackBtnClick);
        listAdapter.AddItemClickListener(OnItemSelect);
        sendBtn.onClick.AddListener(OnSendBtnClick);
    }
    
    protected List<EmoAniConfig> emoAniDataList;
    protected EmoteSubType emoteSubType;
    
    private bool LoopNeedFinish()
    {
        if (emoteSubType == EmoteSubType.SingleLoop)
        {
            _playerAnimationCtrl.ResetEmoteForUICharacter();
             var config =_playerAnimationCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.SingleLoopEnd,
                 () =>
                 {
                     _playerAnimationCtrl.ResetEmoteForUICharacter();
                 });
             _playerAnimationCtrl.SetUIEmoteExpression(_playerAnimationCtrl.CreateExpression(config));
            return true;
        }
        
        return false;
    }

    private void RequestSendGift(GoodsData goodsData)
    {
        var price = GetRealPrice(goodsData);
        var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.GiftTicket);

        if (count < price)
        {
            // 余额不足
            ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
            exchangeCoinPanel.SetData(CurrencyType.GiftTicket,CurrencyType.Gem,price-count);
            return;
        }

        string toUid = _aiBuddyInfo.id;
        string giftId = goodsData.Id;
        int giftType = (int)GiftType.NpcPgc;
        
        JObject req = new JObject()
        {
            ["toUid"] = toUid,
            ["giftId"] = giftId,
            ["giftType"] = giftType,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GiveGift, HttpMethod.POST, JsonConvert.SerializeObject(req),
            (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                OnSendSuccess(goodsData);
                sendGiftSuccesListener?.Invoke(_aiBuddyInfo);
            },
            (error) => { Debug.LogError("Error: " + error); });
    }
    
    private void OnSendSuccess(GoodsData goodsData)
    {
        if (this == null) return;
        string emoteId = goodsData.Id;
        if (_playerAnimationCtrl != null)
        {
            emoAniDataList = Es.DataTables.GetEmoAniConfigList()
                .FindAll((emoAniData) => emoAniData.emoId == emoteId);
            var uiEmoData = DataTables.GetEmoUIConfig(emoteId);
            if (uiEmoData != null)
            {
                emoteSubType = (EmoteSubType) uiEmoData.emoType;
                _playerAnimationCtrl.PlaySingleEmoteForUICharacter(goodsData.Id,null,true,null,LoopNeedFinish);
            }
        }
        
        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
        if (panel != null)
        {
            var intimacySprite = PgcUtils.LoadRewardIcon(BUDRewardType.RewardAIBuddyIntimacyRate, panel.gameObject);
            panel.InitData("送礼成功，亲密度上涨！",intimacySprite);
        }
        
    }


    private int GetRealPrice(GoodsData goodsData)
    {
        float srcPice = goodsData.OriginalPrice.Value;
        float realPrice = srcPice;
        if (goodsData.Discount > 0)
        {
            float discount = (100 - (float)goodsData.Discount)/100;
            realPrice = Mathf.CeilToInt(srcPice * discount);
        }

        return (int)(Mathf.Ceil(realPrice));
    }

    #region UI回调
    private void OnBackBtnClick()
    {
        Hide();
    }

    private void OnSendBtnClick()
    {
        if (curSelectData == null || _aiBuddyInfo == null) return;
        RequestSendGift(curSelectData);
    }
    
    private void OnItemSelect(GoodsData goodsData)
    {
        curSelectData = goodsData;
        discountNode.SetActive(goodsData.Discount > 0);
        srcPriceText.gameObject.SetActive(goodsData.Discount > 0);
        float srcPice = goodsData.OriginalPrice.Value;
        if (goodsData.Discount > 0)
        {
            float discount = (100 - (float)goodsData.Discount)/100;
            discountText.SetLocalText("{0}折生效中",discount * 10);
            srcPriceText.SetText(srcPice.ToString());
            int realPrice = Mathf.CeilToInt(srcPice * discount);
            priceText.SetText(realPrice + "");
        }
        else
        {
            priceText.SetText(srcPice.ToString());
        }
    }

    #endregion
    
}
