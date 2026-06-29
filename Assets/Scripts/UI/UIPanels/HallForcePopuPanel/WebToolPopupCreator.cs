using System;
using UnityEngine;
using View.UI.PopupPanelSystem.Base.Core;
using View.UI.PopupPanelSystem.Data;
using View.UI.PopupPanelSystem.ExtendsPopups;

namespace View.UI.PopupPanelSystem.ExtendsCreators
{
    public class WebToolPopupCreator : BasePopupCreator
    {
        // 定义需要特殊处理的弹窗ID列表
        private static readonly int[] CustomPopupIds = { 9998 }; // 新手7天登录礼ID
        public WebToolPopupCreator(PopupPanelManager context) : base(context)
        {
        }

        public override void Release()
        {
        }

        public override void ParseDataAndCreatePopup(PopupRspData data)
        {
            if (data.popupList is not { Count: > 0 }) return;
            int i = 0;
            foreach (var newsData in data.popupList)
            {
                i++;
                bool isLastPopup = (i == data.popupList.Count);
                if (newsData != null)
                {
                    var copyData = newsData.Clone();
                    if (IsCustomPopup(copyData.popupId))
                    {
                        switch (copyData.popupId)
                        {
                            case 9999:
                                var accurateRecommendationPopPanel = UIManager.Inst.OpenPanelTakeAni<AccurateRecommendationPopPanel>(PanelId.AccurateRecommendationPopPanel);
                                if (accurateRecommendationPopPanel != null)
                                {
                                    accurateRecommendationPopPanel.SetData(copyData);
                                    accurateRecommendationPopPanel.OnPanelClose = () => {
                                        Context.SchedulerSystem.PlayNext();
                                        if (isLastPopup)
                                        {
                                            OnLastPopupClosed();
                                        }
                                    };
                                }
                                break;
                            case 9998:
                                var newbietaskpanel = UIManager.Inst.OpenPanelTakeAni<NewBieSevenDayV2TaskPanel>(PanelId.NewBieSevenDayV2TaskPanel);
                                if (newbietaskpanel != null)
                                {
                                    newbietaskpanel.closeBtn.onClick.AddListener(() => {
                                        Context.SchedulerSystem.PlayNext();
                                        if (isLastPopup)
                                        {
                                            // 在这里添加最后一个弹窗关闭时的逻辑
                                            OnLastPopupClosed();
                                        }
                                    });
                                }
                                break;
                        }
                    }
                    else
                    {
                        // 非特殊ID的处理
                        var popup = new WebToolPopup(Context, copyData);
                        if (isLastPopup)
                        {
                            // 为普通弹窗添加关闭回调
                            popup.CloseCallBack += () =>
                            {
                                // 在这里添加最后一个弹窗关闭时的逻辑
                                OnLastPopupClosed();
                            };
                        }
                        Context.SchedulerSystem.AddSchedule(popup);
                        Context.ImagePreLoaderSystem.AddPreloadImage(copyData.coverUrl);
                    }
                }
            }
        }

        private void OnLastPopupClosed()
        {
            if (!PlayerPrefs.HasKey("FirstOpenBreakIceNew"+AccountDataManager.Inst.Uid)) {
                var gameHallpanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow, PanelId.GameHallPanel);
                gameHallpanel.ChangBtAlpha(1, "BtnNewbieV2");
            }
        }
        // 检查是否是需要特殊处理的弹窗ID
        private bool IsCustomPopup(int popupId)
        {
            foreach (var id in CustomPopupIds)
            {
                if (id == popupId)
                    return true;
            }
            return false;
        }
    }
}