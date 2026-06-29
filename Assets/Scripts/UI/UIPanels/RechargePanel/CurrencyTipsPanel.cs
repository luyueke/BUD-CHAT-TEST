using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.Manager;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

[Serializable]
public class CurrencyTipsInfo
{
    public int type;
    public string id;
    public string title;
    public string iconName;
    public string des;
    public string sourceText;//来源说明文本
    public Action sourceAction;//前往来源地
}

public class CurrencyTipsPanel : BasePanel<CurrencyTipsPanel>
{
    private GameObject _go_CurrencyPreview;
    private Image icon;
    private Text title;
    private Text countTxt;
    private Text des;
    private CButton closeBtn;

    #region 头像框预览
    private GameObject _go_HeadPreview;
    private HeadViewWidget _headViewWidget;
    #endregion

    #region 头像框预览
    private GameObject _go_TitlePreview;
   
    #endregion


    #region 聊天气泡Icon
    private GameObject _go_ChatBubble;
    private Image _img_ChatBubbleIcon;
    #endregion

    #region 获取途径
    private GameObject source;
    private Text sourceText;
    private CButton sourceBtn;
    private GameObject sourceBG;
    #endregion

    private Dictionary<CurrencyType, CurrencyTipsInfo> BalanceConfig = CurrencyTipsConfig.BalanceConfig;
    private ContentPosType _contentPos = ContentPosType.Center;
    public ContentPosType currentPosType
    {
        get
        {
            return _contentPos;
        }
        set
        {
            _contentPos = value;
            adaptPosUI();
        }
    }
    public enum ContentPosType
    {
        Center = 0,
        LeftCenter = 1,
    }

    public override void OnShow(params object[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        CurrencyType balanceType = (CurrencyType)args[0];

        int ownedCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(balanceType);
        if (args.Length > 2)
        {
            ownedCount = (int)args[1];
        }
        //兑换卷相关信息是存在token中的，使用blance无法获取，需要二次判断
        if(balanceType == CurrencyType.CelebrationCoin||
            balanceType == CurrencyType.CommunityAnimationTicket ||
            balanceType == CurrencyType.CommunityInstrumentTicket ||
            balanceType == CurrencyType.CommunityVehicleTicket ||
            balanceType == CurrencyType.CommunityTheaterTicket ||
            balanceType == CurrencyType.CommunitySkinTicket)
        {
            ownedCount = (int)TokenDataManager.Inst.Data.GetToken(balanceType);

        }

        SetContent(balanceType, ownedCount);
    }

    public override void OnCreate()
    {
        _go_CurrencyPreview = GameObjectEx.FindChildByName(transform, "Go_CurrencyPreview").gameObject;
        _go_HeadPreview = GameObjectEx.FindChildByName(transform, "Go_AvatarFramePreview").gameObject;
        _go_ChatBubble = GameObjectEx.FindChildByName(transform, "Go_ChatBubblePreview").gameObject;
        _go_TitlePreview = GameObjectEx.FindChildByName(transform, "Go_TitlePreview").gameObject;
        _headViewWidget = GameObjectEx.FindChildByName(transform, "HeadViewWidget").GetComponent<HeadViewWidget>();
        _img_ChatBubbleIcon = GameObjectEx.FindChildByName(transform, "Go_ChatBubblePreview/Icon").GetComponent<Image>();
        source = GameObjectEx.FindChildByName(transform, "source").gameObject;
        sourceText = GameObjectEx.FindChildByName(source, "sourceText").GetComponent<Text>();
        sourceBtn = GameObjectEx.FindChildByName(source, "sourceBtn").GetComponent<CButton>();
        sourceBG = GameObjectEx.FindChildByName(transform, "sourceBG").gameObject;
        source.SetActive(false);
        sourceBG.SetActive(false);
        _go_CurrencyPreview.SetActive(true);
        _go_HeadPreview.SetActive(false);
        _go_ChatBubble.SetActive(false);
        _go_TitlePreview.SetActive(false);

        icon = GameObjectEx.FindChildByName(transform, "Icon").GetComponent<Image>();
        title = GameObjectEx.FindChildByName(transform, "Title").GetComponent<Text>();
        countTxt = GameObjectEx.FindChildByName(transform, "Count").GetComponent<Text>();
        des = GameObjectEx.FindChildByName(transform, "Des").GetComponent<Text>();
        closeBtn = GameObjectEx.FindChildByName(transform, "CloseBtn").GetComponent<CButton>();
        closeBtn?.onClick.AddListener(OnCloseBtnClick);
    }

    /// <summary>
    /// 设置内容展示
    /// </summary>
    /// <param name="type">当前需要展示的货币</param>
    /// <param name="count">货币数量</param>
    private void SetContent(CurrencyType type, int count)
    {
        if (BalanceConfig.TryGetValue(type, out var info))
        {
            // 作用域覆盖(如娃娃机换皮)：有覆盖名则用原文直显，否则走本地化 key
            var nameOverride = PgcUtils.GetScopeCurrencyName(type);
            if (!string.IsNullOrEmpty(nameOverride))
            {
                title.text = nameOverride;
            }
            else
            {
                title.SetLocalText(info.title);
            }
            des.SetLocalText(info.des);

            var iconName = PgcUtils.GetScopeCurrencyIconName(type) ?? info.iconName;
            var spriteatlasPath = RechargePanel.ConsumptionTicketPanelAtlas;
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, iconName, gameObject);
            if (sprite == null)
            {
                sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, iconName, gameObject);
            }

            icon.sprite = sprite;
            if(!string.IsNullOrEmpty(info.sourceText) && info.sourceAction != null)
            {
                source.SetActive(true);
                sourceBG.SetActive(true);
                sourceText.text = info.sourceText;
                sourceBtn.onClick.RemoveAllListeners();
                sourceBtn.onClick.AddListener(() => info.sourceAction.Invoke());
            }
            else
            {
                source.SetActive(false);
                sourceBG.SetActive(false);
            }
        }

        countTxt.SetLocalText("当前拥有：{0}", count);
    }

    public void SetVipContent()
    {
        title.SetLocalText("VIP体验卡3天");
        des.SetLocalText("特殊权益，领取后VIP月卡有效期增加3天");

        var spriteatlasPath = RechargePanel.ConsumptionTicketPanelAtlas;
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath,"icon_vip", gameObject);
        if (sprite == null)
        {
            sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "icon_vip", gameObject);
        }

        icon.sprite = sprite;
        countTxt.SetLocalText("");
    }

    public void UpdateUI(Sprite iconSp, string titleText, string countText, string descText)
    {
        icon.sprite = iconSp;
        icon.SetNativeSize();
        title.SetLocalText(titleText);
        if (!string.IsNullOrEmpty(countText)) {
            countTxt.SetLocalText("当前拥有：{0}",countText);
        } else {
            countTxt.SetLocalText("");
        }
        des.SetLocalText(descText);
    }
    
    /// <summary>
    /// 预览聊天气泡Icon
    /// </summary>
    /// <param name="type"></param>
    /// <param name="titleText"></param>
    /// <param name="descText"></param>
    public void PreviewChatBubble(ChatBubblesType type, string titleText, string descText)
    {
        _go_CurrencyPreview.SetActive(false);
        _go_ChatBubble.SetActive(true);
        _go_HeadPreview.SetActive(false);
        _go_TitlePreview.SetActive(false);
        var bubbleSp = UserUIWidgetManager.Inst.GetChatBubbleIconByType(type, this.gameObject);
        _img_ChatBubbleIcon.sprite = bubbleSp;
        if(type >= ChatBubblesType.ChatBubblesQianqianWanwan)
        {
            _img_ChatBubbleIcon.SetNativeSize();
        }
        title.SetLocalText(titleText);
        countTxt.SetLocalText("");
        des.SetLocalText(descText);
    }

    public void PreviewTitle(int titleId)
    {
        _go_CurrencyPreview.SetActive(false);
        _go_ChatBubble.SetActive(false);
        _go_HeadPreview.SetActive(false);
        _go_TitlePreview.SetActive(true);
        countTxt.SetLocalText("");
        if (titleId > 0)
        {
            var config = UserUIWidgetManager.Inst.GetTitleData(titleId);
   
            if (config != null )
            {
                title.SetLocalText(config.Name);
                des.SetLocalText(config.Desc);
                for (int i = _go_TitlePreview.transform.childCount - 1; i >= 0; i--)
                {
                    GameObject.DestroyImmediate(_go_TitlePreview.transform.GetChild(i).gameObject);
                }
                var o = Loader.Load<GameObject>(config.Prefab, gameObject);
                 GameObject.Instantiate(o, _go_TitlePreview.transform);
            }

        }
    }


    /// <summary>
    /// 预览昵称框 - 传入 pgcId，title/desc 从 NicknameConfig 读取
    /// </summary>
    public void PreviewNicknameFrame(string pgcId)
    {
        _go_CurrencyPreview.SetActive(false);
        _go_ChatBubble.SetActive(false);
        _go_HeadPreview.SetActive(false);
        _go_TitlePreview.SetActive(true);
        countTxt.SetLocalText("");

        for (int i = _go_TitlePreview.transform.childCount - 1; i >= 0; i--)
            GameObject.DestroyImmediate(_go_TitlePreview.transform.GetChild(i).gameObject);

        var config = UserUIWidgetManager.Inst.GetNicknameByPgcId(pgcId);
        if (config == null)
        {
            LoggerUtils.LogError($"[CurrencyTipsPanel] PreviewNicknameFrame: pgcId={pgcId} 未找到 NicknameData 配置");
            return;
        }

        title.SetLocalText(config.Title ?? config.Name);
        des.SetLocalText(config.Desc);

        if (!string.IsNullOrEmpty(config.Prefab))
        {
            var prefab = Loader.Load<GameObject>(config.Prefab, gameObject);
            if (prefab != null)
            {
                GameObject.Instantiate(prefab, _go_TitlePreview.transform);
                return;
            }
        }

        // Prefab 不存在时回退：加载 atlas 小图显示在 icon 上
        _go_TitlePreview.SetActive(false);
        _go_CurrencyPreview.SetActive(true);
        UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(pgcId, gameObject, (sp) =>
        {
            if (this != null && icon != null && sp != null)
            {
                icon.sprite = sp;
                icon.SetNativeSize();
            }
        });
    }

    /// <summary>
    /// 预览头像框 - 传入AvatarFrameType
    /// Eg:panel.PreviewAvatarFrame(AvatarFrameType.AvatarFrameNewYearLimitedPack);
    /// </summary>
    /// <param name="avatarFrame"></param>
    public void PreviewAvatarFrame(AvatarFrameType avatarFrame)
    {
        _go_CurrencyPreview.SetActive(false);
        _go_ChatBubble.SetActive(false);
        _go_HeadPreview.SetActive(true);
        _go_TitlePreview.SetActive(false);

        var avatarFrameData = UserUIWidgetManager.Inst.GetHeadCycleData((int)avatarFrame, gameObject);
        if (avatarFrameData != null)
        {
            countTxt.gameObject.SetActive(false);
            var name = avatarFrameData.Name;
            title.SetLocalText(name);
            des.SetLocalText(avatarFrameData?.Desc);

            _headViewWidget.InitHeadCycle((int)avatarFrame);
        }
    }

    private void OnCloseBtnClick()
    {
        CloseSelf();
    }

    private void adaptPosUI()
    {
        var porpBg = GameObjectEx.FindChildByName(transform, "PropBg").gameObject;
        var iconNode = GameObjectEx.FindChildByName(transform, "iconNode").gameObject;
        if (porpBg == null || iconNode == null)
        {
            return;
        }

        switch (_contentPos)
        {
            case ContentPosType.Center:
                porpBg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                iconNode.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                break;
            case ContentPosType.LeftCenter:
                int screenWidth = Screen.width;
                float f = Convert.ToSingle(screenWidth / 6.0);
                var Pos = new Vector2(-f, 0);
                var porpBgRectT = porpBg.GetComponent<RectTransform>();
                porpBgRectT.anchoredPosition = Pos;

                var iconNodeRect = iconNode.GetComponent<RectTransform>();
                iconNodeRect.anchoredPosition = Pos;
                break;
        }
    }


}

public class CurrencyTipsConfig
{
    public static Dictionary<CurrencyType, CurrencyTipsInfo> BalanceConfig
    {
        get
        {
            var config = new Dictionary<CurrencyType, CurrencyTipsInfo>()
            {
                {
                    CurrencyType.Coin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.Coin,
                        title = "金币",
                        des = "普通货币，可用于参与金币扭蛋，通过完成任务或者参加活动获得。",
                        iconName = "icon_1",
                    }
                },
                {
                    CurrencyType.Gem, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.Gem,
                        title = "钻石",
                        des = "高级货币，可用于兑换所有商品，通过充值获得。",
                        iconName = "icon_3",
                    }
                },
                {
                    CurrencyType.Badge, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.Badge,
                        title = "徽章",
                        des = "普通货币，可用于购买官方商城的商品，通过参加活动或者钻石兑换获得。",
                        iconName = "icon_2",
                    }
                },
                {
                    CurrencyType.PinkCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.PinkCoin,
                        title = "社区商品币",
                        des = "稀有货币，可用于购买社区商城的商品，通过完成任务或者钻石兑换获得。",
                        iconName = "icon_5",
                    }
                },
                {
                    CurrencyType.GreenCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.GreenCoin,
                        title = "创作者币",
                        des = "稀有货币，可用于参与创作者中心扭蛋，通过售卖社区商品或者接受其他玩家投币获得。",
                        iconName = "icon_6",
                    }
                },
                {
                    CurrencyType.Points, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.Points,
                        title = "创作者积分",
                        des = "通过售卖钻石社区商品可获得等值的创作者积分币，在创作者中心可以用创作者积分进行货币和奖励兑换。",
                        iconName = "icon_7",
                    }
                },
                {
                    CurrencyType.EnergyCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.EnergyCoin,
                        title = "创作能量币",
                        des = "可用于支持创作者，投币的50%将转化为创作者的创作者币奖励，通过参与金币/幸运币扭蛋或者钻石兑换获得。",
                        iconName = "icon_8",
                    }
                },
                {
                    CurrencyType.LuckyCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.LuckyCoin,
                        title = "幸运币",
                        des = "稀有货币，可用于参与幸运币扭蛋，通过购买礼包或钻石兑换获得。",
                        iconName = "icn_common_lucky_big",
                    }
                },
                {
                    CurrencyType.ChristmasCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.ChristmasCoin,
                        title = "圣诞币",
                        des = "限时货币，仅限用于本赛季限定盲盒抽取",
                        iconName = "icn_common_christmas_big",
                    }
                },
                {
                    CurrencyType.MagicCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.MagicCoin,
                        title = "精灵魔法币",
                        des = "限时货币，用于赛季扭蛋抽取，赛季结束后未使用部分会等量转化为下赛季的扭蛋货币",
                        iconName = "icn_common_magic_big",
                    }
                },
                {
                    CurrencyType.LuckyTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.LuckyTicket,
                        title = "幸运券",
                        des = "可用于兑换幸运币扭蛋商城的商品，通过参与幸运币扭蛋获得。",
                        iconName = "icn_common_luckyticket_big",
                    }
                },
                {
                    CurrencyType.ChristmasTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.ChristmasTicket,
                        title = "铃铛券",
                        des = "限时货币，用于在圣诞响叮当盲盒兑换商店中兑换奖品",
                        iconName = "icn_common_christmasticket_big",
                    }
                },
                {
                    CurrencyType.CoinTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CoinTicket,
                        title = "金币券",
                        des = "可用于兑换金币扭蛋商城的商品，通过参与金币扭蛋获得。",
                        iconName = "icn_common_cointicket_big",
                    }
                },
                {
                    CurrencyType.PurpleDreamCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.PurpleDreamCoin,
                        title = "紫梦币",
                        des = "稀有货币，可用于参与扭蛋，通过购买礼包或钻石兑换获得。",
                        iconName = "icn_common_purpledream_big",
                    }
                },
                {
                    CurrencyType.YouYouCoin, new CurrencyTipsInfo() {
                        type = (int)CurrencyType.YouYouCoin,
                        title = "优优币",
                        des = "稀有货币，可用于参与赛季扭蛋，通过完成任务或者钻石兑换获得。",
                        iconName = "icn_common_youyou_big"
                    }
                },
                {
                    CurrencyType.GiftTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.GiftTicket,
                        title = "礼品券",
                        des = "用于兑换礼物",
                        iconName = "icn_common_giftTicket_big",
                    }
                },
                {
                    CurrencyType.PopularityTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.PopularityTicket,
                        title = "人气券",
                        des = "用于在创作者中心扭蛋兑换商店中兑换奖品",
                        iconName = "icn_common_greenTicket_big"
                    }
                },
                {
                    CurrencyType.PurpleDreamTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.PurpleDreamTicket,
                        title = "紫梦券",
                        des = "可用于兑换扭蛋商城的商品，通过参与紫梦币扭蛋获得，扭蛋活动结束后不会被清空",
                        iconName = "icn_common_purpledream_ticket_big"
                    }
                },
                {
                    CurrencyType.SweetieTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.SweetieTicket,
                        title = "甜心券",
                        des = "用于在扭蛋活动中兑换奖品，扭蛋活动结束后不会被清空",
                        iconName = "icn_common_sweetTicket_big"
                    }
                },
                {
                    CurrencyType.AISeasonCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.AISeasonCoin,
                        title = "AI赛季币",
                        des = "普通货币，用于赛季兑换商店中兑换各类奖励",
                        iconName = "icn_common_aiSeason_big"
                    }
                },
                {
                    CurrencyType.AICore, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.AICore,
                        title = "AI核心",
                        des = "稀有货币，用于赛季兑换商店中兑换各类奖励",
                        iconName = "icn_common_aicore_big"
                    }
                },
                {
                    CurrencyType.CommunityAnimationTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CommunityAnimationTicket,
                        title = "社区动作兑换券",
                        des = "稀有货币，可在购买售价≤200社区商品币的社区皮肤时抵扣消耗",
                        iconName = "icn_common_CommunityAnimationTicket_big"
                    }
                },
                {
                    CurrencyType.CommunityInstrumentTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CommunityInstrumentTicket,
                        title = "社区乐器兑换券",
                        des = "稀有货币，可在购买售价≤200社区商品币的社区皮肤时抵扣消耗",
                        iconName = "icn_common_CommunityInstrumentTicket_big"
                    }
                },
                {
                    CurrencyType.CommunitySkinTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CommunitySkinTicket,
                        title = "社区皮肤兑换券",
                        des = "稀有货币，可在购买售价≤60社区商品币的社区皮肤时抵扣消耗",
                        iconName = "icn_common_CommunitySkinTicket_big"
                    }
                },
                {
                    CurrencyType.CelebrationCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CelebrationCoin,
                        title = "庆典币",
                        des = "限时货币，可用于兑换庆典商店内的奖品",
                        iconName = "icn_common_CelebrationCoin_big"
                    }
                },
                {
                    CurrencyType.KoiGachaCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.KoiGachaCoin,
                        title = "抽奖券",
                        des = "限时货币，用于在幸运锦鲤活动中进行抽取奖励",
                        iconName = "icn_reward_lucky_koi"
                    }
                },
                {
                    CurrencyType.CollectionTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CollectionTicket,
                        title = "典藏券",
                        des = "用于在扭蛋活动中兑换珍稀奖品",
                        iconName = "icn_collection_coin"
                    }
                },
                {
                    CurrencyType.SeasonPassCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.SeasonPassCoin,
                        title = "通行证币",
                        des = "用于兑换奖励，可以从赛季通行证、扭蛋中获得",
                        iconName = "icn_seasonpass_coin"
                    }
                },
                {
                    CurrencyType.MiaoCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.MiaoCoin,
                        title = "喵币",
                        des = "限定货币,用于抽取音符布丁联名扭蛋",
                        iconName = "icn_common_miao_coin"
                    }
                },
                {
                    CurrencyType.SockCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.SockCoin,
                        title = "袜币",
                        des = "限定货币,用于抽取袜袜幼稚园联名扭蛋",
                        iconName = "icn_common_wa_coin"
                    }
                },
                {
                    CurrencyType.SockTailTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.SockTailTicket,
                        title = "粉绒小尾券",
                        des = "限定货币,用于袜袜幼稚园联名扭蛋兑换",
                        iconName = "icn_common_watail_coin"
                    }
                },
                {
                    CurrencyType.SockYunyunTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.SockYunyunTicket,
                        title = "暖橙晕晕券",
                        des = "限定货币,用于袜袜幼稚园联名扭蛋兑换",
                        iconName = "icn_common_wayunyun_coin"
                    }
                },
               {
                    CurrencyType.CommunityVehicleTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CommunityVehicleTicket,
                        title = "社区载具兑换券",
                        des = "稀有货币，可在购买售价≤200社区商品币的社区载具时抵扣消耗",
                        iconName = "icn_common_vehicle_Ticket"
                    }
                },
                {
                    CurrencyType.CommunityTheaterTicket, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CommunityVehicleTicket,
                        title = "剧场兑换券",
                        des = "稀有货币，可在购买售价≤3500社区商品币的剧场时抵扣消耗",
                        iconName = "icn_common_theater_onTicket_big"
                    }
                },
                {
                    CurrencyType.Crystal, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.Crystal,
                        title = "水晶",
                        des = "可用于在兑换商店中兑换新品载具等，也可以在载具升级系统中对载具进行升级",
                        iconName = "icn_common_crystal"
                    }
                },
                {
                    CurrencyType.CrystalShards, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.CrystalShards,
                        title = "水晶碎片",
                        des = "可用于在兑换商店中兑换各种道具（碎片不会过期，可在后续活动中继续使用）",
                        iconName = "icn_common_crystal_shards"
                    }
                },
                {
                    CurrencyType.MusicNoteCrystal, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.MusicNoteCrystal,
                        title = "音符水晶",
                        des = "可用于在兑换商店中兑换新品载具等，也可以在载具升级系统中对载具进行升级",
                        iconName = "icn_common_music_note_crystal"
                    }
                },
                {
                    CurrencyType.MusicNoteCrystalShards, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.MusicNoteCrystalShards,
                        title = "音符碎片",
                        des = "可用于在兑换商店中兑换各种道具（碎片不会过期，可在后续活动中继续使用）",
                        iconName = "icn_common_music_note_crystal_shards"
                    }
                },
                {
                    CurrencyType.ShrimpYuan, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.ShrimpYuan,
                        title = "虾元",
                        des = "限定货币,用于虾虾崽联名扭蛋兑换",
                        iconName = "icn_common_shrimp_yuan"
                    }
                },
                {
                    CurrencyType.Shovel, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.Shovel,
                        title = "铲子",
                        des = "限定货币，用于六一寻宝活动挖宝",
                        iconName = "icn_common_shovel_big"
                    }
                },
                {
                    CurrencyType.ZZZCoin, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.ZZZCoin,
                        title = "绒币",
                        des = "限定货币,用于抽取幻音派对联名扭蛋",
                        iconName = "icn_common_zzz_coin",
                        sourceText = "可通过购买绒天使联名礼包获取",
                        sourceAction = () => {
                            UIManager.Inst.ClosePanel(PanelId.CurrencyTipsPanel);
                            UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.zzzGiftPack);
                        }
                    }
                },
                {
                    CurrencyType.ZZZPhantomCrystal, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.ZZZPhantomCrystal,
                        title = "绒天使水晶",
                        des = "限定货币,用于幻音派对联名扭蛋兑换",
                        iconName = "icn_common_zzz_phantom_crystal",
                        sourceText = "可通过参与幻音派对扭蛋获取更多水晶",
                        sourceAction = () => { UIManager.Inst.ClosePanel(PanelId.CurrencyTipsPanel); 
                        UIManager.Inst.ClosePanel(PanelId.StoreMallPanel); 
                        UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.zzz.phantomParty"); }
                        
                    }
                },
                {
                    CurrencyType.ZZZPhantomCrystalShards, new CurrencyTipsInfo()
                    {
                        type = (int)CurrencyType.ZZZPhantomCrystalShards,
                        title = "绒天使碎片",
                        des = "限定货币,用于幻音派对联名扭蛋兑换",
                        sourceText = "可通过参与幻音派对扭蛋获取更多绒天使碎片",
                        iconName = "icn_common_zzz_phantom_crystal_shards",
                        sourceAction = () =>  { UIManager.Inst.ClosePanel(PanelId.CurrencyTipsPanel); 
                          UIManager.Inst.ClosePanel(PanelId.StoreMallPanel); 
                        UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.zzz.phantomParty"); }
                    }
                },

            };
            return config;
        }
    }
}
