using System;
using System.Collections.Generic;
using Es;
using Game.BagSystem;
using Game.Store;
using GameData.Rewards;
using UI.Manager;
using UnityEngine;

namespace UI.UIPanels.RewardPanel
{
    public static class RewardHelper
    {
        private const string RewardAtlasPath = "Assets/Loadable/UI/UIPanel/RewardPanel/GetRewardPanel.spriteatlas";

        public static Sprite LoadRewardSprite(string spriteName, GameObject refObj)
        {
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RewardAtlasPath, spriteName, refObj);
            return sprite;
        }

        public static RewardLevelConfig GetRewardLevelCfg(RewardLevel levelType)
        {
            return DataTables.GetRewardLevelConfig((int)levelType);
        }

        public static RewardLevel GetMaxRewardLevel(List<RewardInfo> rewardInfos)
        {
            RewardLevel maxLevel = RewardLevel.Blue;
            var rewards = rewardInfos;

            // 侧边商标
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                if ((int)reward.level <= (int)maxLevel)
                {
                    maxLevel = (RewardLevel)reward.level;
                }
            }

            return maxLevel;
        }

        public static Sprite GetRewardItemSprite(RewardInfo rewardInfo, GameObject go)
        {
            if (rewardInfo == null)
                return null;

            return PgcUtils.GetIconSpriteByPgcId(rewardInfo.pgcId.ToString(), go);
        }
    }
}