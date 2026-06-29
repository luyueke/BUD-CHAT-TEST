using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using GameData;
using UnityEngine;
using Newtonsoft.Json;

public class ContestSelectListEntry : MonoBehaviour
{
    public ContestListType listType = ContestListType.Top100;

    [SerializeField] private GameObject noDataGo;
    // [SerializeField] private LoopGridView scrollView;
    // private HttpSegmentRequestHandle<ContestEntryInfo> handle;
    //
    // private StandardImagePool imagePool;

    private ContestInfo contestInfo;
    private bool getFirstPage = false;

    public string CurContestId => contestInfo?.contestId;

    public void Init()
    {
        // imagePool = new StandardImagePool(20);
        // handle = new HttpSegmentRequestHandle<ContestEntryInfo>("/contest/entryListV2");
        // scrollView.InitGridView(0, OnGetItemByRowColumn);
        // scrollView.OnPreLoadEvent = handle.Next;
        // scrollView.OnOverBottomEvent = handle.Next;
    }

    private void OnDestroy()
    {
        // handle?.Cancel();
        // imagePool?.Clear();
    }

    public void SetContest(ContestInfo info)
    {
        // contestInfo = info;
        // var ob = new JObject()
        // {
        //     ["id"] = contestInfo.contestId,
        //     ["listType"] = (int)listType,
        // };
        //
        // handle.Init(JsonConvert.SerializeObject(ob), (add) =>
        // {
        //     if (!this) return;
        //     if (add)
        //     {
        //         scrollView.SetListItemCount(handle.Count, false);
        //     }
        //     scrollView.RefreshShownItem();
        //     ShowEmpty(handle.Count <= 0);
        // });
        // handle.AddFailAction((msg) =>
        // {
        //     if (!this) return;
        //     ShowEmpty(handle.Count <= 0);
        // });
    }

    public void GetFirstPage(bool force = false)
    {
        // if ((!force) && getFirstPage) return;
        // handle.Reset();
        // handle.Next();
        // getFirstPage = true;
    }

    // private LoopGridViewItem OnGetItemByRowColumn(LoopGridView gridView, int itemIndex, int row, int column, ScrollDirection sdir)
    // {
    //     LoopGridViewItem item = scrollView.GetItemByPool();
    //     var data = handle.Get(itemIndex);
    //     if (data == null) return item;
    //     IContestItem contestItem = item.GetComponent<IContestItem>();
    //     contestItem?.SetImagePool(imagePool.Pool);
    //     contestItem?.RefreshContest(contestInfo);
    //     contestItem?.Refresh(data);
    //     contestItem?.UseRanking(false);
    //     return item;
    // }

    private void ShowEmpty(bool isOn)
    {
        noDataGo.SetActive(isOn);
    }
}
