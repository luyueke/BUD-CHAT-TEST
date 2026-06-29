using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.MailBox
{
    public class MailBoxNotificationItem : MonoBehaviour
    {
        private Text titleText;
        private CButton propBtn;
        private NotificationInfo _info;
        [SerializeField]private CButton headBtn;
        [SerializeField]private FollowButton followButton;
        [SerializeField]private HeadViewWidget HeadViewWidget;
        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            titleText = GameObjectEx.FindChildByName(transform, "Title").GetComponent<Text>();
            propBtn = GameObjectEx.FindChildByName(transform, "PropBtn").GetComponent<CButton>();
        }
        public void Init(MailboxSubType mailboxSubType, Action<NotificationInfo> onPropClick, NotificationInfo info)
        {
            if (mailboxSubType == MailboxSubType.Interactive)
            {
                HeadViewWidget.gameObject.SetActive(true);
                HeadViewWidget.InitHeadCycle(info.sender.uid, info.sender.portraitUrl, info.sender.avatarFrame);
            }
            else if(mailboxSubType == MailboxSubType.Check)
            {
                HeadViewWidget.gameObject.SetActive(false);
            }
            
            _info = info;
            info.text = $"<i>{info.text}";
            titleText.SetText($"{DataUtil.RemoveRichTextAndEmoji(info.text)}<size=36><color=#9E9E9E> {DataUtil.ToPublishTimeString(info.timestamp)}</color></size>");
            propBtn.gameObject.SetActive(false);
            followButton.gameObject.SetActive(false);
            headBtn.onClick.RemoveAllListeners();
            if (info.sender != null)
            {
                headBtn.onClick.AddListener(()=>UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, info.sender.uid));
            }
            if (info.info!=null)
            {
                propBtn.gameObject.SetActive(true);
                propBtn.onClick.RemoveAllListeners();
                propBtn.onClick.AddListener(()=>onPropClick?.Invoke(_info));
            }
            else if (info.relation!=null&&info.sender!=null)
            {

                followButton.gameObject.SetActive(true);
                followButton.SetRelation(info.sender.uid,ToRelationShipData(info.relation), (relationStatus) =>
                {
                    if (info!=null&&info.relation!=null)
                    {
                        info.relation.status = relationStatus;
                    }
                });
            }

            float maxWidth = 1520;
            if (propBtn.gameObject.activeSelf) {
                maxWidth = 1360f;
            } else if (followButton.gameObject.activeSelf) {
                maxWidth = 1190f;
            }

            if (titleText.preferredWidth > maxWidth) {
                titleText.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                titleText.rectTransform.sizeDelta = new Vector2(maxWidth, titleText.rectTransform.sizeDelta.y);
            } else {
                titleText.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }


        }

        private RelationShipInfo ToRelationShipData(NotificationRelation relation)
        {
            return new RelationShipInfo()
            {
                relationStatus = relation.status,
                relationShip = (int)RelationShipType.Follow
            };
        }
    }
}
