using System.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.MapData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.PublicSever
{
    public class JoinFriendRoomPanel : BasePanel<JoinFriendRoomPanel>
    {
        public GameObject Go_Loading;
        public GameObject Go_Empty;
        public Transform BG;
        public CButton Btn_Close;
        public CButton Btn_EnterRoomCode;
        public PullToRefreshBehaviour RefreshCtr;
        public JoinFriendAdapter Adapter;
        public JoinFriendDataLodaer DataLoader;
        private List<JoinFriendData> allDatas = new List<JoinFriendData>();
        
        public override void OnCreate()
        {
            base.OnCreate();
            Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/AvatarBg.prefab").Instantiate(BG);
            Go_Loading.SetActive(true);
            
            Btn_Close.onClick.AddListener(() => { CloseSelf();});
            Btn_EnterRoomCode.onClick.AddListener(OnBtnEnterRoomCode);
            
            //需要动态拉取数据必须要做的初始化操作
            RefreshCtr.OnRefreshWithSign.AddListener(OnPullReleased);

            DataLoader.ResetCookie();
            Adapter.OnItemsUpdated.AddListener(RefreshCtr.HideGizmo);
            Adapter.Data = new LazyDataHelper<JoinFriendData>(Adapter, CreateNewModel);
            Adapter.Init();

            GetFirstPageDatas();
        }
        
        public void GetFirstPageDatas()
        {
            DataLoader.ResetCookie();
            DataLoader.GetDatas((datas) =>
            {
                Go_Loading.SetActive(false);
                if (datas == null || datas.Count == 0)
                {
                    Go_Empty.SetActive(true);
                    return;
                }
                allDatas.AddRange(datas);
                Adapter.Data.ResetItems(datas.Count, false);
                Adapter.OnItemsUpdated?.Invoke();
            });
        }
        
        /// <summary>
        /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private JoinFriendData CreateNewModel(int index)
        {
            if (index >= 0 && index < allDatas.Count)
            {
                return allDatas[index];
            }

            return new JoinFriendData();
        }
        
        private void OnReceivedNewModelsForInsert(List<JoinFriendData> newDatas)
        {
            if (newDatas == null || newDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }

            Adapter.Data.List.AddRange(newDatas);
            Adapter.Refresh(false);
        }

        private void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                DataLoader.GetDatas(OnReceivedNewModelsForInsert);
            }
        }
        
        private void OnBtnEnterRoomCode()
        {
            UIManager.Inst.OpenPanel<EnterRoomCodePanel>(PanelId.EnterRoomCodePanel);
        }
    }
}