using System;
using UnityEngine;

using Game.Store;
using GameData.Base;
using GameData.PgcData;
using GameData.Gashapon;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class UgcLikeButton : MonoBehaviour
    {
        [SerializeField] private Button btn;
        [SerializeField] private Sprite norSprite;
        [SerializeField] private Sprite selSprite;

        public Action ClickAction;
        private Image ShowImage;
        private bool isRequest = false;
        private void Awake()
        {
            btn?.onClick.AddListener(() =>
            {
                if (this == null)
                {
                    return;
                }
                if (isRequest)
                {
                    return;
                }
                ClickAction?.Invoke();
            });
            ShowImage = btn.GetComponent<Image>();
        }

        private AssetsData _assetsData;
        private RecommendItemData bundleData;

        private bool isLike
        {
            get
            {
                if (_assetsData != null)
                {
                    return _assetsData?.UgcInfo?.interactInfo?.liked == 1;
                } 
                else if (bundleData != null)
                {
                    return bundleData?.interactInfo?.liked == 1;
                }
                return false;
            }
        }

        private string UGCID
        {
            get
            {
                if (_assetsData != null)
                {
                    return _assetsData?.UgcInfo?.UgcInfo?.id;
                } 
                else if (bundleData != null)
                {
                    return bundleData?.ugcId;
                }

                return null;
            }
        }
        
        public void SetData(AssetsData assetsData)
        {
            if (assetsData == null)
            {
                return;
            }

            bundleData = null;
            _assetsData = assetsData;

            CancelReqeust();

            ReloadUI();

            if (assetsData.ResourceType == ResourceType.Theatre)
                FetchLikeState(assetsData.UgcInfo?.ugcId, HttpUrlDefine.TheatreInfo);
            else if (assetsData.ResourceType == ResourceType.AvatarCard)
                FetchLikeState(assetsData.UgcInfo?.ugcId, HttpUrlDefine.ActorInfo);
        }

        private void FetchLikeState(string ugcId, string endpoint)
        {
            if (string.IsNullOrEmpty(ugcId)) return;
            var jb = new JObject { ["id"] = ugcId };
            NetworkManager.Inst.SendHttpRequest(
                endpoint,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                content =>
                {
                    if (_assetsData?.UgcInfo?.ugcId != ugcId) return;
                    var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                    if (rsp?.interactInfo == null) return;
                    if (_assetsData.UgcInfo.interactInfo == null)
                        _assetsData.UgcInfo.interactInfo = new BaseInteractInfo();
                    _assetsData.UgcInfo.interactInfo.liked = rsp.interactInfo.liked;
                    ReloadUI();
                },
                error => { });
        }

        public void SetBundleData(RecommendItemData data)
        {
            _assetsData = null;
            bundleData = data;
            
            CancelReqeust();

            ReloadUI();
        }

        private void ReloadUI()
        {
            if (ShowImage != null)
            {
                Debug.Log("print isLike:" + isLike);
                ShowImage.sprite = isLike ? selSprite : norSprite;
            }
        }
        
        private void CancelReqeust()
        {
            isRequest = false;
        }

        private void FixedOriginalDataAfterChange()
        {
            if (_assetsData != null)
            {
                if (_assetsData.UgcInfo == null) return;
                if (_assetsData.UgcInfo.interactInfo == null)
                    _assetsData.UgcInfo.interactInfo = new GameData.Base.BaseInteractInfo();
                _assetsData.UgcInfo.interactInfo.liked = isLike ? 0 : 1;
            } else if (bundleData != null)
            {
                bundleData.interactInfo.liked = isLike ? 0 : 1;
            }
        }

        public void Like(Action<String> completeAction = null)
        {
            var ugcId = UGCID;
            if (string.IsNullOrEmpty(ugcId))
            {
                return;
            }

            isRequest = true;
            var reqLikeType = isLike ? UGCCommonReq.LikeType.UnLike : UGCCommonReq.LikeType.Like;
            UGCCommonReq.Inst.UGCLikeReq(ugcId, reqLikeType, (reqComplete) =>
            {
                Debug.Log("这里输出点赞按钮:" + this);
                if (this == null)
                {
                    return;
                }
                Debug.Log("reqComplete:" + reqComplete + "   isRequest:" + isRequest);
                // 请求成功 且 没有切换过原数据更新UI
                if (reqComplete && isRequest)
                {
                    FixedOriginalDataAfterChange();
                    ReloadUI();
                    completeAction?.Invoke(ugcId);
                }
                
                isRequest = false;
            });
        }
        
    }
}
