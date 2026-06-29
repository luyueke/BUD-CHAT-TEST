using System;
using System.Collections.Generic;
using Basic.Extensions;
using Game.Event;
using UnityEngine;

public class LoverGiftView : MonoBehaviour
{
    public List<LoverGiftViewItem> rewardItems;

    public void ShowRewards(RewardItem rewardItem)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        if (rewardItem.rewardIcon2.IsNullOrEmpty())
        {
            //rewardItemDatas.Add(new CommonRewardItemData()
            //{
            //    IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1,
            //        gameObject),
            //    RewardAmount = rewardItem.rewardNum1,
            //    rewardName = rewardItem.rewardName1
            //});
        }
        else
        {
            //rewardItemDatas.Add(new CommonRewardItemData()
            //{
            //    IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1,
            //        gameObject),
            //    RewardAmount = rewardItem.rewardNum1,
            //    rewardName = rewardItem.rewardName1
            //});
            //rewardItemDatas.Add(new CommonRewardItemData()
            //{
            //    IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon2,
            //        gameObject),
            //    RewardAmount = rewardItem.rewardNum2,
            //    rewardName = rewardItem.rewardName2
            //});
        }
        panel.ShowRewards(rewardItemDatas);

    }
}