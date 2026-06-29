using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Es;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Drawing;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace GameUI
{
    public class PlantSeedPanel : BasePanel<PlantSeedPanel>
    {
        // 列表最后一个“创建入口”占位数据（非 null，供 OSA 正常渲染）
        internal static readonly object CreateEntryPlaceholder = new object();

        public CButton CloseBtn;

        public CButton Btn;

        public Toggle TogglePgc;

        public Toggle ToggleCreat;

        public PlantSeedPanelAdpter SelectAdpter;

        public PullToRefreshBehaviour PullToRefreshBehaviour;

        PlantTreeSystem data => PlantTreeSystem.Inst;


        private bool isRequesting = false;

        object info;
        public override void OnCreate()
        {
            base.OnCreate();
            Btn.onClick.AddListener(OnBtn);

            CloseBtn.onClick.AddListener(CloseSelf);

            TogglePgc.onValueChanged.AddListener(OnTog1);

            ToggleCreat.onValueChanged.AddListener(OnTog2);

            PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
            SelectAdpter.OnItemSelected = OnItemSelected;
            SelectAdpter.Data = new LazyDataHelper<object>(SelectAdpter, GetMapInfo);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (!SelectAdpter.IsInitialized)
            {
                SelectAdpter.Init();
            }
            isRequesting = false;
            TogglePgc.isOn = true;
        }

        protected override void OnDestroy()
        {
            data.MapListResponse = null;
            data.Seeds.Clear();
            base.OnDestroy();
        }

        void OnBtn() {
            if(isRequesting)
            {
                return;
            }
            isRequesting = true;
            if (info as DraftListItem != null)
            {
                RequestSeed(GameStudioUtils.GetBaseInfo(info as DraftListItem).cover);
            }
            else if (info as GamePropData != null)
            {
                RequestSeed((info as GamePropData).IconName);
            }
        }

        public void RequestSeed(string c)
        {
            var req = new JObject()
            {
                ["plantingCover"] = c,
            };
     
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PlantGetSeed, HttpMethod.POST, JsonConvert.SerializeObject(req),
           OnSuccess, onFail: arg0 => { 
               Debug.LogError("RequestSeed " + arg0);
               isRequesting = false;
           });
        }
        private void OnSuccess(string message)
        {
            TimerManager.Inst.RunOnce("plantSeed", 1f, () =>//延迟1秒，等服务器处理完, 不然排名为0
            {
                PlantTreeSystem.Inst.ActivityReq();

            });
            CloseSelf();
            //Invoke("DelayReqData", 0.2f);
        }

        //private void DelayReqData()
        //{
        //    Debug.Log("DelayReqData ");
        //    CancelInvoke("DelayReqData");
           
        
        //}
        private void OnTog1(bool bo)
        {
            if (bo)
            {
                PullToRefreshBehaviour.HideGizmo();
                SelectAdpter.Data.ResetItems(data.Seeds_pgc.Count);
                SelectAdpter.Refresh();
            }
        }

        private void OnTog2(bool bo)
        {
            if (bo)
            {
                data.SeedInfoReq((items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.Seeds.Count + 1);
                    SelectAdpter.Refresh();
                });
            }
        }

        private object GetMapInfo(int index)
        {
            if (TogglePgc.isOn)
            {
                return data.Seeds_pgc[index];
            }
            else
            {
                // 创作列表首位插入一个“创建入口”Item
                if (index == 0)
                {
                    return CreateEntryPlaceholder;
                }
                var realIndex = index - 1;
                if (realIndex < 0 || realIndex >= data.Seeds.Count)
                {
                    return null;
                }
                return data.Seeds[realIndex];
            }
        }

        private void OnItemSelected(object mapInfo)
        {
            foreach (var item in SelectAdpter._VisibleItems)
            {
                foreach (var item2 in item.ContainingCellViewsHolders)
                {
                    item2.item.On.gameObject.SetActive(false);
                }
            }
            info = mapInfo;
        }

        private void OnPullRefresh()
        {
            if (TogglePgc.isOn)
            {


            }
            else
            {
                data.SeedInfoReq((items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.Seeds.Count + 1);
                    SelectAdpter.Refresh();
                });
            }
        }
    }
}