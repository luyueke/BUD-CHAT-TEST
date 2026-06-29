using System;
using System.Collections.Generic;
using Basic.Extensions;
using Basic.Utils;
using Es;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.MailBox
{
    public class MailBoxSystemView : MailBoxBaseView
    {
        private readonly List<MailBoxGiftRewardItem> _dynamicRewardItems = new List<MailBoxGiftRewardItem>();

        private MailboxEntry mailboxEntry;
        private GameObject rightPart;
        private SuperTextMesh descTitle;
        private Text descSender;
        private Text descSendTime;
        private SuperTextMesh descContent;
        private Text EmptyText;
        private RectTransform descScrollRect;
        private Transform rewardParend;
        private Transform rewardItem;
        private GameObject rewardNode;
        private CButton claimBtn;
        private GameObject claimedMask;
        private GameObject claimLoadObj;
        private GameObject jumpNode;
        private CButton jumpBtn;

        private MailInfo _curShowInfo;
        private MailboxSubType _mailboxSubType = MailboxSubType.System;

        private const string SelfiePoseAtlasPath = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";

        protected override void Init()
        {
            rewardParend = GameObjectEx.FindChildByName(transform, "RewardParend");
            rewardItem = GameObjectEx.FindChildByName(transform, "RewardItem");
            rewardItem.gameObject.SetActive(false);
            mailboxEntry = GameObjectEx.FindChildByName(transform, "SystemEntry").GetComponent<MailboxEntry>();
            descTitle = GameObjectEx.FindChildByName(transform, "Title").GetComponent<SuperTextMesh>();
            descSender = GameObjectEx.FindChildByName(transform, "Sender").GetComponent<Text>();
            descSendTime = GameObjectEx.FindChildByName(transform, "SendTime").GetComponent<Text>();
            descContent = GameObjectEx.FindChildByName(transform, "Desc").GetComponent<SuperTextMesh>();
            descScrollRect = GameObjectEx.FindComponentByName<RectTransform>(transform, "Scroll View");
            rightPart = GameObjectEx.FindChildByName(transform, "RightView").gameObject;
            EmptyText = GameObjectEx.FindChildByName(transform, "EmptyText").GetComponent<Text>();
            rewardNode = GameObjectEx.FindChildByName(transform, "RewardNode").gameObject;
            claimBtn = GameObjectEx.FindChildByName(transform, "ClaimButton").GetComponent<CButton>();
            claimLoadObj = GameObjectEx.FindChildByName(transform, "ClaimLoading").gameObject;
            claimedMask = GameObjectEx.FindChildByName(transform, "ClaimMask").gameObject;
            jumpNode = GameObjectEx.FindChildByName(transform, "JumpNode").gameObject;
            jumpBtn = GameObjectEx.FindChildByName(jumpNode, "JumpButton").GetComponent<CButton>();

            rightPart.SetActive(false);
            RequestDataList();
        }

        protected override void OnViewShow() { }

        private void OnDestroy()
        {
            ClearDynamicRewardItems();
        }

        private void RequestDataList()
        {
            EmptyText.gameObject.SetActive(true);
            EmptyText.SetLocalText("加载中...");
            mailboxEntry.SetActions(OnStudioItemClick, _mailboxSubType);
            mailboxEntry.GetFirstPageDatas(GetPageDatas);
        }

        private void GetPageDatas(List<MailInfo> infos)
        {
            if (infos != null && infos.Count > 0)
            {
                infos[0].isSelect = true;
                OnStudioItemClick(infos[0]);
                EmptyText.gameObject.SetActive(false);
            }
            else
            {
                EmptyText.SetLocalText("你没有收到邮件");
            }
        }

        public void OnStudioItemClick(MailInfo info)
        {
            if (info == null) return;

            _curShowInfo = info;
            rightPart.SetActive(true);
            descTitle.SetText(info.title);
            descSender.SetText(info.sender != null ? info.sender.name : "");
            descSendTime.SetText(TimestampConverter.ConvertToDateTimeString(info.sendTime));
            descContent.SetText(info.content);
            rewardNode.SetActive(false);
            jumpNode.SetActive(false);

            Vector2 offsetMin = descScrollRect.offsetMin;
            offsetMin.y = 270;

            bool isClaimed = info.status == 2;
            claimedMask.SetActive(isClaimed);
            claimBtn.gameObject.SetActive(!isClaimed);
            claimBtn.onClick.RemoveAllListeners();
            claimBtn.onClick.AddListener(() => OnClaimBtnClick(info));

            if (info.attachments != null && info.attachments.Count > 0 && info.attachments[0] != null)
            {
                rewardNode.SetActive(true);
                ClearDynamicRewardItems();

                foreach (var attachment in info.attachments)
                {
                    if (attachment == null) continue;
                    var go = Instantiate(rewardItem.gameObject, rewardParend);
                    go.SetActive(true);
                    var item = go.GetComponent<MailBoxGiftRewardItem>();
                    _dynamicRewardItems.Add(item);
                    SetRewardItemIcon(item, attachment);
                    item.rewardMask.SetActive(isClaimed);
                }
            }
            else if (info.skipData != null)
            {
                jumpNode.SetActive(true);
                jumpBtn.onClick.RemoveAllListeners();
                jumpBtn.onClick.AddListener(() => WebtoolNewsSkipManager.Inst.HandleSkip(info.skipData));
            }
            else
            {
                offsetMin.y = 65;
            }

            descScrollRect.offsetMin = offsetMin;
            SetIsRead(info);
        }

        private void SetRewardItemIcon(MailBoxGiftRewardItem item, MailAttachmentsInfo attachment)
        {
            bool showNum = false;
            if (attachment.rewardType == BUDRewardType.RewardPgcResource)
            {
                item.rewardIcon.sprite = LoadPgcRewardIcon(attachment.rewardId);
            }
            else if (GameUtils.IsCurrencyType((int)attachment.rewardType))
            {
                item.rewardIcon.sprite = PgcUtils.LoadCurrencyIcon(GameUtils.ConvertRewardType((int)attachment.rewardType), gameObject);
                showNum = true;
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeUserTitle)
            {
                TitlePreviewPanel.LoadImage(GetUserTitleStr(attachment.rewardId), item.rewardIcon);
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeEnterEffect)
            {
                TitlePreviewPanel.LoadImage(GetEnterEffectStr(attachment.rewardId), item.rewardIcon);
            }
            else if (attachment.rewardType == BUDRewardType.RewardAvatarFrame)
            {
                if (int.TryParse(attachment.rewardId, out var frameId))
                {
                    var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(frameId, gameObject);
                    if (headCycleData != null) item.rewardIcon.sprite = headCycleData.Sp_PreviewCycle;
                }
                showNum = true;
            }
            else if (attachment.rewardType == BUDRewardType.RewardChatBubbles)
            {
                if (int.TryParse(attachment.rewardId, out var bubbleId))
                    item.rewardIcon.sprite = UserUIWidgetManager.Inst.GetChoosePanelBubbleBg(bubbleId, gameObject);
                showNum = true;
            }
            else if (attachment.rewardType == BUDRewardType.RewardHomepageSkin)
            {
                if (int.TryParse(attachment.rewardId, out var skinId))
                    item.rewardIcon.sprite = ProfileThemeManager.Inst.LoadThemeIcon(skinId, gameObject);
                showNum = true;
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeNicknameFrame)
            {
                if (int.TryParse(attachment.rewardId, out var nfId))
                    item.rewardIcon.sprite = UserUIWidgetManager.Inst.GetNicknameBg(nfId, gameObject);
                showNum = true;
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeTitle)
            {
                if (int.TryParse(attachment.rewardId, out var titleId))
                    item.rewardIcon.sprite = UserUIWidgetManager.Inst.GetTitleBg(titleId, gameObject);
                showNum = true;
            }
            else if (attachment.rewardType == BUDRewardType.RewardCommunityAnimationTicket
                     || attachment.rewardType == BUDRewardType.RewardCommunityInstrumentTicket
                     || attachment.rewardType == BUDRewardType.RewardCommunitySkinTicket)
            {
                item.rewardIcon.sprite = PgcUtils.LoadCurrencyIcon(GameUtils.ConvertRewardType((int)attachment.rewardType), gameObject);
                showNum = true;
            }

            item.rewardNum.transform.parent.gameObject.SetActive(showNum);
            if (showNum) item.rewardNum.SetText(attachment.count.ToString());
        }

        private void SetIsRead(MailInfo mailInfo)
        {
            if (mailInfo.status != 0 || string.IsNullOrEmpty(mailInfo.mailId)) return;

            var mailReadReq = new MailReadReq { mailIds = new List<string> { mailInfo.mailId } };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MailRead, HttpMethod.POST,
                JsonConvert.SerializeObject(mailReadReq),
                (_) =>
                {
                    mailInfo.status = 1;
                    if (mailboxEntry != null) mailboxEntry.UpdateSingleItem(mailInfo);
                },
                (error) => { });
        }

        private void OnClaimBtnClick(MailInfo info)
        {
            claimLoadObj.SetActive(true);
            ClaimReward(info, OnClaimSuccessCallBack, OnClaimFailCallBack);
        }

        private void OnClaimFailCallBack()
        {
            claimLoadObj.SetActive(false);
            TipPanel.ShowToast("奖励领取失败");
        }

        private void OnClaimSuccessCallBack(List<MailAttachmentsInfo> attachments, MailInfo reqMailInfo)
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            claimLoadObj.SetActive(false);

            if (_curShowInfo != null && _curShowInfo.mailId == reqMailInfo.mailId)
            {
                claimedMask.SetActive(true);
                foreach (var item in _dynamicRewardItems)
                    item.rewardMask.SetActive(true);
            }

            reqMailInfo.status = 2;
            mailboxEntry.UpdateSingleItem(reqMailInfo);

            if (attachments == null || attachments.Count == 0) return;

            var rewardList = new List<CommonRewardItemData>();
            foreach (var attachment in attachments)
            {
                if (attachment == null) continue;
                var data = BuildCommonRewardData(attachment);
                rewardList.Add(data);
            }

            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardList);
        }

        private CommonRewardItemData BuildCommonRewardData(MailAttachmentsInfo attachment)
        {
            var data = new CommonRewardItemData { RewardAmount = attachment.count };

            if (attachment.rewardType == BUDRewardType.RewardPgcResource)
            {
                data.IconSp = LoadPgcRewardIcon(attachment.rewardId);
                data.rewardName = attachment.rewardName;
                data.rewardType = (int)BUDRewardType.RewardPgcResource;
                data.pgcId = attachment.rewardId;
            }
            else if (GameUtils.IsCurrencyType((int)attachment.rewardType))
            {
                data.IconSp = PgcUtils.LoadCurrencyIcon(GameUtils.ConvertRewardType((int)attachment.rewardType), gameObject);
                data.rewardName = PgcUtils.GetRewardName(attachment.rewardType);
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeUserTitle)
            {
                data.IconSp = TitlePreviewPanel.LoadImage(GetUserTitleStr(attachment.rewardId), gameObject);
                data.rewardSpecial = attachment.count + "天";
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeEnterEffect)
            {
                data.IconSp = TitlePreviewPanel.LoadImage(GetEnterEffectStr(attachment.rewardId), gameObject);
                data.rewardSpecial = attachment.count + "天";
            }
            else if (attachment.rewardType == BUDRewardType.RewardAvatarFrame)
            {
                if (int.TryParse(attachment.rewardId, out var frameId))
                {
                    var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(frameId, gameObject);
                    if (headCycleData != null) data.IconSp = headCycleData.Sp_PreviewCycle;
                }
                data.rewardName = attachment.rewardName;
            }
            else if (attachment.rewardType == BUDRewardType.RewardChatBubbles)
            {
                if (int.TryParse(attachment.rewardId, out var bubbleId))
                    data.IconSp = UserUIWidgetManager.Inst.GetChoosePanelBubbleBg(bubbleId, gameObject);
                data.rewardName = attachment.rewardName;
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeNicknameFrame)
            {
                if (int.TryParse(attachment.rewardId, out var nfId))
                    data.IconSp = UserUIWidgetManager.Inst.GetNicknameBg(nfId, gameObject);
                data.rewardName = attachment.rewardName;
            }
            else if (attachment.rewardType == BUDRewardType.RewardTypeTitle)
            {
                if (int.TryParse(attachment.rewardId, out var titleId))
                    data.IconSp = UserUIWidgetManager.Inst.GetTitleBg(titleId, gameObject);
                data.rewardName = attachment.rewardName;
            }
            else if (attachment.rewardType == BUDRewardType.RewardCommunityAnimationTicket
                     || attachment.rewardType == BUDRewardType.RewardCommunityInstrumentTicket
                     || attachment.rewardType == BUDRewardType.RewardCommunitySkinTicket)
            {
                data.IconSp = PgcUtils.LoadCurrencyIcon(GameUtils.ConvertRewardType((int)attachment.rewardType), gameObject);
                data.rewardName = PgcUtils.GetRewardName(attachment.rewardType);
            }

            return data;
        }

        private void ClaimReward(MailInfo info, Action<List<MailAttachmentsInfo>, MailInfo> successCallBack, Action failCallBack)
        {
            var req = new MailClaimReq { mailIds = new List<string> { info.mailId } };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MailClaim, HttpMethod.POST, JsonConvert.SerializeObject(req),
                (content) =>
                {
                    var response = JsonConvert.DeserializeObject<MailClaimResponse>(content);
                    if (response.attachments == null)
                        response.attachments = new List<MailAttachmentsInfo>();
                    successCallBack?.Invoke(response.attachments, info);
                },
                (error) => { failCallBack?.Invoke(); }, timeOut: 10);
        }

        private void ClearDynamicRewardItems()
        {
            foreach (var item in _dynamicRewardItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _dynamicRewardItems.Clear();
        }

        private Sprite LoadPgcRewardIcon(string pgcId)
        {
            var sp = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
            if (sp != null) return sp;

            var selfieCfg = DataTables.GetCameraSelfiePose(pgcId);
            if (selfieCfg != null && !string.IsNullOrEmpty(selfieCfg.iconString))
            {
                sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SelfiePoseAtlasPath, selfieCfg.iconString, gameObject);
                if (sp != null) return sp;
                if (selfieCfg.iconString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(selfieCfg.iconString, gameObject);
            }
            return sp;
        }

        private static string GetUserTitleStr(string rewardId) => rewardId switch
        {
            "4" => "planet",
            "5" => "star",
            "6" => "moon",
            _ => ""
        };

        private static string GetEnterEffectStr(string rewardId) => rewardId switch
        {
            "1004" => "PurpleBG",
            "1005" => "BlueBG",
            "1006" => "GreenBG",
            _ => ""
        };

        public void IsEmptyAction() { }
    }

    public class MailClaimReq
    {
        public List<string> mailIds;
    }

    public class MailClaimResponse
    {
        public List<MailAttachmentsInfo> attachments;
    }
}
