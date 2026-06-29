using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using Game.MusicalInstrument;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class JoinInContestPanel : BasePanel<JoinInContestPanel>
{
    [SerializeField] Text title;
    [SerializeField] Text tip;
    [SerializeField] Button joinInButton;
    [SerializeField] Button closeButton;
    [SerializeField] Transform content;
    [SerializeField] JoinInContestItem itemPrefab;
    [SerializeField] JoinInContestItem successItem;
    [SerializeField] Button jumpButton;

    private JoinInContestItem lastToggle;
    private List<ContestInfo> contests;
    private string creationId;

    public override void OnCreate()
    {
        base.OnCreate();
        joinInButton.onClick.AddListener(OnJoinInClick);
        closeButton.onClick.AddListener(OnCloseClick);
        jumpButton.onClick.AddListener(OnJumpClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        contests = args[0] as List<ContestInfo>;
        creationId = args[1] as string;
        UpdateText();
        for (int i = 0, C = contests.Count; i < C; i++)
        {
            var item = Instantiate(itemPrefab, content);
            item.SetData(contests[i]);
            item.Toggle.onValueChanged.AddListener((isOn) =>
            {
                OnContestToggleChange(isOn, item);
            });
        }
    }

    public void OnContestToggleChange(bool isOn, JoinInContestItem item)
    {
        if (isOn)
        {
            if (lastToggle != null && lastToggle != item) lastToggle.Toggle.isOn = false;
            lastToggle = item;
        }
    }

    private bool isRequesting;
    private void OnJoinInClick()
    {
        if (isRequesting) return;
        if (lastToggle == null)
        {
            return;
        }
        var data = new JoinContestReq()
        {
            contestIds = new List<string>() { lastToggle.ContestId },
            creationId = creationId,
        };
        isRequesting = true;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestJoin, HttpMethod.POST, JsonConvert.SerializeObject(data), (response) =>
        {
            OnJoinInSuccess(lastToggle.Data);
            MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
        }, (fail) =>
        {
            isRequesting = false;
        });
    }

    private void UpdateText()
    {
        if (contests == null || contests.Count == 0)
        {
            return;
        }

        var contest = contests[0];
        var contestText = "皮肤";
        if (contest.CurrentContestType == BUDContestType.Instrument)
        {
            contestText = "乐器";
        } 
        else if (contest.CurrentContestType == BUDContestType.MusicScore)
        {
            contestText = "乐谱";
        }
        else if (contest.CurrentContestType == BUDContestType.Bundle)
        {
            contestText = "皮肤套装";
        }
        else if (contest.CurrentContestType == BUDContestType.PetSkin)
        {
            contestText = "宠物皮肤";
        }
        else if (contest.CurrentContestType == BUDContestType.PetBundle)
        {
            contestText = "宠物皮肤套装";
        }else if (contest.CurrentContestType == BUDContestType.Vehicle)
        {
            contestText = "载具";
        }
        else if (contest.CurrentContestType == BUDContestType.Camera)
        {
            contestText = "摄影";
        }
        title.SetLocalText("参加{0}创作活动", LocalizationManager.Inst.GetLocalizedText(contestText));
        tip.SetLocalText("选择你想参加的{0}活动！记得要符合活动的规则哦！", LocalizationManager.Inst.GetLocalizedText(contestText));
    }

    private void OnCloseClick()
    {
        CloseSelf();
    }

    private void OnJoinInSuccess(ContestInfo Data)
    {
        content.parent.parent.gameObject.SetActive(false);
        joinInButton.gameObject.SetActive(false);
        successItem.gameObject.SetActive(true);
        jumpButton.gameObject.SetActive(true);

        title.SetLocalText( "活动参与成功");
        tip.SetLocalText("恭喜你刚刚成功参加了<color=#AD57FF>{0}活动</color>！", lastToggle.Data.contestName);
        successItem.SetData(Data);
    }

    private bool isFromEdit = false;
    public void ShowJoinSuccess(ContestInfo Data)
    {
        isFromEdit = true;
        content.parent.parent.gameObject.SetActive(false);
        joinInButton.gameObject.SetActive(false);
        successItem.gameObject.SetActive(true);
        jumpButton.gameObject.SetActive(true);

        title.SetLocalText( "活动参与成功");
        tip.SetLocalText("恭喜你刚刚成功参加了<color=#AD57FF>{0}活动</color>！", Data.contestName);
        successItem.SetData(Data);
    }

    private void OnJumpClick()
    {
        CloseSelf();
        Debug.LogError("OnJumpClick");
        if (isFromEdit)
        {

            if (UIManager.Inst.TryFindPanel(PanelId.MusicScoreStudioPanel, out MusicScoreStudioPanel panel1))
            {
                panel1.CloseSelf();
            }

            if (UIManager.Inst.TryFindPanel(PanelId.MusicalInstrumentStudioPanel, out MusicalInstrumentStudioPanel panel2))
            {
                panel2.CloseSelf();
            }

            if (UIManager.Inst.TryFindPanel(PanelId.AvatarStudioMainPanel, out AvatarStudioMainPanel panel3))
            {
                panel3.CloseSelf();
            }

            if (UIManager.Inst.TryFindPanel(PanelId.CameraAllPhotoPanel, out CameraAllPhotoPanel panel4))
            {
                panel4.CloseSelf();
            }

            if (UIManager.Inst.TryFindPanel(PanelId.ClothContestPanel, out ContestBaseDetailPanel panel))
            {
                panel.ShowMyEntry();
            }
        }
        else
        {
            ContestEventManager.Inst.OpenContestPage(lastToggle.ContestId);
        }
    }
}

public class JoinContestReq
{
    public List<string> contestIds;
    public string creationId;
    public string poseId;
    public string backgroundUrl;
    public string cover;
    public int setType;
    public string oldCreation;
}
