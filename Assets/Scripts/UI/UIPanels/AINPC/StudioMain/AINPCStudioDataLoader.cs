using System.Collections.Generic;
using GameData.Base;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.AINPCStudio
{
    public class AINPCStudioDataLoader : MonoBehaviour
    {
        protected StudioSubType _curStudioSubType;
        protected bool _isEnd = false;
        protected string _cookie = "";
        private BudTimer _timer;
        private string _reqHead = "";
        private bool _isRequestingData = false;

        private void ResetCookie()
        {
            _isEnd = false;
            _cookie = "";
        }

        public void InitData(StudioSubType type)
        {
            _curStudioSubType = type;
            
            _reqHead = _curStudioSubType == StudioSubType.Drafts
                ? HttpUrlDefine.NpcDraftList
                : HttpUrlDefine.NpcPublishList;
            
            ResetCookie();
        }

        private string GetReqParam()
        {
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["uid"] = AccountDataManager.Inst.Uid,
            };
            
            var reqParam = JsonConvert.SerializeObject(jb);
            return reqParam;
        }
        
        /// <summary>
        /// 获取草稿或发布数据
        /// </summary>
        /// <param name="isFirstRequest"></param>
        /// <param name="resultAction"></param>
        public void GetInstrumentStudioList(UnityAction<List<DraftListItem>> resultAction = null)
        {
            if (_isRequestingData)
            {
                resultAction?.Invoke(new List<DraftListItem>());
                return;
            }

            if (_isEnd)
            {
                resultAction?.Invoke(new List<DraftListItem>());
                return;
            }

            _isRequestingData = true;
            _timer = TimerManager.Inst.RunOnce("GetInstrumentStudioListRsp", 5, () =>
            {
                _isRequestingData = false;
            });
            

            NetworkManager.Inst.SendHttpRequest( _reqHead, HttpMethod.GET, GetReqParam(), (content) =>
            {
                OnGetSuccess(content, resultAction);
            }, (error) =>
            {
                OnGetFail(error, resultAction);
            });
        }

        private void OnGetSuccess(string content, UnityAction<List<DraftListItem>> resultAction = null)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);
            MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
            this._isEnd = mapListResponse.isEnd == 1;
            this._cookie = mapListResponse.cookie;
            
            if (mapListResponse == null || mapListResponse.list == null || mapListResponse.list.Count == null)
            {
                resultAction?.Invoke(new List<DraftListItem>());
            }
            else
            {
                resultAction?.Invoke(mapListResponse.list);
            }
        }

        private void OnGetFail(string error, UnityAction<List<DraftListItem>> resultAction = null)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);
            resultAction?.Invoke(new List<DraftListItem>());
        }
    }
}


