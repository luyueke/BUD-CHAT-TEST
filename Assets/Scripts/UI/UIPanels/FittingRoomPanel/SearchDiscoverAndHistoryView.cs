using System;
using System.Collections;
using System.Collections.Generic;
using EventTracking;
using Game.CommunityGame;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class SearchDiscoverAndHistoryView : MonoBehaviour
    {
        public CButton refreshBtn;
        public CButton deleteBtn;
        public GameObject horiTempGo;
        public Transform discoverContentTrans;
        public Transform historyContentTrans;

        public Action<string,int, int> onItemClick; //word, type, id

        void Awake()
        {
            refreshBtn.onClick.AddListener(OnRefreshBtnClick);
            deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        }

        void OnRefreshBtnClick()
        {
            SearchLogicMgr.Inst.searchWordListRsp.MoveNextPage();
            RefreshData();
        }
        void OnDeleteBtnClick()
        {
            SearchLogicMgr.Inst.DeleteSearchHistoryWord();
            RefreshData();
        }
        public void RefreshData()
        {
            gameObject.SetActive(true);
            AddDiscoverData();
            AddHistoryData();
            UICommonUtils.RefreshLayout(transform);
        }

        void AddDiscoverData()
        {
            var data = SearchLogicMgr.Inst.searchWordListRsp?.GetSearchWordList(); //搜索词列表
            if(data == null)
            {
                return;
            }
            int childCnt = discoverContentTrans.childCount;
            for (int i = childCnt - 1; i >= 0; i--)
            {
                DestroyImmediate(discoverContentTrans.GetChild(i).gameObject);
            }
            int totalCount = data.Count;
            GameObject go = null;
            bool needRefresh = true;
            int itemIndex = 1;
            for (int i = 0; i < totalCount; i++)
            {
                var item = data[i];
                if (needRefresh)
                {
                    go = Instantiate(horiTempGo, discoverContentTrans);
                    go.SetActive(true);
                    needRefresh = false;
                    itemIndex = 1;
                }
                AddItem(go.transform.Find("item" + itemIndex), item.word,1,item.id);
                itemIndex++;
                if(itemIndex > 3)
                {
                    needRefresh = true;
                }
            }
            UICommonUtils.RefreshLayout(discoverContentTrans);
        }

        void AddItem(Transform itemTrans, string word, int type, int id)
        {
            itemTrans.gameObject.SetActive(true);
            itemTrans.Find("text").GetComponent<Text>().text = word;
            itemTrans.GetComponent<Button>().onClick.AddListener(() =>
            {
                onItemClick?.Invoke(word, type, id);
            });
        }
        void AddHistoryData()
        {
            var historyData = SearchLogicMgr.Inst.GetSearchHistoryWordList(); //搜索历史词列表
            int totalCount = historyData.Count;
            int childCnt = historyContentTrans.childCount;
            for (int i = childCnt - 1; i >= 0; i--)
            {
                DestroyImmediate(historyContentTrans.GetChild(i).gameObject);
            }
            GameObject go = null;
            bool needRefresh = true;
            int itemIndex = 1;
            for (int i = 0; i < totalCount; i++)
            {
                var item = historyData[i];
                if(needRefresh){
                    go = Instantiate(horiTempGo, historyContentTrans);
                    go.SetActive(true);
                    needRefresh = false;
                    itemIndex = 1;
                }
                AddItem(go.transform.Find("item"+itemIndex), item,2,0);
                itemIndex++;
                if(itemIndex > 3)
                {
                    needRefresh = true;
                }
            }
            UICommonUtils.RefreshLayout(historyContentTrans);
        }



    }
}