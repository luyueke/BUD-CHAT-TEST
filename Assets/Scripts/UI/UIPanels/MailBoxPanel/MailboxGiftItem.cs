using UnityEngine;
using System;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine.UI;
using UI.BaseWidgets;
using UI.Manager;

namespace BUD.MailBox
{
    public class MailboxGiftItem : MonoBehaviour
    {
        private Text titleText;
        private Text senderText;
        private Text expiresText;
        private GameObject select;
        private GameObject normal;
        private CButton button;
        private GameObject rewardNode;
        private Image rewardIcon;
        private Text rewardNum;
        private MailInfo _info;
        private RemoteImageBehaviour remoteAssetsIcon;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            titleText = GameObjectEx.FindChildByName(transform, "Title").GetComponent<Text>();
            senderText = GameObjectEx.FindChildByName(transform, "Sender").GetComponent<Text>();
            expiresText = GameObjectEx.FindChildByName(transform, "Expires").GetComponent<Text>();
            select = GameObjectEx.FindChildByName(transform, "Select").gameObject;
            normal = GameObjectEx.FindChildByName(transform, "Normal").gameObject;
            button = GameObjectEx.FindChildByName(transform, "button").GetComponent<CButton>();
            rewardNode = GameObjectEx.FindChildByName(transform, "prizeIcon").gameObject;
            rewardIcon = GameObjectEx.FindChildByName(transform, "RewardIcon").GetComponent<Image>();
            rewardNum = GameObjectEx.FindChildByName(transform, "RewardNum").GetComponent<Text>();
            remoteAssetsIcon = GameObjectEx.FindChildByName(transform, "RemoteIcon")
                .GetComponent<RemoteImageBehaviour>();
        }

        public void Init(Action<MailInfo> onSelect, MailInfo model)
        {
            _info = model;
            UpDataItemData(model);
            ShowType(model);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onSelect?.Invoke(_info);
            });
        }

        public void UpDataItemData(MailInfo info)
        {
            titleText.SetText(info.title);
            expiresText.SetLocalText("{0}天过期", expireTime(info.sendTime, 30));
            if (info.sender != null)
            {
                senderText.SetText(info.sender.name);
            }

            if (info.attachments != null && info.attachments.Count > 0 && info.attachments[0] != null)
            {
                var attachment = info.attachments[0];
                if (attachment.rewardType == BUDRewardType.RewardPgcResource)
                {
                    rewardNode.SetActive(true);
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
                    rewardNode.SetActive(true);
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
            }
            else
            {
                rewardNode.SetActive(false);
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

        //返回多少天到期
        private int expireTime(long timeStamp, int expireDays)
        {
            int secondPerDay = 60 * 60 * 24;
            int expireDaysSec = expireDays * secondPerDay;
            timeStamp = expireDaysSec + timeStamp;
            long currentSec = GameUtils.GetTimeStamp();
            int day = (int)((timeStamp - currentSec) / secondPerDay);
            return Math.Abs(day);
        }

        private void ShowType(MailInfo info)
        {
            select.SetActive(info.isSelect);
            normal.SetActive(info.status == 0);

        }
    }
}