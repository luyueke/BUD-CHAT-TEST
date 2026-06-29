using System.Collections.Generic;
using GameData.Base;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.AnimationStudio
{
    public class AnimationStudioDataLoader : MonoBehaviour
    {
        protected StudioSubType _curStudioSubType;
        protected AnimationStudioType _animationStudioType;
        protected EmoteSubType _emoSubType;
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

        public void InitData(StudioSubType type, AnimationStudioType studioType, EmoteSubType ugcPoseSubType = EmoteSubType.ErrEmoteSubType)
        {
            _curStudioSubType = type;
            _animationStudioType = studioType;
            _emoSubType = ugcPoseSubType;
            
            if (_animationStudioType == AnimationStudioType.Animation)
            {
                _reqHead = _curStudioSubType == StudioSubType.Drafts
                    ? HttpUrlDefine.animDraftList
                    : HttpUrlDefine.animPublishList;
            }
            
            if (_animationStudioType == AnimationStudioType.Pose)
            {
                _reqHead = _curStudioSubType == StudioSubType.Drafts
                    ? HttpUrlDefine.poseDraftList
                    : HttpUrlDefine.posePublishList;
            }
            
            ResetCookie();
        }

        private string GetReqParam()
        {
            var skinType = 99;
            if (_emoSubType == EmoteSubType.Single || _emoSubType == EmoteSubType.Double)
            {
                skinType = 0;
            }
            else if (_emoSubType == EmoteSubType.PetSingle || _emoSubType == EmoteSubType.PetWithPlayer)
            {
                skinType = 1;
            }
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["uid"] = AccountDataManager.Inst.Uid,
                ["poseType"] = (int)_emoSubType,
                ["animType"] = (int)_emoSubType,
                ["skinType"] = skinType
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
                OnGetSuccess(error, resultAction);
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


