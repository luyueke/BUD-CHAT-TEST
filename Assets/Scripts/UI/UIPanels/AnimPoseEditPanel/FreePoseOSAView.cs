using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.PgcData;
using UI.BaseWidgets;
using UnityEngine;

namespace BUD.AnimPose
{
    public class FreePoseOSAView : BasePoseOSAView
    {
        public PullToRefreshBehaviour RefreshCtr;
        public FreePoseOSAAdapter Adapter;
        public FreePoseDataLoader DataLoader;
        private List<QuickPoseData> allDatas = new List<QuickPoseData>();

        public override void OnStart(UgcPoseSubType subType)
        {
            Adapter.Data = new LazyDataHelper<QuickPoseData>(Adapter, CreateNewModel);
            Adapter.Init();
            Adapter.SetOnSelectAct(OnItemSelected);
            GetFirstPageDatas(subType);
        }

        public override void OnUpdate()
        {
            CurSelectId = string.Empty;
            if (Adapter.Data != null)
            {
                Adapter.Data.ResetItems(allDatas.Count);
            }
        }

        
        private void OnItemSelected(QuickPoseData data)
        {
            if (!UgcAnimVipChecker.Inst.CanUseVipAnim(data.poseInfo))
            {
                return;
            }
            CurSelectId = data.poseInfo.id;
            Adapter.Data.ResetItems(allDatas.Count);
            OnSelectPoseClick?.Invoke(data.poseInfo.poseData);
        }

        public void GetFirstPageDatas(UgcPoseSubType subType)
        {
            allDatas.Clear();
            DataLoader.GetDatas(subType,(datas) =>
            {
                allDatas.AddRange(datas);
                Adapter.Data.ResetItems(datas.Count, false);
            },this.gameObject);
        }

        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private QuickPoseData CreateNewModel(int index)
        {
            if (index >= 0 && index < allDatas.Count)
            {
                var poseData = allDatas[index];
                poseData.Selected = CurSelectId == poseData.poseInfo.id;
                return allDatas[index];
            }
            return new QuickPoseData();
        }
    }
}
