/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-02-02 15:26:54
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-05-11 18:12:52
 * @ Description: 余额展示部件
 */

using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using Message;
using NetCoreServer;
using Newtonsoft.Json;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using GameData.Manager;
using UI.UIPanels.GashaponPanel;

public class AccountWidget : MonoBehaviour
{
    private Image iconImg;
    private Button tipsBtn;
    private Text numTxt; // 余额
    private Button jumpBtn; // 跳转按钮
    private Image addIcon;//加号图片
    public CurrencyType type; // 账户类型
    public bool isGameHall; //是否是gamehall 特殊处理

    public Action DidClickAction;

    private WindowId _overrideWindowId = WindowId.None;

    public void SetOverrideWindowId(WindowId windowId)
    {
        _overrideWindowId = windowId;
    }

    private const float defaultNumPos = -76f;
    private const float centerNumPos = -51f;

    private bool isInit = false;

    private void Awake()
    {
        MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);

        DeclareUI();
        isInit = true;
    }

    //供外部更改添加按钮监听事件
    public void JumpBtnAddListen(UnityAction action)
    {
        jumpBtn.onClick.RemoveAllListeners();
        jumpBtn.onClick.AddListener(action);
    }

    public void ChangeType(CurrencyType newType)
    {
        this.type = newType;
        InitUI();
        //
        if (this.type == CurrencyType.KoiGachaCoin || this.type == CurrencyType.CollectionTicket ||
            this.type == CurrencyType.Crystal ||
             this.type == CurrencyType.CrystalShards || this.type == CurrencyType.MusicNoteCrystal
             || this.type == CurrencyType.MusicNoteCrystalShards)
        {
            jumpBtn.gameObject.SetActive(false);
            addIcon?.gameObject.SetActive(false);
        }



        UpdateAccountNum(type);
    }

    public void ShowAddBtn(bool value)
    {
        addIcon?.gameObject.SetActive(value);
        jumpBtn?.gameObject.SetActive(value);
        float pos = value ? defaultNumPos : centerNumPos;
        numTxt.rectTransform.anchoredPosition = new Vector3(pos, 0, 0);
    }

    // 声明一下UI
    private void DeclareUI()
    {
        Transform parent = GameObjectEx.FindChildByName(transform, "Layout");
        iconImg = GameObjectEx.FindChildByName(parent, "Icon").GetComponent<Image>();
        tipsBtn = GameObjectEx.FindChildByName(parent, "TipsBtn").GetComponent<Button>();
        numTxt = GameObjectEx.FindChildByName(parent, "Num").GetComponent<Text>();
        jumpBtn = GameObjectEx.FindChildByName(parent, "BtnAccount").GetComponent<Button>();
        addIcon = GameObjectEx.FindChildByName(parent, "Add").GetComponent<Image>();
        jumpBtn.onClick.AddListener(OnJumpClick);
        tipsBtn.onClick.AddListener(OnTipsClick);
    }

    // 初始化一下UI
    private void InitUI()
    {
        if (!isInit)
        {
            DeclareUI();
        }

        if (!isGameHall) {
            string iconName = PgcUtils.GetScopeCurrencyIconName(type);
            if (string.IsNullOrEmpty(iconName)) {
                if (!IconConfig.TryGetValue(type, out iconName)) {
                    PgcUtils.CurrencyIconPath.TryGetValue(type, out iconName);
                }
            }
            if (!string.IsNullOrEmpty(iconName))
            {
                var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
                iconImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject);
            }
            else
            {
                LoggerUtils.Log($"AccountWidget Type={type} Not Config.");
            }
        }

        if (type == CurrencyType.ZZZPhantomCrystal || type == CurrencyType.ZZZPhantomCrystalShards || type == CurrencyType.LuckyTicket || type == CurrencyType.ChristmasTicket || type == CurrencyType.CoinTicket || type == CurrencyType.PopularityTicket || type == CurrencyType.GreenCoin || type == CurrencyType.SweetieTicket || type == CurrencyType.PurpleDreamTicket
            || type == CurrencyType.CollectionTicket || type == CurrencyType.SockTailTicket || type == CurrencyType.SockYunyunTicket || type == CurrencyType.Crystal || type == CurrencyType.CrystalShards)
        {
            ShowAddBtn(false);
        }
    }

    private void Start()
    {
        InitUI();
        UpdateAccountNum(type);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
    }

    private void OnPlayerInfoAccountChange(CurrencyType bType)
    {
        UpdateAccountNum(bType);
    }

    private void UpdateAccountNum(CurrencyType bType)
    {
        Debug.Log("响应到了刷新，type是:" + bType.ToString());
        if (this.type == bType)
        {
            int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(bType);
            if (bType == CurrencyType.CelebrationCoin)
            {
                Debug.Log("刷新了数值，数值是:" + TokenDataManager.Inst.Data.GetToken(CurrencyType.CelebrationCoin).ToString());
                numTxt.text = TokenDataManager.Inst.Data.GetToken(CurrencyType.CelebrationCoin).ToString();
                return;
            }
            if (num < 1000000)
            {
                numTxt.text = num.ToString();
            }
            else
            {
                numTxt.text = "999999";
            }
            
        }

    }

    // 跳转
    private void OnJumpClick()
    {
        DidClickAction?.Invoke();
        if (type == CurrencyType.Gem)
        {
            var panel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, RechargeId.GemPack);
            panel.OnTabClick(RechargeId.GemPack);
            //UIManager.Inst.OpenPanel(PanelId.RechargePanel, RechargeId.GemPack);
        }
        else if(type == CurrencyType.MiaoCoin)
        {
            UIManager.Inst.OpenPanel(PanelId.CatRechargePanel);
        }
        else if (type == CurrencyType.SockCoin)
        {
            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.WaCoinPack);
        }
        else if (type == CurrencyType.AISeasonCoin || type == CurrencyType.AICore)
        {
            UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel, SeasonPassType.S9AbandonedHospitalSeasonPass);
        }
        else if (type == CurrencyType.ZZZCoin)
        {
              UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.zzzGiftPack);
        }
        else if (type != CurrencyType.Free && type != CurrencyType.None)
        {
            ExchangeCoinPanel exchangeCoinPanel = _overrideWindowId != WindowId.None
                ? UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel, _overrideWindowId)
                : UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
            exchangeCoinPanel.SetData(type);
        }
    }

    private void OnTipsClick()
    {
        DidClickAction?.Invoke();
        UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, this.type);
    }

    public static Dictionary<CurrencyType, string> IconConfig
    {
        get
        {
            return new Dictionary<CurrencyType, string>()
            {
                { CurrencyType.Gem, "ic_widget_gem" },
                { CurrencyType.Coin, "ic_widget_coin" },
                { CurrencyType.Badge, "ic_widget_badge" },
                { CurrencyType.PinkCoin, "icn_common_pink_big" },
                { CurrencyType.GreenCoin, "icn_common_green_big" },
                { CurrencyType.Points, "icn_common_points_big" },
                { CurrencyType.EnergyCoin, "icn_common_creator_big" },
                { CurrencyType.LuckyCoin, "icn_common_lucky_big" },
                { CurrencyType.ChristmasCoin, "icn_common_christmas_big" },
                { CurrencyType.MagicCoin, "icn_common_magic_big" },
                { CurrencyType.LuckyTicket, "icn_common_luckyticket_big" },
                { CurrencyType.ChristmasTicket, "icn_common_christmasticket_big" },
                { CurrencyType.CoinTicket, "icn_common_cointicket_big" },
                { CurrencyType.YouYouCoin, "icn_common_youyou_big"},
                { CurrencyType.PurpleDreamCoin, "icn_common_purpledream_big"},
                { CurrencyType.PopularityTicket, "icn_common_greenTicket_big" },
                { CurrencyType.GiftTicket, "icn_common_giftTicket_big" },
                { CurrencyType.PurpleDreamTicket, "icn_common_purpledream_ticket_big" },
                { CurrencyType.SweetieTicket, "icn_common_sweetTicket_big" },
                { CurrencyType.AISeasonCoin, "icn_common_aiSeason_big" },
                { CurrencyType.AICore, "icn_common_aicore_big" },
                { CurrencyType.SeasonPassCoin, "icn_seasonpass_coin" },
                { CurrencyType.CollectionTicket, "icn_collection_coin" },
                { CurrencyType.Crystal, "icn_common_crystal" },
                { CurrencyType.CrystalShards, "icn_common_crystal_shards" },
                { CurrencyType.MusicNoteCrystal, "icn_common_music_note_crystal" },
                { CurrencyType.MusicNoteCrystalShards, "icn_common_music_note_crystal_shards" },
                { CurrencyType.ShrimpYuan, "icn_common_shrimp_yuan" },
                { CurrencyType.ZZZCoin, "icn_common_zzz_coin" },
                { CurrencyType.ZZZPhantomCrystal, "icn_common_zzz_phantom_crystal" },
                { CurrencyType.ZZZPhantomCrystalShards, "icn_common_zzz_phantom_crystal_shards" },
            };
        }
    }
}
