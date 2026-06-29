// WWW class usage
#pragma warning disable 0618

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Game.Utils
{
    public class GameSimpleImageDownloader : MonoBehaviour
    {
        public static GameSimpleImageDownloader Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new GameObject(typeof(GameSimpleImageDownloader).Name).AddComponent<GameSimpleImageDownloader>();

                return _Instance;
            }
        }

        static GameSimpleImageDownloader _Instance;


        public int MaxConcurrentRequests { get; set; }

        const int DEFAULT_MAX_CONCURRENT_REQUESTS = 20;

        List<Request> _QueuedRequests = new List<Request>();
        List<Request> _ExecutingRequests = new List<Request>();
        WaitForSeconds _Wait1Sec = new WaitForSeconds(1f);


        IEnumerator Start()
        {
            if (MaxConcurrentRequests == 0)
                MaxConcurrentRequests = DEFAULT_MAX_CONCURRENT_REQUESTS;

            while (true)
            {
                while (_ExecutingRequests.Count >= MaxConcurrentRequests)
                {
                    yield return _Wait1Sec;
                }

                int lastIndex = _QueuedRequests.Count - 1;
                if (lastIndex >= 0)
                {
                    var lastRequest = _QueuedRequests[lastIndex];
                    _QueuedRequests.RemoveAt(lastIndex);

                    StartCoroutine(DownloadCoroutine(lastRequest));
                }

                yield return null;
            }
        }

		void OnDestroy()
		{
			_Instance = null;
		}

		public void Enqueue(Request request)
        { _QueuedRequests.Add(request); }

        public bool Contains(string url)
        {
            var req = _QueuedRequests.Find(x=>x.url == url);
            return req != null;
        }

        IEnumerator DownloadCoroutine(Request request)
        {
            _ExecutingRequests.Add(request);
            var www = UnityWebRequestTexture.GetTexture(request.url);

            yield return www.SendWebRequest();

            if (string.IsNullOrEmpty(www.error))
            {
                if (request.onDone != null)
                {
                    var result = new Result(www);
                    request?.onDone(result);
                }
            }
            else
            {
                if (request.onError != null)
                    request?.onError();
            }
            www.Dispose();
            _ExecutingRequests.Remove(request);
        }

        public class Request
        {
            public string url;
            public Action<Result> onDone;
            public Action onError;
        }

        public class Result
        {
            UnityEngine.Networking.UnityWebRequest _UsedRequest;


			public Result(UnityWebRequest www)
            { _UsedRequest = www; }

            public string GetUrl()
            { return _UsedRequest.url; }

            public Texture2D CreateTextureFromReceivedData()
            { return DownloadHandlerTexture.GetContent(_UsedRequest); }
            
        }
    }
}
#pragma warning restore 0618