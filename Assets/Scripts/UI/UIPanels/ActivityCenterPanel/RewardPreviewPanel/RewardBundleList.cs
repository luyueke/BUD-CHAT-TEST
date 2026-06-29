
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardBundleList : MonoBehaviour
{
    [SerializeField] Text bundleName;
    [SerializeField] Transform content;
    [SerializeField] RewardBundleItem bundleItem;
    [SerializeField] Image contentBg;

    public void SetTarget(ActivityInfo info, string name)
    {
        content.ClearChildren();
        bundleName.text = name;
        foreach (var asset in info.rewardList)
        {
            var item = Instantiate(bundleItem, content);

            item.SetData(asset);
        }
    }
    
    public void SetTarget(List<string> pgcIds, string name, string bgColor = null, string contentBgColor = null)
    {
        content.ClearChildren();
        bundleName.text = name;
        foreach (var pgcId in pgcIds)
        {
            var item = Instantiate(bundleItem, content);

            item.SetData(pgcId, bgColor);
        }

        if (!string.IsNullOrEmpty(contentBgColor))
        {
            contentBg.color = DataUtil.DeSerializeColorCheckHash(contentBgColor);
        }
    }
}
