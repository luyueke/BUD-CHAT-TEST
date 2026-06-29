using System.Collections;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;

public class PlayerContributionView : MonoBehaviour
{
    public List<PlayerContributionItem> items;
    bool isInit = false;
    private const int FIXED_ITEM_COUNT = 3;

    public void InitUI(List<HeatContribution> heatContribution)
    {

        // 更新items数据
        for (int i = 0; i < FIXED_ITEM_COUNT; i++)
        {
            if (items[i] != null)
            {
                if (heatContribution != null && i < heatContribution.Count)
                {
                    items[i].InitUI(heatContribution[i]);
                }
            }
        }

        isInit = true;
    }

    public void UpdateUI(List<HeatContribution> heatContribution)
    {
        if (!isInit)
        {
            InitUI(heatContribution);
        }
        for (int i = 0; i < FIXED_ITEM_COUNT; i++)
        {
            if (items[i] != null)
            {
                items[i].Init();
            }
        }

        // 只更新有数据的item
        if (heatContribution != null)
        {
            for (int i = 0; i < FIXED_ITEM_COUNT && i < heatContribution.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].InitUI(heatContribution[i]);
                }
            }
        }
    }
}
