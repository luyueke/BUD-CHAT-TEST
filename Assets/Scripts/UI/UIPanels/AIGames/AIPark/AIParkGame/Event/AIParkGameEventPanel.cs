using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using GameData.BaseInfo;
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

    public class AIParkGameEventPanel : BasePanel<AIParkGameEventPanel>
    {
        public CButton CloseBtn;

        public Text TitleTxt;
        public Text NameTxt;

        public Transform Desc;
        public Text DescTxt;
        public Text ChooseTipsTxt;

        public Transform Choose;
        public CButton ConfirmBtn;
        public Transform GrayBtn;
        public Text ConfirmTxt;
        public List<Toggle> Toggles;

        public Transform Ending;
        public Text EndingTxt;

        public RemoteImageBehaviour Paster;

        public RectTransform BgRect;

        public List<Image> UgcImage = new();

        public List<Image> UgcImage2 = new();

        public List<Text> Text = new();

        public List<Text> Text2 = new();

        [HideInInspector] public StoryEventDetail storyEventDetail;
        [HideInInspector] public StoryEvent storyEvent;

        private BudTimer timer;
        private int time;
        private int idx;
        int EventDuration { get { return storyEvent.EventOpts[0].EventDuration; } }
        public override void OnCreate()
        {
            base.OnCreate();
            CloseBtn.onClick.AddListener(OnCloseBtn);
            ConfirmBtn.onClick.AddListener(OnConfirmBtn);
            for (int i = 0; i < Toggles.Count; i++)
            {
                var idx = i;
                Toggles[i].onValueChanged.AddListener((succ) => { OnTog(idx, succ); });
            }

            foreach (var item in UgcImage)
            {
                item.ParkPopImageColor1();
            }

            foreach (var item in UgcImage2)
            {
                item.ParkPopImageColor2();
            }

            foreach (var item in Text)
            {
                item.ParkPopTextColor();
            }

            Color color = Color.white;
            if (AIParkGameUgcsetTool.GetParkPopImageColor2(ref color))
            {
                foreach (var item in Text2)
                {
                    item.color = color;
                }
            }


            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.gamePop != null)
            {
                for (int i = 0; i < AIParkGameUgcsetTool.gameConfig.gamePop.pasterUrls.Count; i++)
                {
                    var item = AIParkGameUgcsetTool.gameConfig.gamePop.pasterUrls[i];
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

            storyEvent = args[0] as StoryEvent;

            storyEventDetail = args[1] as StoryEventDetail;

            time = (int)args[2];

            Desc.gameObject.SetActive(false);
            Choose.gameObject.SetActive(false);
            Ending.gameObject.SetActive(false);
            if (timer != null)
            {
                TimerManager.Inst.Stop(timer);
                timer = null;
            }
            if (storyEvent.EventType == 0)
            {
                ShowDesc();
            }
            else
            {
                ShowChoose();
            }
        }

        protected override void OnDisable()
        {
            if (timer != null)
            {
                TimerManager.Inst.Stop(timer);
                timer = null;
            }
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (timer != null)
            {
                TimerManager.Inst.Stop(timer);
                timer = null;
            }
            base.OnDestroy();
        }

        public void ShowDesc()
        {
            Desc.gameObject.SetActive(true);
            TitleTxt.text = "事件名：" + storyEvent.EventOpts[0].EventName;
            NameTxt.text = "事件描述：";
            DescTxt.text = storyEvent.EventOpts[0].EventDesc;
            if (storyEvent.EventType == 1)
            {
                ChooseTipsTxt.text = "本次事件为选择事件，需要在限时结束前完成选择。";
            }
            else
            {
                ChooseTipsTxt.text = "本次事件为普通事件，倒计时结束后本次事件结束。";
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(DescTxt.transform.parent as RectTransform);
        }

        public void ShowChoose()
        {
            Choose.gameObject.SetActive(true);

            if (EventDuration - time <= 20)
            {
                GrayBtn.gameObject.SetActive(true);
                ConfirmTxt.text = $"{20 - EventDuration + time}S";
            }
            else
            {
                GrayBtn.gameObject.SetActive(false);
                ConfirmTxt.text = "确定";
            }

            TitleTxt.text = "选择事件";
            NameTxt.text = storyEventDetail.EventQuestion;

            foreach (var item in Toggles)
            {
                item.gameObject.SetActiveValid(false);
            }
            for (int i = 0; i < storyEventDetail.EventAnswers.Count; i++)
            {
                if (i < Toggles.Count)
                {
                    Toggles[i].gameObject.SetActiveValid(true);
                    Toggles[i].transform.GetChild(2).GetComponent<Text>().text = storyEventDetail.EventAnswers[i];
                }
            }

            TimerManager.Inst.Stop(timer);
            if (time > 0)
            {
                timer = TimerManager.Inst.Run("AIParkGameEventPanel", 0f, 1f, OnTimer);
            }
        }

        public void ShowEnding()
        {
            Ending.gameObject.SetActive(true);

            TitleTxt.text = "事件名" + storyEvent.EventOpts[0].EventName;
            NameTxt.text = "事件结果：";

            EndingTxt.text = storyEvent.EventOpts[0].EventDesc;
        }

        private void OnTimer()
        {
            time -= 1;
            if (time <= 0)
            {
                TimerManager.Inst.Stop(timer);
                timer = null;
                return;
            }
            if (EventDuration - time <= 20)
            {
                GrayBtn?.gameObject?.SetActiveValid(true);
                if(ConfirmTxt != null){
                    ConfirmTxt.text = $"{20 - EventDuration + time}S";
                }
            }
            else
            {
                GrayBtn?.gameObject?.SetActiveValid(false);
                if(ConfirmTxt != null){
                    ConfirmTxt.text = "确定";
                }
            }
        }

        private void OnTog(int _idx, bool _bo)
        {
            if (_bo)
            {
                idx = _idx;
            }
        }

        private void OnConfirmBtn()
        {
            if (GrayBtn.gameObject.activeSelf)
            {
                TipPanel.ShowToast($"{20 - EventDuration + time}后可以开始选择");
            }
            else
            {
                var choose = storyEventDetail.EventAnswers[idx];
                TipPanel.ShowToast($"选择了" + choose);
                AIParkTcpNetMgr.Instance.SendAIParkSyncReq_SelectedEvent(choose);

                AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
                aiGame.OnSendSelectEvent();
                TimerManager.Inst.Stop(timer);
                CloseSelf();
            }
        }

        public override void OnHidden()
        {
            TimerManager.Inst.Stop(timer);
            base.OnHidden();
        }

        private void OnCloseBtn()
        {
            if (storyEvent?.EventType == 1 && time <= 0)
            {
                TipPanel.ShowToast($"请先完成选择");
            }
            else
            {
                TimerManager.Inst.Stop(timer);
                CloseSelf();
                AIParkGuideMgr.Inst.CheckNormalEventGuide();
            }
        }
    }
}