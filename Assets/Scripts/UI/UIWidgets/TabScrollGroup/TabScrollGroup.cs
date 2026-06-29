using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using UnityEngine;

public class TabScrollGroup : MonoBehaviour
{
    public UScrollView uScrollView;
    public LogicToggleGroupItem tabGroupItem;
    public float scrollAnimTime = 0.5f;
    public float scrollAnimOffset = 0.06f;
    public bool horizontal = true;

    [Tooltip("是否使用平均区间计算 打开每个区间大小不根据Card实际长度计算 适用于每个区间大小差不多的场景")]
    public bool useAverageRegioning = false;

    private DoubleSearchDictionary<int, string> groupTabDict = new DoubleSearchDictionary<int, string>();
    private List<TabScrollTab> tabs = new List<TabScrollTab>();
    private List<TabScrollCard> cards = new List<TabScrollCard>();
    private List<int> activeGroups = new List<int>();
    private TabScrollRegion tabRegion;

    private Action<int, bool> onTabLogic;

    private bool rebuildScheduled;

    private void Awake()
    {
        RebuildImmediate();
    }

    private void Start()
    {
        tabGroupItem.AddListenerLogicAll(OnLogicToggle);
        uScrollView.onValueChanged.AddListener(OnScroll);
    }

    public void ReListener()
    {
        rebuildScheduled = true;
        tabGroupItem.ClearListenerLogicAll();
        tabGroupItem.AddListenerLogicAll(OnLogicToggle);
    }
    
    private void LateUpdate()
    {
        if (rebuildScheduled)
        {
            RebuildImmediate();
            rebuildScheduled = false;
        }
    }

    public void Rebuild()
    {
        rebuildScheduled = true;
    }

    public void RebuildImmediate()
    {
        activeGroups.Clear();
        tabs.Clear();
        cards.Clear();
        groupTabDict.Clear();

        List<TabScrollTab> newTabs = GameUtils.GetBehaviourInFirstLayer<TabScrollTab>(tabGroupItem.toggleParent.transform);
        for(int i = 0; i < newTabs.Count; ++i)
        {
            AddTab(newTabs[i]);
        }
        cards.AddRange(GameUtils.GetBehaviourInFirstLayer<TabScrollCard>(uScrollView.content));

        List<TabScrollTab> activeTabs = tabs.FindAll(c => c.IsElementActive);
        for(int i = 0; i < activeTabs.Count; ++i)
        {
            activeGroups.Add(activeTabs[i].groupId);
        }

        if (useAverageRegioning)
        {
            tabRegion = TabScrollRegion.CreateAverage(activeGroups.Count);
        }
        else
        {
            tabRegion = TabScrollRegion.Create(activeGroups, cards.FindAll(c => c.IsElementActive), horizontal);
        }
    }

    public void AddTabListener(Action<int, bool> act)
    {
        onTabLogic += act;
    }

    public void SetGroup(int group)
    {
        if (groupTabDict.TryGet(group, out string name))
        {
            tabGroupItem.SetValue(name, true);
        }
    }

    public void SetGroupWithoutNotify(int group, bool useAnim = true)
    {
        if(groupTabDict.TryGet(group, out string name))
        {
            tabGroupItem.SetIsOnNoLogic(name, true);
            ScrollToGroup(group, useAnim);
        }
    }

    private void AddTab(TabScrollTab tab)
    {
        tabs.Add(tab);
        groupTabDict.AddPair(tab.groupId, tab.logicToggle.gameObject.name);
    }

    private void OnLogicToggle(string name, bool isOn)
    {
        if(groupTabDict.TryGet(name, out int group))
        {
            ScrollToGroup(group);
            onTabLogic?.Invoke(group, isOn);
        }
    }

    private void OnScroll(Vector2 normalPos)
    {
        if (uScrollView.IsAnimMoving) return;
        int group = NormalPosToGroup(normalPos);
        if(groupTabDict.TryGet(group, out string name))
        {
            tabGroupItem.SetIsOnNoLogic(name, true);
        }
    }

    private void ScrollToGroup(int group, bool useAnim = true)
    {
        List<TabScrollCard> groupCards = cards.FindAll(c => c.groupId == group);
        TabScrollCard card = GetLowestValidSibling(groupCards);
        if (card)
        {
            if (useAnim)
            {
                uScrollView.SmoothMoveTo(card.transform.GetSiblingIndex(), scrollAnimTime, scrollAnimOffset);
            }
            else
            {
                uScrollView.MoveTo(card.transform.GetSiblingIndex(), scrollAnimOffset);
            }
        }
    }

    private int NormalPosToGroup(Vector2 pos)
    {
        if (activeGroups.Count <= 0) return 0;
        if (tabRegion == null) return 0;
        int tabIndex = tabRegion.GetRegion(horizontal ? pos.x : pos.y);
        return activeGroups[tabIndex];
    }

    private TabScrollCard GetLowestValidSibling(List<TabScrollCard> groupCards)
    {
        TabScrollCard tab = null;
        int minSib = int.MaxValue;
        for(int i = 0; i < groupCards.Count; ++i)
        {
            if (!groupCards[i].gameObject.activeSelf) continue;
            int index = groupCards[i].transform.GetSiblingIndex();
            if (index < minSib)
            {
                minSib = index;
                tab = groupCards[i];
            }
        }
        return tab;
    }


    private void SortCardList()
    {
        cards.Sort((a, b) =>
        {
            int comp = a.groupId - b.groupId;
            if (comp == 0) return a.transform.GetSiblingIndex() - b.transform.GetSiblingIndex();
            return comp;
        });
    }
}
