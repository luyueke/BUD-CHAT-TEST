using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class BadgeScorePanel : BasePanel<BadgeScorePanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton WeekScoreBtn;
    [SerializeField] private GameObject WeekScoreSelect;
    [SerializeField] private CButton HistoryScoreBtn;
    [SerializeField] private GameObject HistoryScoreSelect;
    [SerializeField] private List<Toggle> BadgeTypeToggles;
    [SerializeField] private BadgeListAdapter adapter;
    [SerializeField] private GameObject NoneTipsObj;

    private int _currentBadgeType = 0;
    private int _currentBadgeId = 0;
    private List<CreatorBadgeInfoData> _badgeList = new List<CreatorBadgeInfoData>();
    private Coroutine _refreshAdapterCo;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        WeekScoreBtn.onClick.AddListener(OnClickWeekScore);
        HistoryScoreBtn.onClick.AddListener(OnClickHistoryScore);
        for(int i = 0; i < BadgeTypeToggles.Count; i++)
        {
            int index = i;
            BadgeTypeToggles[i].onValueChanged.AddListener((isOn) => OnBadgeTypeToggleValueChanged(isOn, index));
        }

        if (adapter != null)
        {
            adapter.SetOnItemClick(OnBadgeItemClick);
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _currentBadgeId = 0;
        _currentBadgeType = 0;
        BadgeTypeToggles[0].SetIsOnWithoutNotify(true);
        RefreshBySeverInfo();
    }

    private void OnClickWeekScore()
    {
        _currentBadgeId = 0;
        WeekScoreSelect.SetActive(true);
        HistoryScoreSelect.SetActive(false);
        RefreshBySeverInfo();
    }
    private void OnClickHistoryScore()
    {
        _currentBadgeId = 1;
        RefreshBySeverInfo();
        WeekScoreSelect.SetActive(false);
        HistoryScoreSelect.SetActive(true);
    }
    private void OnBadgeTypeToggleValueChanged(bool isOn, int index)
    {
        if(isOn)
        {
            _currentBadgeType = index;
            RefreshBadgeList(_badgeList);
        }
    }

    private void RefreshBySeverInfo()
    {
        _badgeList.Clear();
        if (CreatorRequestCtrl.Inst == null)
        {
            NoneTipsObj?.SetActive(true);
            TryApplyAdapterData(new List<CreatorBadgeInfoData>());
            return;
        }

        // 本周积分：先拉 publicProfile 回填 creatorBadgeInfo，再拉 badgeList（否则 UserInfo 里徽章类型为空无法显示「使用中」）
        if (_currentBadgeId == 0)
        {
            RequestPublicProfileThenCreatorBadgeList();
        }
        else
        {
            RequestCreatorBadgeListOnly();
        }
    }

    private void RequestPublicProfileThenCreatorBadgeList()
    {
        var uid = AccountDataManager.Inst?.UserInfo?.uid;
        if (string.IsNullOrEmpty(uid) || NetworkManager.Inst == null)
        {
            RequestCreatorBadgeListOnly();
            return;
        }

        var jb = new JObject
        {
            ["targetUid"] = uid
        };

        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.publicProfile,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                if (!this)
                {
                    return;
                }

                try
                {
                    var res = JsonConvert.DeserializeObject<GetUserInfoRsp>(content);
                    if (res?.userInfo?.creatorBadgeInfo != null)
                    {
                        AccountDataManager.Inst?.ApplyCreatorBadgeInfo(res.userInfo.creatorBadgeInfo);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BadgeScorePanel] publicProfile: " + e.Message);
                }

                RequestCreatorBadgeListOnly();
            },
            _ =>
            {
                if (!this)
                {
                    return;
                }

                RequestCreatorBadgeListOnly();
            },
            retryCount: 3);
    }

    private void RequestCreatorBadgeListOnly()
    {
        CreatorRequestCtrl.Inst.RequestCreatorBadgeListInfo(_currentBadgeId, (res) =>
        {
            if (res.list == null || res.list.Count == 0)
            {
                NoneTipsObj?.SetActive(true);
                TryApplyAdapterData(new List<CreatorBadgeInfoData>());
                return;
            }

            for (int i = 0; i < res.list.Count; i++)
            {
                _badgeList.Add(res.list[i]);
            }

            RefreshBadgeList(_badgeList);
        }, (error) =>
        {
            Debug.LogError("RequestCreatorBadgeListInfo error: " + error);
        });
    }

    private void RefreshBadgeList(List<CreatorBadgeInfoData> badgeList)
    {
        var filtered = new List<CreatorBadgeInfoData>();
        if (badgeList != null && badgeList.Count > 0)
        {
            for (int i = 0; i < badgeList.Count; i++)
            {
                var item = badgeList[i];
                if (item == null)
                {
                    continue;
                }

                if (PassesBadgeTypeFilter(item, _currentBadgeType))
                {
                    filtered.Add(item);
                }
            }
        }

        NoneTipsObj?.SetActive(filtered.Count == 0);
        TryApplyAdapterData(filtered);
    }

    /// <summary>
    /// BadgeTypeToggles 下标：0=全部；1=积分 2000～3600；2=积分≥3600；3=排名 1～100（含）。
    /// </summary>
    private static bool PassesBadgeTypeFilter(CreatorBadgeInfoData item, int toggleIndex)
    {
        switch (toggleIndex)
        {
            case 0:
                return true;
            case 1:
                return item.score >= 2000 && item.score <= 3600;
            case 2:
                return item.score >= 3600 && item.level <= 1;
            case 3:
                return item.level > 1;
            default:
                return true;
        }
    }

    private void TryApplyAdapterData(List<CreatorBadgeInfoData> list)
    {
        if (_refreshAdapterCo != null)
        {
            StopCoroutine(_refreshAdapterCo);
            _refreshAdapterCo = null;
        }
        _refreshAdapterCo = StartCoroutine(CoApplyAdapterData(list ?? new List<CreatorBadgeInfoData>()));
    }

    private IEnumerator CoApplyAdapterData(List<CreatorBadgeInfoData> list)
    {
        int waitFrames = 10;
        while (waitFrames-- > 0 && (adapter == null || adapter.Data == null))
        {
            yield return null;
        }

        if (adapter == null || adapter.Data == null)
        {
            yield break;
        }

        // 设置本周/历史模式：0=本周，1=历史
        adapter.SetHistoryMode(_currentBadgeId == 1, refresh: false);
        adapter.Data.ResetItems(list);
        adapter.Refresh();

        if (list.Count > 0)
        {
            adapter.SetSelectedId(list[0].id, refresh: false);
        }
    }

    private void OnBadgeItemClick(CreatorBadgeInfoData model)
    {
        if (adapter != null)
        {
            adapter.SetSelected(model, refresh: false);
        }
        // 这里如果后续需要刷新右侧详情，可在此扩展
    }
}
