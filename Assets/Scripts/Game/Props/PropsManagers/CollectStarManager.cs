using System;
using System.Collections.Generic;
using Es;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(CollectStarBehaviour))]
    public class CollectStarManager : BaseNodeManager
    {
        public int MinNum => GetCollectAllStarConfig().MinNum;
        public int MaxNum => GetCollectAllStarConfig().MaxNum;

        public string MaxHint => $"最多设置 {MaxNum} 个星星";
        public string MinHint => $"最少设置 {MinNum} 个星星";

        public Action<string> SetCollectProgress;

        private GameMode curGameMode;

        protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
        {
            ReSortStarId();
        }

        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
        {
            var cmp = nodeBehaviour.entity.AddComp<CollectStarComponent>();
            cmp.Id = GetNewStarId();
            cmp.StarName = string.Empty;
            ReSortStarId();
        }

        protected override void OnNotifyRemove(NodeBaseBehaviour nodeBehaviour)
        {
            ReSortStarId();
        }

        protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
        {
            var newCmp = newBehaviour.entity.GetComp<CollectStarComponent>();
            var oldCmp = oldBehaviour.entity.GetComp<CollectStarComponent>();
            newCmp.StarName = oldCmp.StarName;
            ReSortStarId();
        }

        public override void OnEdit()
        {
            base.OnEdit();
            curGameMode = GameMode.Edit;
            OnChangeMode(curGameMode);
        }

        public override void OnPlay()
        {
            base.OnPlay();
            curGameMode = GameMode.Play;
            OnChangeMode(curGameMode);
        }

        public override void OnGuest()
        {
            base.OnGuest();
            curGameMode = GameMode.Guest;
            OnChangeMode(curGameMode);
        }

        public void OnChangeMode(GameMode gameMode)
        {
            var allStar = GetCollectStarList();
            if (allStar == null || allStar.Count <= 0) return;
            foreach (var star in allStar)
            {
                if (star != null && star is CollectStarBehaviour)
                {
                    var starBev = star as CollectStarBehaviour;
                    starBev.OnChangeMode(gameMode);
                }
            }
        }

        public void OnPassLevelStart()
        {
            OnChangeMode(curGameMode);
        }

        public void OnPassLevelStop()
        {
            OnChangeMode(GameMode.Edit);
        }

        public void ResetCollectProgress()
        {
            var allStar = GetCollectStarList();
            if (allStar == null || allStar.Count <= 0)
            {
                return;
            }

            foreach (var star in allStar)
            {
                if (star && star.entity != null)
                {
                    star.entity.GetComp<CollectStarComponent>().IsCollect = false;
                }
            }
            
            SetProgressUIShow();
        }

        // 星星序号，从1开始计数
        public int GetNewStarId()
        {
            return GetCurStarCount() + 1;
        }

        private void ReSortStarId()
        {
            var allStars = GetCollectStarList();
            foreach (var star in allStars)
            {
                var cmp = star.entity.GetComp<CollectStarComponent>();
                cmp.Id = allStars.IndexOf(star) + 1;
#if UNITY_EDITOR
                star.gameObject.name = $"CollectStar_{cmp.Id}";
#endif
            }
        }

        public List<NodeBaseBehaviour> GetCollectStarList()
        {
            return entities;
        }

        public int GetCurStarCount()
        {
            return entities?.Count ?? 0;
        }

        public CollectStarBehaviour GetCollectStarBev(int index)
        {
            if (index < 0 || index > entities.Count) return null;
            return entities[index] as CollectStarBehaviour;
        }


        public GamePropData GetCollectAllStarConfig()
        {
            return GamePropDataHelper.GetPropDataByID("20100028");
        }

        public string GetCollectProgressText()
        {
            var collects = 0;
            foreach (var star in GetCollectStarList())
            {
                if (star != null && star.entity != null && star.entity.GetComp<CollectStarComponent>().IsCollect)
                {
                    collects++;
                }
            }

            return $"{collects}/{GetCurStarCount()}";
        }

        public void DoStarCollected(CollectStarBehaviour bev)
        {
            bev.entity.GetComp<CollectStarComponent>().IsCollect = true;
            SetProgressUIShow();
        }

        public void SetProgressUIShow()
        {
            SetCollectProgress?.Invoke(GetCollectProgressText());
        }
    }
}