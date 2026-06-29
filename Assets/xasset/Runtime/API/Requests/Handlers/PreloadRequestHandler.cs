using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace xasset
{
    public interface PreloadRequestHandler
    {
        void OnStart();
        void Update();
        void Dispose();
        void WaitForCompletion();
    }

    public struct PreloadRequestHandlerEdit : PreloadRequestHandler
    {
        private PreloadRequest _request { get; set; }

        public void Dispose()
        {

        }

        public void OnStart()
        {
            _request.SetResult(Request.Result.Success);
        }

        public void Update()
        {
            //throw new NotImplementedException();
        }

        public void WaitForCompletion()
        {
            //throw new NotImplementedException();
        }
        public static PreloadRequestHandler CreateInstance(PreloadRequest assetRequest)
        {
            return new PreloadRequestHandlerEdit { _request = assetRequest };
        }
    }

    public struct PreloadRequestHandlerRuntime : PreloadRequestHandler
    {
        private Dependencies _dependencies;
        private PreloadRequest _request { get; set; }

        public void OnStart()
        {
            _dependencies = Dependencies.LoadAsync(_request.info);
        }

        public void Update()
        {
            _dependencies.Update();
            _request.progress = _dependencies.progress * 0.5f;
            if (!_dependencies.isDone) return;
            SetResult();
        }

        private void SetResult()
        {
            if (_dependencies.CheckResult(_request))
            {
                _request.SetResult(Request.Result.Success);
            }
            else
            {
                _request.SetResult(Request.Result.Failed, "preload fail");
            }
        }

        public void Dispose()
        {
            _dependencies.Release();
        }

        public void WaitForCompletion()
        {
            _dependencies.WaitForCompletion();
            SetResult();
        }

        public static PreloadRequestHandler CreateInstance(PreloadRequest assetRequest)
        {
            return new PreloadRequestHandlerRuntime { _request = assetRequest};
        }
    }
}