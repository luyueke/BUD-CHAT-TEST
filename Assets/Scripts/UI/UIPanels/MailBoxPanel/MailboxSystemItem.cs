using UnityEngine;
using System;
using Basic.Utils;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine.U2D;

namespace BUD.MailBox
{
    public class MailboxSystemItem : MonoBehaviour
    {
        private SuperTextMesh titleText;
        private Text senderText;
        private Text expiresText;
        private GameObject select;
        private GameObject normal;
        private CButton button;
        private GameObject rewardNode;
        private Image rewardIcon;
        private Text rewardNum;
        private MailInfo _info;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            titleText = GameObjectEx.FindChildByName(transform, "Title").GetComponent<SuperTextMesh>();
            senderText = GameObjectEx.FindChildByName(transform, "Sender").GetComponent<Text>();
            expiresText = GameObjectEx.FindChildByName(transform, "Expires").GetComponent<Text>();
            select = GameObjectEx.FindChildByName(transform, "Select").gameObject;
            normal = GameObjectEx.FindChildByName(transform, "Normal").gameObject;
            button = GameObjectEx.FindChildByName(transform, "button").GetComponent<CButton>();
            rewardNode = GameObjectEx.FindChildByName(transform, "prizeIcon").gameObject;
            rewardIcon = GameObjectEx.FindChildByName(transform, "RewardIcon").GetComponent<Image>();
            rewardNum = GameObjectEx.FindChildByName(transform, "RewardNum").GetComponent<Text>();
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
                }
            );
        }

        public void UpDataItemData(MailInfo info)
        {
            titleText.SetText(info.title);
            expiresText.SetLocalText("{0}天过期", expireTime(info.sendTime, 30));
            if (info.sender != null)
            {
                senderText.SetText(info.sender.name);
            }

            MailAttachmentsInfo attachment = null;
            if (info.attachments != null)
            {
                foreach (var item in info.attachments)
                {
                    if (item != null && item.rewardType == BUDRewardType.RewardPgcResource || GameUtils.IsCurrencyType((int)item.rewardType))
                    {
                        attachment = item;
                        break;
                    }
                }
            }

            if (attachment != null)
            {
                //var attachment = info.attachments[0];
                if (attachment.rewardType == BUDRewardType.RewardPgcResource)
                {
                    rewardNode.SetActive(true);
                    rewardIcon.sprite = PgcUtils.GetIconSpriteByPgcId(attachment.rewardId, rewardIcon.gameObject);
                    rewardNum.transform.parent.gameObject.SetActive(false);
                }
                else if (GameUtils.IsCurrencyType((int)attachment.rewardType))
                {
                    rewardNode.SetActive(true);
                    rewardIcon.sprite = PgcUtils.LoadCurrencyIcon(GameUtils.ConvertRewardType((int)attachment.rewardType), rewardIcon.gameObject);
                    rewardNum.transform.parent.gameObject.SetActive(true);
                    rewardNum.SetText(attachment.count.ToString());
                }
            }
            else
            {
                rewardNode.SetActive(false);
            }
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