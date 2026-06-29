using AIGame.Base;
using GameData.Manager;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static Pb.Game.AIGameAmusementParkSyncReply.Types;

public class AIParkGuestGroup : MonoBehaviour
{
    public CButton EventChooseBtn;
    public CButton BookBtn;
    public CButton AvatarBtn;
    public Transform AvatarNew;

    public Transform EventTargetBg;
    public Text EventTargetTxt;

    public Transform Notice;
    public Text NoticeTxt;

    public Transform CurEventBg;
    public Text CurEventTxt;
    public Text CountdownTxt;

    public AIParkGameMiniMap MiniMap;

    public List<ImageGradient> UgcImage2 = new();

    public List<Text> Text = new();

    private BudTimer BudTimer;

    private List<string> targetLs = new List<string>() {
        "和游乐园内的各位交流认识一下吧",
        "询问一下各位对本次事件有什么看法",
        "选择你心里认为的捣蛋鬼吧！"
    };
    private void Awake()
    {
        EventChooseBtn.onClick.AddListener(OnEventChooseBtn);
        BookBtn.onClick.AddListener(OnBookBtn);

        Color color = Color.white;
        if (AIParkGameUgcsetTool.GetParkPopImageColor1(ref color))
        {
            foreach (var item in UgcImage2)
            {
                item.color2 = color;
            }

            foreach (var item in Text)
            {
                item.color = color;
            }
        }



        Notice.gameObject.SetActive(false);
        CurEventBg.gameObject.SetActive(false);
        EventTargetBg.gameObject.SetActive(false);
        EventChooseBtn.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (BudTimer != null)
        {
            TimerManager.Inst.Stop(BudTimer);
            BudTimer = null;
        }
    }

    public void ShowEvent()
    {
        var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;

        var storyEvent = AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1];

        EventChooseBtn.gameObject.SetActive(false);
        if (storyEvent != null)
        {
            EventTargetBg.gameObject.SetActive(true);
            var idx = sceneIdx - 1;
            if (AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter)
            {
                EventTargetTxt.text = "当前事件目标：" + targetLs[sceneIdx - 1];
            }
            else
            {
                var eventCfg = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.AICommonGameConfig.events;
                if (eventCfg != null && idx < eventCfg.Count)
                {
                    EventTargetTxt.text = "当前事件目标：" + eventCfg[idx].target;
                }
                else
                {
                    EventTargetTxt.text = "当前事件目标";
                }
            }


            CurEventBg.gameObject.SetActive(true);
            CurEventTxt.text = "当前事件：" + storyEvent.EventOpts[0].EventName;

            if (storyEvent.EventType == 1)
            {
                EventChooseBtn.gameObject.SetActive(true);
            }
        }

    }

    public void ShowCustomEvent(string eventName)
    {
        EventTargetBg.gameObject.SetActive(true);
        EventTargetTxt.text = "当前目标:" + eventName;
    }

    public void HideEvent()
    {
        EventTargetBg?.gameObject?.SetActive(false);
        CurEventBg?.gameObject?.SetActive(false);
    }

    public void ShowNotice(string notice)
    {
        if (BudTimer != null)
        {
            TimerManager.Inst.Stop(BudTimer);
            BudTimer = null;
        }
        Notice.gameObject.SetActive(true);
        NoticeTxt.text = notice;
        BudTimer = TimerManager.Inst.RunOnce("ShowNotice", 2, () =>
        {
            Notice.gameObject.SetActive(false);
        });
    }

    public void HideChooseBtn()
    {
        EventChooseBtn.gameObject.SetActive(false);
    }

    private void OnEventChooseBtn()
    {
        var sceneIdx = AIParkUtils.Inst.ParkCustomData.SceneIndex;

        // var storyEvent = AIParkUtils.Inst.ParkGameData.events[sceneIdx-1];

        var be = SimpleGameDurationManager.Inst.GetDurationBehaviour();
        int leftTime = be == null ? 0 : (int)be.leftTime;

        for (int i = 0; i < AIParkUtils.Inst.ParkGameData.events.Count; i++)
        {
            if (AIParkUtils.Inst.ParkGameData.events[i].EventType == 1)
            {
                var panel = UIManager.Inst.OpenPanel<AIParkGameEventPanel>(PanelId.AIParkGameEventPanel, WindowId.GuestWindow, AIParkUtils.Inst.ParkGameData.events[i],
                    AIParkUtils.Inst.ParkGameData.events[i].EventOpts[0], leftTime);
                panel.ShowChoose();
                break;
            }
        }

    }

    private void OnBookBtn()
    {
        UIManager.Inst.OpenPanel(PanelId.AIParkGameBookPanel);
    }
}