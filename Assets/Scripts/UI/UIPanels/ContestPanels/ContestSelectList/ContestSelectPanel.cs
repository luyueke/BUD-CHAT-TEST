using Com.TheFallenGames.OSA.Util.IO;
using System.Collections.Generic;
using System.Linq;
using GameData;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using GameUI;

public class ContestSelectPanel : BasePanel<ContestSelectPanel>
{
    private Button backBtn;
    private Button joinBtn;

    private ContestBgItem contestBg;
    private ContestEndInHint endInHint;
    private RemoteImageBehaviour bannerRawImage;
    private Button bannerBtn;
    private Transform listContainer;

    private SelectableContestGroupView contestView;

    private Dictionary<string, ContestListView> allEntries;

    private Transform BannerRefer;
    public override void OnCreate()
    {
        base.OnCreate();
        Transform uiRoot = GetBaseLayout2D();
        backBtn = GameObjectEx.FindComponentByName<CButton>(uiRoot, "BackBtn");
        joinBtn = GameObjectEx.FindComponentByName<CButton>(uiRoot, "JoinBtn");
        backBtn.onClick.AddListener(OnCloseClick);
        joinBtn.onClick.AddListener(OnJoinClick);

        listContainer = GameObjectEx.FindChildByName(gameObject, "MainListView");
        contestView = GameObjectEx.FindComponentByName<SelectableContestGroupView>(uiRoot, "ActiveContests");
        contestView.InitViewInfo();
        contestView.OnSelect = OnSelectContest;

        allEntries = new Dictionary<string, ContestListView>();

        contestBg = GameObjectEx.FindComponentByName<ContestBgItem>(uiRoot, "Bg/ContestBgItem");
        endInHint = GameObjectEx.FindComponentByName<ContestEndInHint>(uiRoot, "Hint");
        bannerRawImage = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(uiRoot, "BannerRefer/BannerImg");
        bannerBtn = bannerRawImage.GetComponent<Button>();
        bannerBtn.onClick.AddListener(OnPrizesClick);

        BannerRefer = GameObjectEx.FindChildByName(uiRoot, "BannerRefer");
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            //string contestId = (string)args[0];
            //SelectContest(contestId);
            contestView.SetSelectItem(0);
        }
    }

    public void SelectContest(string contestId)
    {
        contestView.SetSelectItem(contestId);
    }

    protected Transform GetBaseLayout2D()
    {
        return transform.Find("BaseLayout2D");
    }

    private void OnJoinClick()
    {
        ContestInfo contest = contestView.CurContest;
        if (contest == null)
        {
            return;
        }

        ContestEventManager.Inst.OpenContestPage(contest.contestId);
    }

    private void OnCloseClick()
    {
        CloseSelf();
    }

    private void OnPrizesClick()
    {
        UIManager.Inst.OpenPanel(PanelId.ContestRewardPanel, contestView.CurContest);
    }

    private void OnSelectContest(ContestInfo contestInfo)
    {
        if (contestInfo == null)
        {
            return;
        }

        foreach (ContestListView e in allEntries.Values)
        {
            e.gameObject.SetActive(e.CurContestId == contestInfo.contestId);
        }

        ContestListView entry;
        if (allEntries.TryGetValue(contestInfo.contestId, out entry))
        {
            entry.gameObject.SetActive(true);
        }
        else
        {
            ContestListView entrySrc = ContestListView.ContestListViewObject(contestInfo, gameObject);
            if (entrySrc == null)
            {
                return;
            }

            entry = Instantiate(entrySrc, listContainer);
            entry.gameObject.SetActive(true);
            entry.InitUI(ContestPageType.Top100, contestInfo,
                (entryInfo) => { OnClickItem(contestInfo, entryInfo); }, hideRankForce: true);
            allEntries.Add(contestInfo.contestId, entry);
        }

        RefreshBg(contestInfo);

        BannerRefer.gameObject.SetActive(contestInfo.contestType != (int)BUDContestType.OC);
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
        var viewKey = contestInfo.contestId;
        if (string.IsNullOrEmpty(viewKey))
        {
            return;
        }

        if (contestInfo.status != (int)ContestStatus.InProgress)
        {
            return;
        }

        if (!allEntries.Keys.Contains(viewKey))
        {
            return;
        }

        ContestListView listView = allEntries[viewKey];
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

    private void RefreshBg(ContestInfo info)
    {
        contestBg.Refresh(info);
        endInHint.Refresh(info);
        if (bannerRawImage)
        {
            ContestEventManager.Inst.SetRawImage(bannerRawImage, info.rewardUrl, (isSucc, fromCache) =>
            {
                if (isSucc && this)
                {
                    Vector2 refer = (bannerRawImage.transform.parent as RectTransform).rect.size;
                    //banner图可能会有一条很细的黑边 所以比父节点大1点 让父节点mask掉
                    bannerRawImage.RawImage.FitTexture(refer.x + 1f, widthFirst: true);
                }
            });
        }

        Text joinTxt = joinBtn.GetComponentInChildren<Text>();
        if (joinTxt)
        {
            if (info.status == (int)ContestStatus.Completed)
            {
                joinTxt.SetLocalText("查看结果");
            }
            else
            {
                joinTxt.SetLocalText("参加");
            }
        }
    }

}
