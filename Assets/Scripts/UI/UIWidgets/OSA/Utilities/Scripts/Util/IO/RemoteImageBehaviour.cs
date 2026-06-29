using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util;
using System;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using System.Collections.Generic;
using xasset;

namespace Com.TheFallenGames.OSA.Util.IO
{
    /// <summary>Utility behavior to be attached to a GameObject containing a RawImage for loading remote images using <see cref="SimpleImageDownloader"/>, optionally displaying a specific image during loading and/or on error</summary>
    [RequireComponent(typeof(RawImage))]
    public class RemoteImageBehaviour : MonoBehaviour
    {
        public delegate void LoadCompleteDelegate(bool fromCache, bool success);

        [Tooltip("If not assigned, will try to find it in this game object")]
        [SerializeField]
        protected RawImage _RawImage = null;
#pragma warning disable 0649

        public GameObject _LoadingObj;

        public Texture2D _LoadingTexture = null;
        public Texture2D _ErrorTexture = null;
#pragma warning restore 0649

        public string _CurrentRequestedURL;
        bool _DestroyPending;
        Texture2D _Texture;
        IPool _Pool;

        public RawImage RawImage => _RawImage;

        public bool isSetNativeSize;
        private RectTransform Rect;
        private Vector2 PreSize;
        public void InitializeWithPool(IPool pool)
        {
            _Pool = pool;
        }

        void Awake()
        {
            Rect = transform as RectTransform;
            PreSize = Rect.sizeDelta;
            if (!_RawImage)
                _RawImage = GetComponent<RawImage>();
        }

        public void Init()
        {
            if (_RawImage != null)
            {
                _RawImage.texture = _LoadingTexture;
            }

            if (_LoadingObj != null)
            {
                _LoadingObj.SetActive(true);
            }
        }

        public void ResetRawImage()
        {
            if (_RawImage != null)
            {
                _RawImage.texture = _LoadingTexture;
            }
        }

        private int poolNum = 3;

        private Queue<LoadRequest> loadedPath = new Queue<LoadRequest>();

        /// <summary>Starts the loading, setting the current image to <see cref="_LoadingTexture"/>, if available. If the image is already in cache, and <paramref name="loadCachedIfAvailable"/>==true, will load that instead</summary>
        public void Load(string imageURL, bool loadCachedIfAvailable = true, LoadCompleteDelegate onCompleted = null, Action onCanceled = null, int index = 0)
        {
            OnLoadingView();
            _CurrentRequestedURL = imageURL;
            if (string.IsNullOrEmpty(imageURL))
            {
                LogReportFirebase($"图片加载失败: URL={imageURL}, error: imageURL == null");
                LoadFail("fail");
                onCompleted?.Invoke(true, false);
                return;
            }
            LoadRequest request = imageURL.StartsWith("Assets/") ? Asset.LoadAsync(imageURL, typeof(Texture2D)) : Asset.LoadRemoteImageAsync(imageURL);

            if (request == null)
            {
                LogReportFirebase($"图片加载失败: URL={imageURL}, error:request == null");
                LoadFail("fail");
                onCompleted?.Invoke(true, false);
                return;
            }
            if (request.result != Request.Result.Success)
            {
                OnLoadingView();
            }
            else 
            {
                var asset = GetAsset(request);
                if (imageURL.StartsWith("Assets"))
                {
                    Debug.Log("RemoteImageBehaviour CallBack:" + asset);
                }
                AddRequestCache(request);
                LoadSuccess(asset);
                onCompleted?.Invoke(true, true);
                return;
            }
            request.completed += (_) =>
            {
                var asset = GetAsset(request);
                if (imageURL.StartsWith("Assets"))
                {
                    Debug.Log("RemoteImageBehaviour completed:" + asset + "," + request.result + "," + request.path + "," + _CurrentRequestedURL);
                }
                if (!this)
                {
                    request.Release();
                    return;
                }
                if (request.result == Request.Result.Success)
                {
                    AddRequestCache(request);
                    if (request.path != _CurrentRequestedURL) return;
                    LoadSuccess(asset);
                    onCompleted?.Invoke(true, true);
                }
                else if (request.result == Request.Result.Cancelled)
                {
                    request.Release();
                    LogReportFirebase($"图片加载失败: URL={imageURL}, error:request.result=Cancelled");
                    LoadFail("cancelled");
                    onCanceled?.Invoke();
                }
                else
                {
                    request.Release();
                    LoadFail("fail");
                    LogReportFirebase($"图片加载失败: URL={imageURL}, error:request.result=Failed message{request.error}");
                    onCompleted?.Invoke(true, false);
                }
            };
        }

        protected void AddRequestCache(LoadRequest request)
        {
            loadedPath.Enqueue(request);
            if (loadedPath.Count > poolNum) loadedPath.Dequeue().Release();
        }

        protected Texture2D GetAsset(LoadRequest load)
        {
            if (load is AssetRequest)
            {
                return (Texture2D)(load as AssetRequest).asset;
            }

            if (load is RemoteImageRequest)
            {
                return (Texture2D)(load as RemoteImageRequest).asset;
            }

            return null;
        }

        protected virtual void OnLoadingView()
        {
            if (_LoadingTexture != null) _RawImage.texture = _LoadingTexture;
            if (_LoadingObj != null)
            {
                _LoadingObj.SetActive(true);
            }
        }

        protected virtual void OnErrorView()
        {
            if (_ErrorTexture != null) _RawImage.texture = _ErrorTexture;
            if (_LoadingObj != null)
            {
                _LoadingObj.SetActive(false);
            }
        }

        void OnDestroy()
        {
            _DestroyPending = true;

            while (loadedPath.Count > 0)
            {
                loadedPath.Dequeue()?.Release();
            }
        }

        public void ChangeRawImageState(bool isRelease)
        {
            System.Action act = isRelease ? TemporaryRecycleGameObject : ResumeGameObject;
            act.Invoke();
        }

        private void ResumeGameObject()
        {
            if (!string.IsNullOrEmpty(_CurrentRequestedURL))
            {
                Load(_CurrentRequestedURL);
            }
        }

        private void TemporaryRecycleGameObject()
        {
            while (loadedPath.Count > 0)
            {
                loadedPath.Dequeue()?.Release();
            }
        }

        public Texture2D GetTexture()
        {
            return _Texture;
        }

        private void LoadSuccess(Texture texture)
        {
            _Texture = (Texture2D)texture;
            if (_RawImage)
            {
                _RawImage.color = Color.white;
                _RawImage.texture = texture;
                if (isSetNativeSize)
                {
                    SetNativeSize();
                }
            }
            if (_LoadingObj != null)
            {
                _LoadingObj.SetActive(false);
            }
        }

        private void LoadFail(string msg)
        {
            OnErrorView();
        }

        private void LogReportFirebase(string msg)
        {
            Debug.LogError(msg);
        }

        private void SetNativeSize() {
            if (_RawImage.texture != null)
            {
                if (Rect == null)
                {
                    Rect = transform as RectTransform;
                    PreSize = Rect.sizeDelta;
                }

                var size = new Vector2(_RawImage.texture.width, _RawImage.texture.height);

                if (size.x < PreSize.x || size.y < PreSize.y)
                {
                    if (size.x < PreSize.x)
                    {
                        size *= (PreSize.x / size.x);
                    }

                    if (size.y < PreSize.y)
                    {
                        size *= (PreSize.y / size.y);
                    }
                }
                else
                {
                    if (size.x > PreSize.x)
                    {
                        size *= (PreSize.x / size.x);
                    }

                    if (size.y < PreSize.y)
                    {
                        size *= (PreSize.y / size.y);
                    }
                }


                Rect.sizeDelta = size;
            }
        }
    }
}
