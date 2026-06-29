using GameData.BaseInfo;
using GameUI;
using System.Collections;
using System.Collections.Generic;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.MailBox
{
    public class MailBoxNotificationView : MailBoxBaseView
    {
        [SerializeField]private NotificationEntry notificationEntry;
        [SerializeField]private Text EmptyText;
        [SerializeField]private MailboxSubType _mailboxSubType;
        protected override void Init()
        {
           
            RequestDataList();
        }
        private void RequestDataList()
        {
            EmptyText.gameObject.SetActive(true);
            EmptyText.SetLocalText("加载中...");
            notificationEntry.SetActions(OnPropClick, _mailboxSubType);
            notificationEntry.GetFirstPageDatas(GetPageDatas);
        }
        private void GetPageDatas(List<NotificationInfo> infos)
        {
            if (infos!=null&&infos.Count>0)
            {
                EmptyText.gameObject.SetActive(false);
            }
            else
            {
                EmptyText.SetLocalText("你没有收到邮件");
            }
        }
        protected void OnPropClick(NotificationInfo info)
        {
            if (info.info!=null)
            {
                switch (info.info.bizType)
                {
                    case (int)NotificationBizInfo.BizType.Map:
                        if (info.info.gameType == (int)GameType.Normal)
                        {
                            UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, info.info.bizId);
                        }
                        else if (info.info.gameType == (int)GameType.AIGame)
                        {
                            GameEntrySystem.Inst.MapInfoReq(info.info.bizId, (mapinfo) => {
                                if (mapinfo.mapInfo.gameSetting.aiGameId == (int)PGCGameType.AIPark)
                                {
                                    GameEntrySystem.Inst.OpenParkDetailPanel(info.info.bizId);
                                }
                                else
                                {
                                    UIManager.Inst.SwapPanel(PanelId.AIHospitalUgcMapInfoPanel, info.info.bizId);
                                }
                            });
                        }
                        break;
                    case (int)NotificationBizInfo.BizType.Skin:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin ,info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.Prop:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Prop ,info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.Mat:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Mat ,info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.MusicScore:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicScore ,info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.MusicTone:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicTone ,info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.UgcAnim:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcAnim, info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.UgcPose:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcPose, info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.UgcAnimMusic:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcAnimMusic, info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.NPC:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc, info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.Vehicle:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Vehicle, info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.Theatre:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Theatre, info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.CabinCharacter:
                        CabinRolesNetManager.Inst.OpenPanelWithFreshData(info.info.bizId);
                        break;
                    case (int)NotificationBizInfo.BizType.CabinTone:
                        UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CabinTone, info.info.bizId);
                        break;
                }
            }
            
        }
    }
}