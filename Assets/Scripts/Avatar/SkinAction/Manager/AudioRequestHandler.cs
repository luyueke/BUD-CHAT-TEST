using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace xasset
{
    public interface AudioRequestHandler
    {
        void OnStart();
        void Update();
        void Dispose();
        void WaitForCompletion();
    }

    public class AudioRequestHandlerRuntime : AudioRequestHandler
    {
        protected enum Step
        {
            Download,
            LoadLocal,
            None,
        }
        private static bool NeedCheckResSize = false;
        protected Step _step;
        protected AudioRequest _request { get; set; }
        private UnityWebRequest www { get; set; }
        protected string _savePath;
        protected int _retryTimes;
        protected AudioClip audioClip;
        /// <summary>
        /// 根据原始 URL 扩展名确定的音频格式，供下载和本地沙盒加载时统一使用。
        /// </summary>
        private AudioType _audioType;

        public void OnStart()
        {
            _retryTimes = 0;
            // 根据原始 URL 扩展名提前确定解码格式，避免后续沙盒加载时拿不到原始路径
            _audioType = GetAudioTypeFromUrl(_request.path);
            var path = _request.path;
            var url = path;

            // TODO 从本地文件加载
            if (_request.isLocalUrl)
            {
                _savePath = new System.Uri(_request.path).AbsoluteUri;
                _step = Step.LoadLocal;
                return;
            }

            _savePath = SandboxStorage.GetSandboxPath(GetMd5(url));
            var file = new FileInfo(_savePath);
            if (file.Exists)
            {
                _step = Step.LoadLocal;
            }
            else
            {
                StartDownload(url);
            }
        }

        private string GetMd5(string url)
        {
            var bytes = Encoding.UTF8.GetBytes(url);
            var md5 = Utility.ComputeHash(bytes);
            return md5;
        }

        /// <summary>
        /// 根据 URL 的扩展名推断对应的 Unity AudioType。
        /// 未能识别的扩展名默认返回 MPEG（兼容 mp3）。
        /// </summary>
        private AudioType GetAudioTypeFromUrl(string url)
        {
            if (url.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase))
                return AudioType.WAV;

            if (url.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
                return AudioType.OGGVORBIS;

            return AudioType.MPEG;
        }

        public virtual void Update()
        {
            switch (_step)
            {
                case Step.Download:
                    UpdateDownload();
                    break;

                case Step.LoadLocal:
                    UpdateLoadLocal();
                    break;
                default:
                    Debug.LogError("Download Ugc AudioClip Fail");
                    break;
            }
        }

        protected virtual void UpdateDownload()
        {
            if (www == null) {
                Debug.LogError("assetBundleRequest is Null");
                _request.SetResult(Request.Result.Failed, "assetBundleRequest is Null");
                return;
            }

            if (_request == null) {
                Debug.LogError("UpdateDownload _request is Null");
                return;
            }

            _request.progress = www.downloadProgress;
            if (!www.isDone)
                return;

            if (_request == null) {
                Debug.LogError("OnDownloadCompleted _request is Null");
                return;
            }

            if (www.result == UnityWebRequest.Result.Success) {
                audioClip = DownloadHandlerAudioClip.GetContent(www);
                // 建立写沙盒的请求
                SandboxStorage.DownLoadFinish((ulong)www.downloadHandler.data.Length, _savePath, www.downloadHandler.data);
                
                _request.asset = audioClip;
                _request.SetResult(Request.Result.Success);
                _step = Step.None;
                return;
            }
            
            _request.SetResult(Request.Result.Failed, www?.error);
            _step = Step.None;
        }
        

        private void UpdateLoadLocal()
        {
            if (www == null)
            {
                // 沙盒路径是 MD5 哈希（无扩展名），格式依赖 OnStart 中记录的 _audioType
                www = UnityWebRequestMultimedia.GetAudioClip("file://" + _savePath, _audioType);
                www.SendWebRequest();
            }
            
            if (!www.isDone) 
                return;
            
            if (!string.IsNullOrEmpty(www.error))
            {
                OnLocalLoadFail(www.error);
                return;
            }
            
             audioClip = DownloadHandlerAudioClip.GetContent(www);
            _request.asset = audioClip;
            _request.SetResult(Request.Result.Success);
        }

        protected virtual void OnLocalLoadFail(string msg)
        {
            // 本地加载不存在远端地址
            if (_request.isLocalUrl)
            {
                _request.SetResult(Request.Result.Failed, msg);
                return;
            }

            // 删除本地损坏的文件
            SandboxStorage.DeleteFile(_savePath);
            
            _request.SetResult(Request.Result.Failed, msg);
        }

        private void StartDownload(string url)
        {
            // 使用从 URL 扩展名推断的格式，而非硬编码 MPEG
            www = UnityWebRequestMultimedia.GetAudioClip(url, _audioType);
            www.SendWebRequest();
            _step = Step.Download;
        }
        
        public void Dispose()
        {
            www?.Dispose();
            www = null;
            _savePath = "";
            
            if (audioClip != null)
            {
                Object.Destroy(audioClip);
            }
            audioClip = null;
        }

        public void WaitForCompletion()
        {
            while (!_request.isDone) 
                Update();
        }

        public static AudioRequestHandler CreateInstance(AudioRequest assetRequest)
        {
            return new AudioRequestHandlerRuntime { _request = assetRequest };
        }
    }
}
