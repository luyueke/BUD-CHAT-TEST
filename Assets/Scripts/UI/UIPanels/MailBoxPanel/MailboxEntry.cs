using System;
using System.Collections.Generic;
using BUD.MailBox;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace BUD.MailBox
{
    public class MailboxEntry : MonoBehaviour
    {
        public MailboxAdapter adapter;
        private MailboxSubType _mailboxSubType;
        private bool _isEnd;
        private string _cookie;

        protected void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdatedAct.AddListener(refreshController.HideGizmo);
        }

        public void ResetAdpater()
        {
            if (adapter != null && adapter.IsInitialized)
            {
                adapter.ResetItems(0);
            }
        }

        public void SetActions(Action<MailInfo> onSelectItemAct, MailboxSubType mailboxSubType)
        {
            adapter.OnSelectItemAct = onSelectItemAct;
            this._mailboxSubType = mailboxSubType;
        }

        public void GetFirstPageDatas(Action<List<MailInfo>> complete)
        {
            ResetCookies();
            GetMailDatas(datas =>
            {
                complete?.Invoke(datas);
                ResetAdpater();
                adapter.OnItemsUpdatedAct?.Invoke();
                adapter.Data.ResetItems(datas);
            });
        }


        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                GetMailDatas(OnReceivedNewModelsForInsert);
            }
        }

        void OnReceivedNewModelsForInsert(List<MailInfo> newModels)
        {
            if (newModels == null || newModels.Count == 0)
            {
                adapter.OnItemsUpdatedAct?.Invoke();
                return;
            }

            adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
            adapter.OnItemsUpdatedAct?.Invoke();
        }


        public void UpdateSingleItem(MailInfo mailItem)
        {
            adapter.UpdateSingleItem(mailItem);
        }

        private void ResetCookies()
        {
            this._cookie = "";
            this._isEnd = false;
        }

        private void GetMailDatas(UnityAction<List<MailInfo>> resultAction = null)
        {
            if (_isEnd)
                return;

            if (!this)
                return;

            var req = new MailboxListReq
            {
                cookie = this._cookie,
                type = _mailboxSubType == MailboxSubType.System?0:1
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MailList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
                {
                    MailListResponse mapListResponse = JsonConvert.DeserializeObject<MailListResponse>(content);
                    this._isEnd = mapListResponse.isEnd == 1;
                    this._cookie = mapListResponse.cookie;
                    if (mapListResponse.mails == null)
                    {
                        mapListResponse.mails = new List<MailInfo>();
                    }

                    resultAction?.Invoke(mapListResponse.mails);
                },
                (error) => { resultAction?.Invoke(new List<MailInfo>()); });
        }

    }
    public class MailboxListReq
    {
        public string cookie;
        public string isEnd;
        public int type;//0-系统邮件，1-个人邮件
    }
    public class MailListResponse
    {
        public List<MailInfo> mails;
        public int isEnd;
        public string cookie;
    }
}