using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace xasset
{
    public interface AssetRequestHandler
    {
        void OnStart();
        void Update();
        void Dispose();
        void WaitForCompletion();
    }

    public struct AssetRequestHandlerEdit : AssetRequestHandler
    {
        private AssetRequest _request { get; set; }

        public void Dispose()
        {

        }

        public void OnStart()
        {
            Load(_request);
        }

        public void Update()
        {
            //throw new NotImplementedException();
        }

        public void WaitForCompletion()
        {
            //throw new NotImplementedException();
        }
        public static AssetRequestHandler CreateInstance(AssetRequest assetRequest)
        {
            return new AssetRequestHandlerEdit { _request = assetRequest };
        }

        private void Load(AssetRequest request)
        {
            

#if UNITY_EDITOR
            if (request.isAll)
            {
                request.assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(request.path);
                if (request.assets == null)
                {
                    request.SetResult(Request.Result.Failed, "subAssets == null");
                    return;
                }
            }
            else
            {
                request.asset = AssetDatabase.LoadAssetAtPath(request.path, request.type);
                if (request.asset == null)
                {
                    request.SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            request.progress = 1;
            request.SetResult(Request.Result.Success);
#else
            request.progress = 0;
            request.SetResult(Request.Result.Failed);
#endif
        }
    }

    public struct AssetRequestHandlerRuntime : AssetRequestHandler
    {
        private enum Step
        {
            LoadDependencies,
            LoadAsset
        }

        private Dependencies _dependencies;
        private AssetBundleRequest _loadAssetAsync;
        private Step _step;
        private AssetRequest _request { get; set; }

        public void OnStart()
        {
            _dependencies = Dependencies.LoadAsync(_request.info);
            _step = Step.LoadDependencies;
        }

        public void Update()
        {
            switch (_step)
            {
                case Step.LoadDependencies:
                    UpdateLoadDependencies();
                    break;

                case Step.LoadAsset:
                    UpdateLoadAsset();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateLoadAsset()
        {
            _request.progress = 0.5f * _loadAssetAsync.progress * 0.5f;
            if (!_loadAssetAsync.isDone) return;
            SetResult();
        }

        private void UpdateLoadDependencies()
        {
            _dependencies.Update();
            _request.progress = _dependencies.progress * 0.5f;
            if (!_dependencies.isDone) return;
            LoadAssetAsync();
        }

        private void LoadAssetAsync()
        {
            if (!_dependencies.CheckResult(_request))
                return;

            var assetBundle = _dependencies.assetBundle;
            if (assetBundle == null)
            {
                _request.SetResult(Request.Result.Failed, "assetBundle == null");
                return;
            }

            var info = _request.info;
            var type = _request.type;
            var path = info.path;
            _loadAssetAsync = _request.isAll
                ? assetBundle.LoadAssetWithSubAssetsAsync(path, type)
                : assetBundle.LoadAssetAsync(path, type);
            _step = Step.LoadAsset;
        }

        private void SetResult()
        {
            if (_request.isAll)
            {
                _request.assets = _loadAssetAsync.allAssets;
                if (_request.assets == null)
                {
                    _request.SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                _request.asset = _loadAssetAsync.asset;
                if (_request.asset == null)
                {
                    _request.SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            _request.SetResult(Request.Result.Success);
        }

        public void Dispose()
        {
            _dependencies.Release();
        }

        public void WaitForCompletion()
        {
            _dependencies.WaitForCompletion();
            if (_request.result == Request.Result.Failed) return;
            if (_loadAssetAsync == null) LoadAssetAsync();
            if (_request.result == Request.Result.Failed) return;
            SetResult();
        }

        public static AssetRequestHandler CreateInstance(AssetRequest assetRequest)
        {
            return new AssetRequestHandlerRuntime {_request = assetRequest};
        }
    }
}
