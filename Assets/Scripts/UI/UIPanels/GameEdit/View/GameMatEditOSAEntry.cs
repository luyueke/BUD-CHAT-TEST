using System.Collections;
/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-08 21:28:10
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-13 10:56:03
 * @ Description:
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Es;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class GameMatEditOSAEntry : MonoBehaviour
    {
        [SerializeField]private GameMatEditOSAAdapter osMaxAdpater;
        [SerializeField]private GameMatEditOSAAdapter osSmallAdapter;
        [SerializeField]private GameObject matStoreEmptyGo;

        GameMatEditOSAAdapter curAdapter;
        MatGroupTypeEnum curMatGroupTypeEnum;
        List<GameMatUIData> matDataList;
        bool isSmall;
        MaterialUnionID initializeMatId;
        bool isInitializeNotify;
        GameMatUIData curGameMatUIData = null; // 当前选择的材质
        GameMatUIData goStoreStyleData;
        Action<GameMatUIData, GameMatUIData, bool> onMatOldNewChangeAction;


        private void Awake()
        {
            matDataList = DataTables.GetMatDataConfigList().Select(x=>new GameMatUIData(x)).ToList();
            // UGC
            goStoreStyleData = new GameMatUIData(){IsStoreGoStyle=true, MatGroupType=MatGroupTypeEnum.Store};
            matDataList.Add(goStoreStyleData);
            var ugcMatInfoList = GameUgcMatManager.Inst.MatResInfoList;
            matDataList.AddRange(ugcMatInfoList.Select(x=>new GameMatUIData(x)));

            GameUgcMatManager.Inst.AddRefreshInteractListener(OnUgcMatRefresh);
            GameUgcMatManager.Inst.AddRestInteractListener(OnUgcMatReset);
        }

        private void OnDestroy()
        {
            GameUgcMatManager.Inst.RemoveRefreshInteractListener(OnUgcMatRefresh);
            GameUgcMatManager.Inst.RemoveRestInteractListener(OnUgcMatReset);
        }

        public void Init(Action<GameMatUIData, GameMatUIData, bool> action)
        {
            onMatOldNewChangeAction = action;
        }

        public void ChangeAdapter(bool isSmall)
        {
            curAdapter = isSmall ? osSmallAdapter : osMaxAdpater;
            curAdapter.OnMatChange = null;
            curAdapter.OnMatChange += OnMatChangeCallback;

            this.isSmall = isSmall;
            if (!isSmall)
            {
                var refreshController = curAdapter.GetComponentInChildren<PullToRefreshBehaviour>();
                refreshController.OnRefreshWithSlideUp.RemoveListener(OnPullReleased);
                refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            }

            ResetView(curMatGroupTypeEnum);
        }

        public void ResetView(MatGroupTypeEnum matGroupTypeEnum)
        {
            curMatGroupTypeEnum = matGroupTypeEnum;

            List<GameMatUIData> curMatDataList;
            matStoreEmptyGo.SetActive(false);
            if (this.isSmall)
            {
                // 最小化的时候先排除UGC素材
                curMatDataList = matDataList.Where(x=>x.MatGroupType != MatGroupTypeEnum.Store).ToList();
                curMatDataList = SplitHorizontalData(curMatDataList);
            } else {
                curMatDataList = matDataList.Where(x=>x.MatGroupType == matGroupTypeEnum).ToList();
                if (matGroupTypeEnum == MatGroupTypeEnum.Store)
                {
                    matStoreEmptyGo.SetActive(curMatDataList.Count <= 1);
                    if(curMatDataList.Count <= 1 ) curMatDataList.Clear();
                    GameUgcMatManager.Inst.RefreshInteractList();
                }
            }

            TryInitiliazeMaterial();

            if (curAdapter!=null && curAdapter.IsInitialized)
            {
                curAdapter?.Data.ResetItems(curMatDataList);
            } else {
                curAdapter?.Data.List.Clear();
                curAdapter?.Data.List.AddRange(curMatDataList);
            }
        }

        void OnMatChangeCallback(GameMatUIData data)
        {
            var oldData = curGameMatUIData;
            var newData = data;
            if (oldData != null)
                oldData.IsSelected = false;

            newData.IsSelected = true;
            curGameMatUIData = data;

            curAdapter?.Refresh();
            onMatOldNewChangeAction?.Invoke(oldData, newData, true);
        }

        public void SetInitializeMaterial(MaterialUnionID id, bool isNotify)
        {
            initializeMatId = id;
            isInitializeNotify = isNotify;
        }

        public void SetUndoMaterialId(MaterialUnionID id)
        {
            if (curGameMatUIData != null)
                curGameMatUIData.IsSelected = false;

            var newData = GetMatDataById(id);
            if (newData != null) {
                newData.IsSelected = true;
            }
            curGameMatUIData = newData;
            curAdapter?.Refresh();
        }

        void TryInitiliazeMaterial()
        {
            if (initializeMatId != null)
            {
                curGameMatUIData = GetMatDataById(initializeMatId);
                if (curGameMatUIData!=null)
                {
                    curGameMatUIData.IsSelected = true;
                    initializeMatId = null;
                    onMatOldNewChangeAction?.Invoke(null, curGameMatUIData, isInitializeNotify);
                }
            }
        }

        public GameMatUIData GetMatDataById(MaterialUnionID id)
        {
            return matDataList.Find(x=>x.Id == id);
        }

        public GameMatUIData GetMatUIDataOrDefaultById(MaterialUnionID id)
        {
            var matData = GetMatDataById(id);
            if (matData == null)
            {
                matData = matDataList[0];
            }

            return matData;
        }

        List<GameMatUIData> SplitHorizontalData(List<GameMatUIData> gameMatUIs)
        {
            int count = gameMatUIs.Count;
            int halfCount = Mathf.CeilToInt(count / 2);
            List<GameMatUIData> filterDataList = new List<GameMatUIData>(halfCount * 2);
            for (int i = 0; i < halfCount; i++)
            {
                filterDataList.Insert(i * 2, gameMatUIs[i]);
                if (halfCount + i < count)
                    filterDataList.Insert(i * 2 + 1, gameMatUIs[halfCount + i]);
            }

            return filterDataList;
        }

        void OnPullReleased()
        {
            LoggerUtils.Log("GameMatEditOSAEntry.OnPullReleased");
        }

        void OnUgcMatRefresh(List<ResInfo<MaterialInfo>> data)
        {
            if (data == null) return;

            var curMatDataList = data.Select(x=>
            {
                var matData = new GameMatUIData(x);
                if (curGameMatUIData != null && curGameMatUIData.Id == matData.Id)
                {
                    matData.IsSelected = true;
                    curGameMatUIData = matData;
                }
                return matData;
            }
            ).ToList();
            matDataList.AddRange(curMatDataList); // Cache

            TryInitiliazeMaterial();
            if (curMatGroupTypeEnum == MatGroupTypeEnum.Store)
            {
                matStoreEmptyGo.SetActive(false);
                if (curAdapter != null)
                {
                    var adapterData = curAdapter.Data;
                    if (adapterData.List.Count == 0)
                    {
                        adapterData.List.Add(goStoreStyleData);
                    }
                    adapterData.List.AddRange(curMatDataList);
                    if (curAdapter!=null && curAdapter.IsInitialized)
                    {
                        adapterData.NotifyListChangedExternally();
                    }
                }
            }
        }

        void OnUgcMatReset()
        {
            matDataList.RemoveAll(x=>x.MatGroupType==MatGroupTypeEnum.Store && !x.IsStoreGoStyle);

            if (curMatGroupTypeEnum == MatGroupTypeEnum.Store)
            {
                curAdapter?.Data.List.Clear();
                if (curAdapter!=null && curAdapter.IsInitialized)
                {
                    curAdapter?.Data.NotifyListChangedExternally();
                }
            }
        }
    }
}
