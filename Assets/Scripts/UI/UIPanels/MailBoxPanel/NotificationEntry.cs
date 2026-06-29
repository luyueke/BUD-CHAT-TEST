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
    public class NotificationEntry : MonoBehaviour
    {
        public NotificationAdapter adapter;
        private MailboxSubType _mailboxSubType;
        private bool _isEnd;
        private string _cookie;

        protected void Start()
        {
            //需要动态拉取数据必须要做的初始化操作
            PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
            refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
            adapter.OnItemsUpdatedAct.AddListener(refreshController.HideGizmo);
            adapter._mailboxSubType = _mailboxSubType;
        }

        public void ResetAdpater()
        {
            if (adapter != null && adapter.IsInitialized)
            {
                adapter.ResetItems(0);
                adapter.ClearPool();
            }
        }

        public void SetActions(Action<NotificationInfo> onPropClickAct, MailboxSubType mailboxSubType)
        {
            adapter.onPropClickAct = onPropClickAct;
            this._mailboxSubType = mailboxSubType;
        }

        public void GetFirstPageDatas(Action<List<NotificationInfo>> complete)
        {
            ResetCookies();
            GetNotificationDatas(datas =>
            {
                complete?.Invoke(datas);
                ResetAdpater();
                if (adapter!=null)
                {
                    adapter.OnItemsUpdatedAct?.Invoke();
                    adapter.Data.ResetItems(datas);
                }
                
            });
        }


        public void OnPullReleased(float sign)
        {
            if (sign < 0)
            {
                GetNotificationDatas(OnReceivedNewModelsForInsert);
            }
        }

        void OnReceivedNewModelsForInsert(List<NotificationInfo> newModels)
        {
            if (newModels == null || newModels.Count == 0)
            {
                adapter.OnItemsUpdatedAct?.Invoke();
                return;
            }
            adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
            adapter.OnItemsUpdatedAct?.Invoke();
        }



        private void ResetCookies()
        {
            this._cookie = "";
            this._isEnd = false;
        }

        private void GetNotificationDatas(UnityAction<List<NotificationInfo>> resultAction = null)
        {
            if (_isEnd)
                return;

            if (!this)
                return;

            var req = new NotificationListReq
            {
                cookie = this._cookie,
                type = (int)_mailboxSubType
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NotificationList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
                {
                    NotificationListResponse notificationListResponse = JsonConvert.DeserializeObject<NotificationListResponse>(content);
                    this._isEnd = notificationListResponse.isEnd == 1;
                    this._cookie = notificationListResponse.cookie;
                    if (notificationListResponse.notifications == null)
                    {
                        notificationListResponse.notifications = new List<NotificationInfo>();
                    }
                    resultAction?.Invoke(notificationListResponse.notifications);
                },
                (error) => { resultAction?.Invoke(new List<NotificationInfo>()); });
        }

    }

    /// <summary>
    /// Mail数据管理
    /// </summary>
    public enum MailboxSubType
    {
        System, //系统
        Interactive, //互动
        Gift,//礼物
        Check //审核
    }
    public class NotificationListReq
    {
        public string cookie;
        public string isEnd;
        public int type;//0-系统邮件，1-个人邮件
    }
    public class NotificationListResponse
    {
        public List<NotificationInfo> notifications;
        public int isEnd;
        public string cookie;
    }
}