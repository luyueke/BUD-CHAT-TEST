using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.Utilities;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.MailBox
{
    public enum MailBoxGiftTabType
    {
        ReceiveGift,
        RequestGift,
    }

    public enum GiftButtonType
    {
        Illegal = 0,
        Accept = 1,
        Send = 2,
        Refuse = 3
    }

    public class MailReadReq
    {
        public List<string> mailIds;
    }

    public class MailBoxGiftView : MailBoxBaseView
    {
        private MailBoxGiftEntry mailboxEntry;
        private GameObject rightPart;
        private SuperTextMesh descTitle;
        private Text descSender;
        private Text descSendTime;
        private SuperTextMesh descContent;
        private Text EmptyText;
        private GameObject receiveBg;
        private GameObject requestBg;
        private Button receiveButton;
        private Button requestButton;
        private Button infoButton;

        private Button refuseButton;
        private Button sendButton;
        private Button rewardButton;
        private GameObject toggle;

        #region 奖励图标

        private GameObject rewardNode;
        private Image rewardIcon;
        private RemoteImageBehaviour remoteAssetsIcon;
        private Text rewardNum;

        #endregion

        private GameObject claimedMask;
        private GameObject rewardMask;
        private GameObject SentMask;

        private GameObject claimLoadObj;

        private MailBoxGiftTabType mailBoxGiftTabType = MailBoxGiftTabType.ReceiveGift;

        private MailInfo _curShowInfo;
        private MailboxSubType _mailboxSubType = MailboxSubType.Gift;
        private bool isFirst = true;

        protected override void Init()
        {
            toggle = GameObjectEx.FindChildByName(transform, "Toggle").gameObject;
            mailboxEntry = GameObjectEx.FindChildByName(transform, "SystemEntry").GetComponent<MailBoxGiftEntry>();
            descTitle = GameObjectEx.FindChildByName(transform, "Title").GetComponent<SuperTextMesh>();
            descSender = GameObjectEx.FindChildByName(transform, "Sender").GetComponent<Text>();
            descSendTime = GameObjectEx.FindChildByName(transform, "SendTime").GetComponent<Text>();
            descContent = GameObjectEx.FindChildByName(transform, "Desc").GetComponent<SuperTextMesh>();
            rightPart = GameObjectEx.FindChildByName(transform, "RightView").gameObject;
            EmptyText = GameObjectEx.FindChildByName(transform, "EmptyText").GetComponent<Text>();
            rewardNode = GameObjectEx.FindChildByName(transform, "RewardNode").gameObject;
            rewardIcon = GameObjectEx.FindChildByName(transform, "RewardIcon").GetComponent<Image>();
            rewardNum = GameObjectEx.FindChildByName(transform, "RewardNum").GetComponent<Text>();
            claimLoadObj = GameObjectEx.FindChildByName(transform, "ClaimLoading").gameObject;
            rewardMask = GameObjectEx.FindChildByName(transform, "RewardMask").gameObject;
            receiveBg = GameObjectEx.FindChildByName(transform, "ReceiveBg").gameObject;
            requestBg = GameObjectEx.FindChildByName(transform, "RequestBg").gameObject;
            receiveButton = GameObjectEx.FindChildByName(transform, "ReceiveButton").GetComponent<Button>();
            requestButton = GameObjectEx.FindChildByName(transform, "RequestButton").GetComponent<Button>();
            refuseButton = GameObjectEx.FindChildByName(transform, "RefuseButton").GetComponent<Button>();
            sendButton = GameObjectEx.FindChildByName(transform, "SendButton").GetComponent<Button>();
            rewardButton = GameObjectEx.FindChildByName(transform, "RewardButton").GetComponent<Button>();
            infoButton = GameObjectEx.FindChildByName(transform, "InfoButton").GetComponent<Button>();
            claimedMask = GameObjectEx.FindChildByName(transform, "ClaimMask").gameObject;
            SentMask = GameObjectEx.FindChildByName(transform, "SentMask").gameObject;
            remoteAssetsIcon = GameObjectEx.FindChildByName(transform, "RemoteIcon")
                .GetComponent<RemoteImageBehaviour>();

            rightPart.SetActive(false);


            receiveButton.onClick.AddListener(() =>
            {
                rightPart.SetActive(false);
                receiveBg.gameObject.SetActive(true);
                requestBg.gameObject.SetActive(false);
                mailBoxGiftTabType = MailBoxGiftTabType.ReceiveGift;
                RequestDataList();
            });

            requestButton.onClick.AddListener(() =>
            {
                rightPart.SetActive(false);
                receiveBg.gameObject.SetActive(false);
                requestBg.gameObject.SetActive(true);
                mailBoxGiftTabType = MailBoxGiftTabType.RequestGift;
                RequestDataList();
            });

            infoButton.onClick.AddListener(() =>
            {
                MailboxRulePanel panel =
                    UIManager.Inst.OpenPanel<MailboxRulePanel>(PanelId.MailboxRulePanel);
            });

            RequestDataList();
        }

        protected override void OnViewShow()
        {
        }

        /// <summary>
        /// 重新请求数据
        /// </summary>
        private void RequestDataList()
        {
            EmptyText.gameObject.SetActive(true);
            EmptyText.SetLocalText("加载中...");
            mailboxEntry.SetActions(OnStudioItemClick, mailBoxGiftTabType);
            mailboxEntry.GetFirstPageDatas(GetPageDatas);
        }

        private void GetPageDatas(List<MailInfo> infos)
        {
            if (isFirst)
            {
                isFirst = false;

                // 首次调用时检查是否需要请求邮件
                if (infos == null || infos.Count == 0)
                {
                    GetRequestMails();
                }
            }

            if (!infos.IsNullOrEmpty())
            {
                OnStudioItemClick(infos[0]);
            }

            // 更新空状态
            UpdateEmptyState(infos);
        }

        private void GetRequestMails()
        {
            if (!this) return;

            var req = new MailboxListReq
            {
                cookie = "",
                type = 4
            };

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.MailList,
                HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                OnMailListResponse,
                (error) => { Debug.LogError("Failed to fetch mail list."); });
        }

        private void OnMailListResponse(string content)
        {
            var mailListResponse = JsonConvert.DeserializeObject<MailListResponse>(content);

            // 更新界面元素状态
            bool hasMails = mailListResponse?.mails != null && mailListResponse.mails.Count > 0;
            infoButton.gameObject.SetActive(hasMails);
            toggle.gameObject.SetActive(hasMails);

            // 更新空状态
            UpdateEmptyState(hasMails ? mailListResponse.mails : null);
        }

        private void UpdateEmptyState(List<MailInfo> infos)
        {
            bool isEmpty = infos == null || infos.Count == 0;
            EmptyText.gameObject.SetActive(isEmpty);
            EmptyText.SetLocalText(isEmpty ? "你没有收到邮件" : string.Empty);
        }

        public void OnStudioItemClick(MailInfo info)
        {
            if (info != null)
            {
                _curShowInfo = info;
                rightPart.SetActive(true);
                descTitle.SetText(info.title);
                descSender.SetText(info.sender != null ? ("发件人：" + info.sender.name) : "");
                descSendTime.SetText(TimestampConverter.ConvertToDateTimeString(info.sendTime));
                descContent.SetText(info.content);
                rewardNode.SetActive(false);
                if (info.attachments != null && info.attachments.Count > 0 && info.attachments[0] != null)
                {
                    var attachment = info.attachments[0];

                    rewardNode.SetActive(true);

                    if (attachment.rewardType == BUDRewardType.RewardPgcResource)
                    {
                        rewardIcon.sprite = PgcUtils.GetIconSpriteByPgcId(attachment.rewardId, rewardIcon.gameObject);
                        remoteAssetsIcon.gameObject.SetActive(false);
                        rewardIcon.gameObject.SetActive(true);
                        rewardNum.transform.parent.gameObject.SetActive(false);
                    }
                    else if (attachment.rewardType == BUDRewardType.RewardGiftUgc)
                    {
                        rewardNode.SetActive(true);
                        remoteAssetsIcon.Load(attachment.cover, onCompleted: (bool fromCache, bool success) =>
                        {
                            if (this == null) return;
                            rewardIcon.gameObject.SetActive(false);
                            remoteAssetsIcon.gameObject.SetActive(true);
                            rewardNum.transform.parent.gameObject.SetActive(false);
                        });
                    }
                    else if (attachment.rewardType == BUDRewardType.RewardPgcBundle)
                    {
                        rewardIcon.sprite = PgcUtils.LoadBundleIcon(attachment.rewardId, rewardIcon.gameObject);
                        remoteAssetsIcon.gameObject.SetActive(false);
                        rewardIcon.gameObject.SetActive(true);
                        rewardNum.transform.parent.gameObject.SetActive(false);
                    }
                    else
                    {
                        Sprite rewardSprite = GetSprite(attachment.id, attachment.rewardType);
                        rewardNode.SetActive(true);
                        rewardIcon.sprite = rewardSprite;
                        remoteAssetsIcon.gameObject.SetActive(false);
                        rewardIcon.gameObject.SetActive(true);
                        rewardNum.transform.parent.gameObject.SetActive(false);
                    }

                    bool isClaimed = info.status == 2;
                    bool isSent = info.status == 3;

                    rewardMask.SetActive(isClaimed || isSent);
                    claimedMask.SetActive(isClaimed);
                    SentMask.SetActive(isSent);

                    switch (mailBoxGiftTabType)
                    {
                        case MailBoxGiftTabType.ReceiveGift:
                            refuseButton.gameObject.SetActive(false);
                            sendButton.gameObject.SetActive(false);
                            rewardButton.gameObject.SetActive(true);
                            refuseButton.onClick.RemoveAllListeners();
                            sendButton.onClick.RemoveAllListeners();
                            rewardButton.onClick.RemoveAllListeners();
                            rewardButton.onClick.AddListener(() =>
                            {
                                claimLoadObj.SetActive(true);
                                GiftInvoke((int)GiftButtonType.Accept, info);
                            });

                            break;
                        case MailBoxGiftTabType.RequestGift:
                            refuseButton.gameObject.SetActive(!isSent);
                            sendButton.gameObject.SetActive(true);
                            rewardButton.gameObject.SetActive(false);
                            refuseButton.onClick.RemoveAllListeners();
                            sendButton.onClick.RemoveAllListeners();
                            rewardButton.onClick.RemoveAllListeners();
                            sendButton.onClick.AddListener(() =>
                            {
                                EditMailPanel editMailPanel =
                                    UIManager.Inst.OpenPanel<EditMailPanel>(PanelId.EditMailPanel, null, null, info,
                                        attachment.giftType);
                                editMailPanel.SetSentMailCallback(isSuccess =>
                                {
                                    if (isSuccess)
                                    {
                                        SentMask.SetActive(true);
                                        refuseButton.gameObject.SetActive(false);
                                        sendButton.gameObject.SetActive(false);
                                        rewardMask.SetActive(true);
                                    }
                                });
                            });
                            refuseButton.onClick.AddListener(() => { GiftInvoke((int)GiftButtonType.Refuse, info); });
                            break;
                    }
                }
                SetIsRead(info);
            }
        }

        private Sprite GetSprite(string productId, BUDRewardType rewardType)
        {
            var spriteName = "icon_premium_pass";
            if (productId.Contains("giftgaojipass"))
            {
                spriteName = "icon_premium_pass";
            }
            else if (productId.Contains("gifthaohuapass"))
            {
                spriteName = "icon_deluxe_pass";
            }
            else if (productId.Contains("gifthaohuauppass"))
            {
                spriteName = "icon_deluxe_pass_upgrade";
            }
            else if (productId.Contains("giftvip"))
            {
                spriteName = "icon_vip_month";
            }
            else if (productId.Contains("newYearLimitedPack1"))
            {
                spriteName = "icon_newYearLimitedPack1";
            }
            else if (productId.Contains("newYearLimitedPack2"))
            {
                spriteName = "icon_newYearLimitedPack2";
            }
            else if (productId.Contains("LaborDay"))
            {
                spriteName = "newLaborPack2";
            }
            else if (productId.Contains("newYearLimitedDancingPack"))
            {
                spriteName = "icon_newYearLimitedDancingPack";
            }


            var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
            return sprite;
        }


        private void GiftInvoke(int buttonType, MailInfo mailInfo)
        {
            string mailId = mailInfo.mailId;

            JObject req = new JObject()
            {
                ["mailId"] = mailId,
                ["buttonType"] = buttonType,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MailClick, HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                (_) =>
                {
                    if (buttonType == (int)GiftButtonType.Accept)
                    {
                        OnClaimSuccessCallBack(mailInfo.attachments, mailInfo);
                    }
                    else if (buttonType == (int)GiftButtonType.Refuse)
                    {
                        TipPanel.ShowToast("已拒绝");
                        rightPart.SetActive(false);
                        RequestDataList();
                    }
                },
                (error) => { OnClaimFailCallBack(); });
        }

        private void SetIsRead(MailInfo mailInfo)
        {
            if (mailInfo.status != 0)
            {
                return;
            }

            string mailId = mailInfo.mailId;
            if (string.IsNullOrEmpty(mailId))
            {
                return;
            }
            MailReadReq mailReadReq = new MailReadReq();
            mailReadReq.mailIds = new List<string> { mailId };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MailRead, HttpMethod.POST,
                JsonConvert.SerializeObject(mailReadReq),
                (_) =>
                {
                    mailInfo.status = 1;
                    mailboxEntry?.UpdateSingleItem(mailInfo);
                },
                (error) => { });
        }

        private void OnClaimFailCallBack()
        {
            claimLoadObj.SetActive(false);
            TipPanel.ShowToast("奖励领取失败");
        }

        private void OnClaimSuccessCallBack(List<MailAttachmentsInfo> attachments, MailInfo reqMailInfo)
        {
            claimLoadObj.SetActive(false);
            if (_curShowInfo != null && _curShowInfo.mailId == reqMailInfo.mailId)
            {
                claimedMask.SetActive(true);
                rewardMask.SetActive(true);
            }

            reqMailInfo.status = 2;
            mailboxEntry.UpdateSingleItem(reqMailInfo);
            var rewardList = new List<CommonRewardItemData>();
            for (int i = 0; i < attachments.Count; i++)
            {
                CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
                var attachment = attachments[i];
                switch (attachment.rewardType)
                {
                    case BUDRewardType.RewardPgcResource:
                        {
                            commonRewardItemData.IconSp =
                                PgcUtils.GetIconSpriteByPgcId(attachment.rewardId, rewardIcon.gameObject);
                            GiftMallHandler dataHandler = AssetsDataManager.GetData<GiftMallHandler>();
                            if (dataHandler != null)
                            {
                                string pgcName = dataHandler.GetPgcName(attachment.rewardId);
                                commonRewardItemData.rewardName = pgcName;
                            }

                            break;
                        }
                    case BUDRewardType.RewardPgcBundle:
                        {
                            commonRewardItemData.IconSp =
                                PgcUtils.LoadBundleIcon(attachment.rewardId, rewardIcon.gameObject);
                            commonRewardItemData.rewardName = PgcUtils.GetBundleName(attachment.rewardId);
                            break;
                        }
                    case BUDRewardType.RewardGiftUgc:
                        {
                            commonRewardItemData.IconSp =
                                ConvertTextureToSprite(remoteAssetsIcon.GetTexture());
                            commonRewardItemData.rewardName = attachment.rewardName;
                            break;
                        }
                    default:
                        {
                            Sprite rewardSprite = GetSprite(attachment.id, attachment.rewardType);
                            commonRewardItemData.IconSp =
                                rewardSprite;
                            commonRewardItemData.rewardName = attachment.rewardName;
                            break;
                        }
                }

                commonRewardItemData.RewardAmount = attachments[i].count;
                rewardList.Add(commonRewardItemData);
            }

            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardList);
        }

        Sprite ConvertTextureToSprite(Texture2D texture)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f) // Pivot point (center)
            );
        }

        public void IsEmptyAction()
        {
        }
    }
}