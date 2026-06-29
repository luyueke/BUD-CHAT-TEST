
using System.Collections.Generic;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace  BUD.MailBox
{
    public class MailboxPanel : BasePanel<MailboxPanel>
    {
        [SerializeField] private TabView tabView;
        [SerializeField] private MailBoxSystemView systemView;
        [SerializeField] private MailBoxInteractiveView interactiveView;
        [SerializeField] private MailBoxGiftView giftView;
        [SerializeField] private MailBoxCheckView checkView;
        private CButton closeBtn;
        public enum MailboxViewEnum
        {
            System,//系统
            Interactive,//互动
            Gift,//礼物
            Check//审核
        }
        public class MailboxConfig
        {
            public string name;
            public MailboxViewEnum type;
            public ReddotType redDotType;

            public RedDotItem redDot;
        }
        private List<MailboxConfig> rtConfig = new()
        {
            new(){name = "系统", type = MailboxViewEnum.System,redDotType = ReddotType.Mail},
            new(){name = "互动", type = MailboxViewEnum.Interactive,redDotType = ReddotType.InteractNotification},
            new(){name = "礼物", type = MailboxViewEnum.Gift,redDotType = ReddotType.GiftNotification},
            new(){name = "审核", type = MailboxViewEnum.Check,redDotType = ReddotType.AuditNotification},
        };

        public override void OnCreate()
        {
            InitUI();
            foreach (var cfg in rtConfig)
            {
                var tab = tabView.CreateItem(cfg.type.ToString(), cfg.name);
                tab.SetIsSelect(false);
                cfg.redDot = tab.GetComponentInChildren<RedDotItem>();
            }
            tabView.AddItemSelectCallBack(MailTabClick);
            tabView.SetSelect((int)MailboxViewEnum.System);
            MessageHelper.AddListener(MessageName.ReddotNotice, UpdateNewIcon);
            UpdateNewIcon();
        }
        
        private void UpdateNewIcon()
        {
            rtConfig.Find(x => x.type == MailboxViewEnum.System).redDot.SetRedDotNum(ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.Mail));
            rtConfig.Find(x => x.type == MailboxViewEnum.Interactive).redDot.SetRedDotNum(ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.InteractNotification));
            rtConfig.Find(x => x.type == MailboxViewEnum.Gift).redDot.SetRedDotNum(ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.GiftNotification));
            rtConfig.Find(x => x.type == MailboxViewEnum.Check).redDot.SetRedDotNum(ReddotManagerUtils.Inst.GetRedDotCount(ReddotType.AuditNotification));
        }
        private void InitUI()
        {
            closeBtn = GameObjectEx.FindChildByName(transform,"ClosePanelBtn" ).GetComponent<CButton>();
            closeBtn.onClick.AddListener( ()=>
            {
                UIManager.Inst.ClosePanel(this);
                //var hall = UIManager.Inst.FindPanel<GameHallPanel>(PanelId.GameHallPanel);
                //hall.ReturnToLandscape();
            });
        }
        private void MailTabClick(TabItem item, int index)
        {
            var data = rtConfig[index];
            ReddotManagerUtils.Inst.CleanRedDotNum(data.redDotType);
            SetViewOpen(data);
        }
        private void SetViewOpen(MailboxConfig config)
        {
            //关闭开启的view
            if (config.type!=MailboxViewEnum.System)
            {
                systemView.Hide();
            }
            if (config.type!=MailboxViewEnum.Interactive)
            {
                interactiveView.Hide();
            }

            if (config.type!=MailboxViewEnum.Gift)
            {
                giftView.Hide();
            }
            if (config.type!=MailboxViewEnum.Check)
            {
                checkView.Hide();
            }
            switch (config.type)
            {
                case MailboxViewEnum.System:
                    systemView.Show();
                    break;
                case MailboxViewEnum.Interactive:
                    interactiveView.Show();
                    break;
                case MailboxViewEnum.Gift:
                    giftView.Show();
                    break;
                case MailboxViewEnum.Check:
                    checkView.Show();
                    break;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            MessageHelper.RemoveListener(MessageName.ReddotNotice, UpdateNewIcon);
        }
    }

    public class MailInfo
    {
        public string mailId;
        public long sendTime;
        public string title;
        public string content;
        public int status;//0-未读｜1-已读｜2-已领取 | 3-已拒绝
        public MailSenderInfo sender;
        public List<MailAttachmentsInfo> attachments;

        public MailSkipData skipData;
        //业务使用，用于选中项
        public bool isSelect;
    }


    public class MailSkipData {
        public int skipType;
        public string extraData;
    }


    public class MailSenderInfo
    {
        public string uid;
        public string name;
        public string portraitUrl;
        public int avatarFrame;
    }
    //附件信息
    public class MailAttachmentsInfo
    {
        public string id;
        public int count;
        public BUDRewardType rewardType;
        public string rewardId;
        public string rewardName;
        public string cover;
        public int giftPrice;
        public int isIapProduct;
        public int giftType;
        public int giftCurrencyType;

        public CurrencyType GetTokenType()
        {
            switch (id)
            {
                case "Coin":
                    return CurrencyType.Coin;
                case "Badge":
                    return CurrencyType.Badge;
                case "Gem":
                    return CurrencyType.Gem;
                case "AvatarVoucher":
                    return CurrencyType.Ticket;
                case "GashaponVoucher":
                    return CurrencyType.Ticket;
                default:
                    return CurrencyType.None;
            }
        }
    }
    public class NotificationInfo
    {
        public string text;
        public long timestamp;
        public MailSenderInfo sender;
        public NotificationBizInfo info;
        public NotificationRelation relation;
    }
    public class NotificationBizInfo
    {
        public int bizType;
        public string bizId;
        public string bizUrl;
        public int gameType;
        public int gameId;
        public enum BizType
        {
            Map = 1,
            Skin = 2,
            Prop = 3,
            Mat = 4,
            MusicTone = 5,
            MusicScore = 6,
            UgcAnim = 7,
            UgcPose = 8,
            UgcAnimMusic = 9,
            NPC = 10,
            Vehicle = 11,
            CabinCharacter = 12,
            CabinTone = 13,
            Theatre = 15,
        }
    }
    public class NotificationRelation
    {
        public int status;
    }
}
