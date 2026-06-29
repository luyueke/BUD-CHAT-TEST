using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : GlobalInstance<RewardManager>
{
    public GameObject GetRewardGo(BUDRewardType rewardType, int rewardNum)
    {
        var asset = Loader.Load<GameObject>("Assets/Loadable/UI/UIWidgets/Reward/RewardItem");
        var go = asset.Instantiate();
        var rewardItemMono = go.GetComponent<RewardItemMono>();
        rewardItemMono.SetData(rewardType, rewardNum);
        return go;
    }



}

[System.Serializable]
public struct RewardItemData
{
    public int pgcId;
    public int rewardNum;
}

[System.Serializable]
public class RewardItemDataArray
{
    [SerializeField] public RewardItemData[] rewardItems;
}
