using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Game.COSXML.Utils;
using GameData.BaseInfo;
using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static Pb.Game.AIGameAmusementParkSyncReply.Types;

namespace AIGame.Base
{
    [Serializable]
    public class AIParkGameBookPanelToggle
    {
        public Toggle Tog;
        public Text NameTxt;
        public int Type;

        [HideInInspector] public StoryEvent StoryEvent;
        [HideInInspector] public string Summary;
    }
    public class AIParkGameBookPanel : BasePanel<AIParkGameBookPanel>
    {
        public CButton CloseBtn;

        public Transform StartGroup;
        public Text StartDescTxt;

        public Transform EndingGroup;
        public Text EndingDescTxt;

        public Transform EventGroup;
        public Transform Content;
        public Text EventNameTxt;
        public Text EventTimeTxt;
        public Text EventTypeTxt;
        public Text EventDescTxt;
        public Text EventEndTxt;

        public RemoteImageBehaviour Paster;

        public RectTransform BgRect;

        public List<AIParkGameBookPanelToggle> Toggles;

        public List<Image> UgcImage = new();

        public List<Image> UgcImage2 = new();

        public override void OnCreate()
        {
            base.OnCreate();
            CloseBtn.onClick.AddListener(OnCloseBtn);


            foreach (var item in UgcImage)
            {
                item.ParkBookImageColor1();
            }

            foreach (var item in UgcImage2)
            {
                item.ParkBookImageColor2();
            }

            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.book != null)
            {
                for (int i = 0; i < AIParkGameUgcsetTool.gameConfig.book.pasterUrls.Count; i++)
                {
                    var item = AIParkGameUgcsetTool.gameConfig.book.pasterUrls[i];
                    var v = GameObject.Instantiate(Paster, BgRect).GetComponent<RemoteImageBehaviour>();
                    v.Load(item.url);
                    (v.transform as RectTransform).anchoredPosition = new Vector2(item.x, item.y);
                    v.transform.localScale = Vector3.one * item.scale;
                    v.gameObject.SetActive(true);
                }
            }
        }
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;

            var events = AIParkUtils.Inst.ParkGameData.events;

            for (int i = 0; i < Toggles.Count; i++)
            {
                var tog = Toggles[i];
                tog.Tog.onValueChanged.AddListener((succ) => { OnToggle(tog, succ); });
                switch (tog.Type)
                {
                    case 1:
                        tog.Tog.gameObject.SetActive(true);
                        if (!string.IsNullOrEmpty(AIParkGameUgcsetTool.gameConfig.plot))
                        {
                            StartDescTxt.text = AIParkGameUgcsetTool.gameConfig.plot;
                        }
                        else
                        {
                            StartDescTxt.text = AIParkGuidePanel.ugcContent + "\n" + AIParkGuidePanel.ugcContent1;
                        }
                        break;
                    case 2:
                        var idx = i - 1;
                        if (idx < events.Count)
                        {
                            tog.Tog.gameObject.SetActive(true);
                            if (idx < sceneIdx)
                            { 
                                tog.StoryEvent = events[idx];
                                tog.NameTxt.text = $"事件{i}:{tog.StoryEvent.EventOpts[0].EventName}";

                                if (AIParkUtils.Inst.IsEnterGameSummary() && tog.StoryEvent.EventType == 1)
                                {
                                    var sumStr = $"{tog.StoryEvent.EventOpts[0].EventQuestion}\n" + $"你的选项是\"【{AIParkTcpNetMgr.Instance.curSelectEvent}】\"";
                                    tog.Summary = sumStr;
                                }
                                else
                                {
                                    if ((idx < AIParkUtils.Inst.ParkGameData.EventSummaryList.Count && idx < sceneIdx - 1))
                                    {
                                        tog.Summary = AIParkUtils.Inst.ParkGameData.EventSummaryList[idx];
                                    }
                                }

                            }
                            else
                            {
                                tog.StoryEvent = null;
                                tog.NameTxt.text = $"事件{i}:?????";
                            }
                        }
                        else
                        {
                            tog.Tog.gameObject.SetActive(false);
                        }
                        break;
                    case 3:
                        if (AIParkUtils.Inst.IsEnterGameSummary())
                        {
                            tog.Tog.gameObject.SetActive(true);
                            EndingDescTxt.text = AIParkUtils.Inst.ParkGameData.Summary;
                        }
                        else
                        {
                            tog.Tog.gameObject.SetActive(false);
                        }
                        break;
                    default:
                        break;
                }
            }

            if (AIParkUtils.Inst.IsEnterGameSummary())
            {
                Toggles[Toggles.Count - 1].Tog.isOn = true;
                OnToggle(Toggles[Toggles.Count - 1], true);
            }
            else
            {
                Toggles[sceneIdx].Tog.isOn = true;
                OnToggle(Toggles[sceneIdx], true);
            }
        }

        void OnToggle(AIParkGameBookPanelToggle tog, bool bo)
        {
            if (bo)
            {
                StartGroup.gameObject.SetActive(false);
                EventGroup.gameObject.SetActive(false);
                EndingGroup.gameObject.SetActive(false);
                switch (tog.Type)
                {
                    case 1:
                        StartGroup.gameObject.SetActive(true);
                        break;
                    case 2:
                        EventGroup.gameObject.SetActive(true);
                        if (tog.StoryEvent != null)
                        {
                            EventNameTxt.text = tog.StoryEvent.EventOpts[0].EventName;
                            if (tog.StoryEvent.EventType == 1)
                            {
                                EventTypeTxt.text = "选择事件";
                            }
                            else
                            {
                                EventTypeTxt.text = "普通事件";
                            }
                            EventDescTxt.text = tog.StoryEvent.EventOpts[0].EventDesc;
                            EventTimeTxt.text = GetTimeStr(tog.StoryEvent.EventOpts[0].EventDuration);
                            EventEndTxt.text = tog.Summary;
                        }
                        else
                        {
                            EventNameTxt.text = "";
                            EventTypeTxt.text = "";
                            EventDescTxt.text = "";
                            EventTimeTxt.text = "";
                            EventEndTxt.text = "";
                        }
                        LayoutRebuilder.ForceRebuildLayoutImmediate(EventDescTxt.transform.parent as RectTransform);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(EventEndTxt.transform.parent as RectTransform);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(Content as RectTransform);
                        break;
                    case 3:
                        EndingGroup.gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnCloseBtn()
        {
            CloseSelf();
        }



        string GetTimeStr(int seconds)
        {
            var hour = seconds / 3600;
            var minute = seconds % 3600 / 60;
            var second = seconds % 3600 % 60;

            var str = "";
            if (hour > 0)
            {
                str += (hour + "时");
            }
            if (minute > 0)
            {
                str += (minute + "分");
            }
            if (second > 0)
            {
                str += (second + "秒");
            }
            return str;
        }
    }
}