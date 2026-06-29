using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MusicalInstrument
{
    public class UgcToneDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";
        private BudTimer _timer;
        public bool _isRequestingData = false;

        public void ResetCookie()
        {
            _isEnd = false;
            _cookie = "";
        }
        
        /// <summary>
        /// 获取草稿或发布数据
        /// </summary>
        /// <param name="isFirstRequest"></param>
        /// <param name="resultAction"></param>
        public void GetTonePublishedData(UnityAction<List<ToneItemData>> resultAction = null)
        {
            if (_isRequestingData)
            {
                resultAction?.Invoke(new List<ToneItemData>());
                return;
            }

            if (_isEnd)
            {
                resultAction?.Invoke(new List<ToneItemData>());
                return;
            }

            _isRequestingData = true;
            _timer = TimerManager.Inst.RunOnce("GetPublishToneListRsp", 5, () =>
            {
                _isRequestingData = false;
            });
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["uid"] = AccountDataManager.Inst.Uid,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest( HttpUrlDefine.UGCToneDraftList, HttpMethod.GET, reqParam, (content) =>
            {
                OnGetSuccess(content, resultAction);
            }, (error) =>
            {
                OnGetFail(error, resultAction);
            });
        }

        private void OnGetSuccess(string content, UnityAction<List<ToneItemData>> resultAction = null)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);
            var publishToneListRsp = JsonConvert.DeserializeObject<PublishToneListRsp>(content);
            this._isEnd = publishToneListRsp.IsEnd == 1;
            this._cookie = publishToneListRsp.cookie;
            
            if (publishToneListRsp == null || publishToneListRsp.list == null || publishToneListRsp.list.Count == null)
            {
                resultAction?.Invoke(new List<ToneItemData>());
            }
            else
            {
                resultAction?.Invoke(publishToneListRsp.list);
            }
        }

        private void OnGetFail(string error, UnityAction<List<ToneItemData>> resultAction = null)
        {
            _isRequestingData = false;
            TimerManager.Inst.Stop(_timer);
            resultAction?.Invoke(new List<ToneItemData>());
        }
    }
    
    public class PublishToneListRsp : HttpPageBaseData
    {
        public List<ToneItemData> list = new List<ToneItemData>();
    }

    public class ToneItemData
    {
        public ToneInfo musicToneInfo;
        public BaseCreator creator;
    }
}
