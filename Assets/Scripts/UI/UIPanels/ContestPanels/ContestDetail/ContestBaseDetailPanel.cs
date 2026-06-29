
using System.Collections.Generic;
using Basic.Utils;
using Game.MusicalInstrument;
using GameData;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class ContestBaseDetailPanel : BasePanel<ContestBaseDetailPanel>
{
    public enum ViewType
    {
        AllEntries,
        WinningEntries,
        MyEntry,
        Top100,
        Winners,
        LuckyDraw
    }

    [SerializeField] private ContestCommonComponent ContestCommonUI;
    [SerializeField] private ContestRankView ContestRankView;
    [SerializeField] private LuckyDrawView LuckyDrawView;
    [SerializeField] private List<Toggle> ToggleList;
    [SerializeField] private Button JoinBtn;
    [SerializeField] private Transform EntryRoot;

    /// <summary>
    ///  当前显示的类型
    /// </summary>
    public ViewType curView;
    public ViewType JumpType = ViewType.AllEntries;

    protected Dictionary<ViewType, GameObject> viewDict = new Dictionary<ViewType, GameObject>();
    protected ContestInfo contestInfo;

    public string ContestId => contestInfo?.contestId;

    public override void OnShow(params object[] args)
    {
        ContestInfo contestInfo = args[0] as ContestInfo;
        if (contestInfo == null)
        {
            return;
        }
        Init(contestInfo);
        RefreshFromServer(contestInfo);
    }

    private void Init(ContestInfo info)
    {
        contestInfo = info;
        ContestCommonUI.SetCommonInfo(info);
        SyncToggleViewStatus();
    }

    private void RefreshFromServer(ContestInfo info)
    {
        var contestId = info?.contestId;
        if (string.IsNullOrEmpty(contestId))
        {
            return;
        }

        ContestDataManager.Inst.RefreshContestInfo(contestId, (b, contestInfo) =>
        {
            if (!b || this == null)
            {
                return;
            }

            RefreshUI(contestInfo);
        });
    }

    private void RefreshUI(ContestInfo contestInfo)
    {
        this.contestInfo = contestInfo;
        SyncToggleViewStatus();
    }

    protected override void Awake()
    {
        base.Awake();
        InitBtn();
        RefreshCurView();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

         viewDict.Clear();
    }

    protected void InitBtn()
    {
        for (int i = 0, C = ToggleList.Count; i < C; i++)
        {
            int index = i;
            ToggleList[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    if ((int)curView != index) SwitchView((ViewType)index);
                }
            });
        }
        ContestCommonUI.AddBackButtonListener(() =>
        {
            OnCloseBtnClick();
        });
        JoinBtn.onClick.AddListener(() =>
        {
            OnJoinBtnClick();
        });
    }

    public virtual void OnCloseBtnClick()
    {
        CloseSelf();
    }

    public virtual void OnJoinBtnClick()
    {
        switch (contestInfo.CurrentContestType)
        {
            case BUDContestType.Skin:
            case BUDContestType.Bundle:
                var panel = UIManager.Inst.SwapPanel(PanelId.AvatarStudioMainPanel) as AvatarStudioMainPanel;
                panel?.ShowContestView(contestInfo);
                break;
            case BUDContestType.OC:
            case BUDContestType.PetOC:
                OnJoinAvatar();
                break;
            case BUDContestType.Instrument:
                var instrumentPanel = UIManager.Inst.OpenPanel<MusicalInstrumentStudioPanel>(PanelId.MusicalInstrumentStudioPanel);
                instrumentPanel.SetContestFlag(contestInfo);
                break;
            case BUDContestType.Vehicle:
                var vehiclePanel = UIManager.Inst.OpenPanel<VehicleStudioPanel>(PanelId.VehicleStudioPanel);
                vehiclePanel.SetContestFlag(contestInfo);
                break;
            case BUDContestType.Camera:
                var albumPanel = UIManager.Inst.OpenPanel<CameraAllPhotoPanel>(PanelId.CameraAllPhotoPanel,false);
                albumPanel.SetContestFlag(contestInfo);
                break;
            case BUDContestType.MusicScore:
                var MusicScorePanel = UIManager.Inst.OpenPanel<MusicScoreStudioPanel>(PanelId.MusicScoreStudioPanel);
                MusicScorePanel.SetContestFlag(contestInfo);
                break;
            case BUDContestType.PetSkin:
            case BUDContestType.PetBundle:
                var petPanel = UIManager.Inst.SwapPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Pet) as AvatarStudioMainPanel;
                petPanel?.ShowContestView(contestInfo);
                break;
            default:
                LoggerUtils.Log($"[Contest] 暂不支持此类型：{contestInfo.CurrentContestType}");
                break;
        }
    }

    private void OnJoinAvatar()
    {
        var panel = UIManager.Inst.OpenPanel<ContestCreateOcPanel>(PanelId.ContestCreateOcPanel, contestInfo);
        panel.JoinSuccessAction = () =>
        {
            if (this == null)
            {
                return;
            }
            ShowMyEntry();
        };
    }

    private void OnJoinDesign()
    {
        if (UIManager.Inst.TryFindPanel<AvatarStudioMainPanel>(PanelId.AvatarStudioMainPanel, out var studioPanel))
        {
            studioPanel.ShowContestView(contestInfo);
            CloseSelf();
        }
        else
        {
            var panel = UIManager.Inst.OpenPanel<AvatarStudioMainPanel>(PanelId.AvatarStudioMainPanel);
            panel.ShowContestView(contestInfo);
        }
    }

    private void SyncToggleViewStatus()
    {
        if (contestInfo == null)
        {
            return;
        }

        ToggleList[(int)ViewType.AllEntries].gameObject.SetActive(contestInfo.status == 1);
        ToggleList[(int)ViewType.MyEntry].gameObject.SetActive(contestInfo.status == 1);
        ToggleList[(int)ViewType.Top100].gameObject.SetActive(contestInfo.status == 1);
        ToggleList[(int)ViewType.WinningEntries].gameObject.SetActive(contestInfo.status == 2);
        ToggleList[(int)ViewType.Winners].gameObject.SetActive(contestInfo.status == 2);
        ToggleList[(int)ViewType.LuckyDraw].gameObject.SetActive(contestInfo.status == 2 && contestInfo.hasLuckyDraw == 1);

        JoinBtn.gameObject.SetActive(contestInfo.status == 1);

        Invoke("LateSetCurView", 0.01f);
    }

    public void LateSetCurView()
    {
        switch (contestInfo.status)
        {
            case (int)ContestStatus.InProgress:
                InitView(JumpType);
                break;
            case (int)ContestStatus.Completed:
                InitView(ViewType.WinningEntries);
                break;
        }
        // 还原跳转类型
        JumpType = ViewType.AllEntries;
    }

    private void InitView(ViewType type)
    {
        ToggleList[(int)type].isOn = true;
        SwitchView(type);
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            if (gameObject) RefreshCurView();
        }
    }

    public void SwitchView(ViewType view)
    {
        curView = view;

        foreach (var element in viewDict)
        {
            if (view == element.Key)
            {
                element.Value.SetActive(true);
            }
            else
            {
                element.Value.SetActive(false);
            }
        }

        if (!viewDict.ContainsKey(view))
        {
            CreateView(view);
        }
        else
        {
            ResetView(view);
        }

        OnSwitchView(view);
    }

    public void CreateView(ViewType view)
    {
        switch (view)
        {
            case ViewType.AllEntries:
            case ViewType.MyEntry:
            case ViewType.Top100:
            case ViewType.WinningEntries:
                if (viewDict.ContainsKey(view))
                {
                    return;
                }
                ContestListView entrySrc = ContestListView.ContestListViewObject(contestInfo, gameObject);
                if (entrySrc == null)
                {
                    return;
                }
                var entry = Instantiate(entrySrc, EntryRoot);
                entry.gameObject.SetActive(true);
                ContestPageType pageType = ContestPageType.All;
                if (view == ViewType.MyEntry)
                {
                    pageType = ContestPageType.MyEntry;
                }
                else if (view == ViewType.Top100)
                {
                    pageType = ContestPageType.Top100;
                }
                else if (view == ViewType.WinningEntries)
                {
                    pageType = ContestPageType.Top100;
                }

                entry.InitUI(pageType, contestInfo, (entryInfo) => { OnClickItem(contestInfo, entryInfo); });
                if (pageType == ContestPageType.All)
                {
                    entry.ShowSearch();

                    if (contestInfo.CurrentContestType == BUDContestType.Skin)
                    {
                        entry.SetSearchPlaceholder("搜索皮肤设计码/作者ID");
                    } else if (contestInfo.CurrentContestType == BUDContestType.Instrument)
                    {
                        entry.SetSearchPlaceholder("搜索乐器设计码/作者ID");
                    } else if (contestInfo.CurrentContestType == BUDContestType.MusicScore)
                    {
                        entry.SetSearchPlaceholder("搜索乐谱设计码/作者ID");
                    }
                    else if (contestInfo.CurrentContestType == BUDContestType.Bundle)
                    {
                        entry.SetSearchPlaceholder("搜索皮肤套装设计码/作者ID");
                    }
                    else if (contestInfo.CurrentContestType == BUDContestType.PetSkin)
                    {
                        entry.SetSearchPlaceholder("搜索宠物皮肤设计码/作者ID");
                    }
                    else if (contestInfo.CurrentContestType == BUDContestType.PetOC)
                    {
                        entry.SetSearchPlaceholder("搜索宠物形象设计码/作者ID");
                    }
                    else if (contestInfo.CurrentContestType == BUDContestType.PetBundle)
                    {
                        entry.SetSearchPlaceholder("搜索宠物套装设计码/作者ID");
                    }
                    else if (contestInfo.CurrentContestType == BUDContestType.Vehicle)
                    {
                        entry.SetSearchPlaceholder("搜索载具设计码/作者ID");
                    }
                }

                if (contestInfo.CurrentContestType == BUDContestType.OC)
                {
                    var rtf = entry.gameObject.GetComponent<RectTransform>();
                    rtf.anchoredPosition = new Vector2(rtf.anchoredPosition.x + 80, rtf.anchoredPosition.y);
                }

                viewDict.Add(view, entry.gameObject);
                break;
            case ViewType.Winners:
                var winners = GameObjectEx.FindChildByName(transform,"Winners").gameObject;
                viewDict.Add(view, winners);
                winners.SetActive(true);
                ContestRankView.InitView(contestInfo, "当前活动已结束!\n记得参加我们下一个活动哦！", ScoreFormat);
                break;
            case ViewType.LuckyDraw:
                var luckyDraw = GameObjectEx.FindChildByName(transform,"LuckyDraw").gameObject;
                viewDict.Add(view, luckyDraw);
                luckyDraw.SetActive(true);
                LuckyDrawView.InitView(contestInfo);
                break;
        }
    }

    private void OnClickItem(ContestInfo contestInfo, ContestEntryInfo entryInfo)
    {
        ContestEventManager.ShowDetailView(contestInfo, entryInfo, i =>
        {
            if (this == null)
            {
                return;
            }

            OnVoteNumChange(contestInfo, entryInfo, i);
        });
    }

    private void OnVoteNumChange(ContestInfo contestInfo, ContestEntryInfo entryInfo, int num)
    {
        if (contestInfo.status != (int)ContestStatus.InProgress)
        {
            return;
        }

        if (!viewDict.ContainsKey(curView))
        {
            return;
        }

        ContestListView listView = viewDict[curView].GetComponent<ContestListView>();
        if (listView == null)
        {
            return;
        }

        var fixedInfo = entryInfo;
        if (fixedInfo.scoreInfo == null)
        {
            fixedInfo.scoreInfo = new ScoreInfo();
        }

        fixedInfo.scoreInfo.score = num;

        listView.UpdateSingleItem(fixedInfo);
    }

    /// <summary>
    /// 列表数据重置
    /// </summary>
    /// <param name="view"></param>
    private void ResetView(ViewType view)
    {
        switch (view)
        {
            case ViewType.AllEntries:
            case ViewType.MyEntry:
            case ViewType.Top100:
            case ViewType.WinningEntries:
                if (!viewDict.ContainsKey(view))
                {
                    return;
                }

                ContestListView listView = viewDict[view].GetComponent<ContestListView>();
                listView?.RequestDataList();
                break;
            case ViewType.Winners:
                break;
            case ViewType.LuckyDraw:
                break;
        }
    }

    public void RefreshCurView()
    {
        ResetView(curView);
    }

    public void ShowMyEntry()
    {
        InitView(ViewType.MyEntry);
    }

    protected virtual void OnSwitchView(ViewType view) { }

    protected virtual string ScoreFormat(int score)
    {
        return $"{GameUtils.ToBudCommonNumString(score)} Likes";
    }

}
