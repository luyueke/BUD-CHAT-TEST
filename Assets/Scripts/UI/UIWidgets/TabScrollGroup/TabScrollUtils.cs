using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabScrollRegion
{
    private List<float> ratios; //[0.1, 0.4, ... , 1]

    public TabScrollRegion()
    {
        ratios = new List<float>();
    }

    public int GetRegion(float normPos)
    {
        if (ratios.Count <= 0) return 0;
        float norm = Mathf.Clamp01(normPos);
        for(int i = 0; i < ratios.Count; ++i)
        {
            if(norm < ratios[i])
            {
                return i;
            }
        }
        return ratios.Count - 1;
    }

    public static TabScrollRegion Create(List<float> raws)
    {
        TabScrollRegion region = new TabScrollRegion();
        float sum = 0;
        float totalSum = GetSum(raws);
        for(int i = 0; i < raws.Count; ++i)
        {
            sum += raws[i];
            region.ratios.Add(sum / totalSum);
        }
        return region;
    }

    public static TabScrollRegion CreateAverage(int count)
    {
        List<float> ratios = new List<float>();
        for(int i = 0; i < count; ++i)
        {
            ratios.Add(1f);
        }
        return Create(ratios);
    }

    
    public static TabScrollRegion Create(List<int> activeGroups, List<TabScrollCard> cards, bool isHorizontal)
    {
        List<float> raws = new List<float>();
        for(int i = 0; i < activeGroups.Count; ++i) 
        {
            int group = activeGroups[i];
            List<TabScrollCard> gcards = cards.FindAll(c => c.groupId == group);
            float sum = 0;
            for(int j = 0; j < gcards.Count; ++j)
            {
                sum += isHorizontal ? gcards[j].Dimension.x : gcards[j].Dimension.y;
            }
            raws.Add(sum);
        }
        return Create(raws);
    }

    public static float GetSum(List<float> raws)
    {
        float sum = 0;
        for (int i = 0; i < raws.Count; ++i)
        {
            sum += raws[i];
        }
        return sum;
    }
}
