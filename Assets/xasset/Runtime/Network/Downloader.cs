using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace xasset
{
    public enum RequestType
    {
        UnityWebRequest,
        HttpWebRequest
    }

    [DisallowMultipleComponent]
    public class Downloader : MonoBehaviour
    {
        public static readonly Queue<DownloadContentRequest> Queue = new Queue<DownloadContentRequest>();
        private static readonly List<DownloadContentRequest> Progressing = new List<DownloadContentRequest>();
        private static readonly Queue<DownloadContentRequest> Unused = new Queue<DownloadContentRequest>();

        [Range(1, 10)] [SerializeField] private byte maxRequests = 10;
        [Range(0, 3)] [SerializeField] private byte maxRetryTimes = 2;
        [SerializeField] private bool simulationMode;
        [SerializeField] private bool resumable = true;
        [SerializeField] private RequestType requestType = RequestType.UnityWebRequest;

        public static Func<DownloadContentRequest, DownloadContentRequestHandler> CreateHandler { get; set; }
        public static byte MaxRequests { get; set; } = 5;
        public static byte MaxRetryTimes { get; set; } = 2;
        public static bool IsDownloading => Queue.Count > 0 || Progressing.Count > 0;
        public static bool SimulationMode { get; set; }
        public static bool Paused { get; private set; }
        public static bool Resumable { get; set; } = true;

        /// <summary>
        /// 下载重试时触发。参数：(url, 当前重试次数)。
        /// 游戏层订阅此事件可在第 N 次重试后弹出"网络不稳定"提示。
        /// 注意：此回调在主线程触发，可以直接操作 UI。
        /// </summary>
        public static System.Action<string, int> OnDownloadRetry;

        private void Awake()
        {
            if (Application.isEditor) SimulationMode = simulationMode;
            MaxRequests = maxRequests;
        }

        private void Start()
        {
            MaxRetryTimes = maxRetryTimes;
            Resumable = resumable;

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            if (Assets.IsWebGLPlatform)
                requestType = RequestType.UnityWebRequest;

            if (requestType == RequestType.HttpWebRequest)
                CreateHandler = request => new DownloadContentRequestHandlerHWR(request);
            else
                CreateHandler = request => new DownloadContentRequestHandlerUWR(request);
        }

        private void Update()
        {
            UpdateAll();
        }

        private void OnDestroy()
        {
            CancelAll();
        }

        public static DownloadContentRequest Get(DownloadContent content)
        {
            if (!Resumable)
                content.Clear();

            var contentRequest = Progressing.Find(request => request.content == content);
            return contentRequest ?? DownloadAsync(content);
        }

        public static DownloadContentRequest DownloadAsync(DownloadContent content)
        {
            if (!Resumable)
                content.Clear();

            DownloadContentRequest contentRequest;
            if (Unused.Count > 0)
            {
                contentRequest = Unused.Dequeue();
                contentRequest.Reset();
            }
            else
            {
                contentRequest = new DownloadContentRequest();
            }

            contentRequest.content = content;
            contentRequest.SendRequest();
            contentRequest.handler = Assets.IsWebGLPlatform
                ? new DownloadContentRequestHandlerUWR(contentRequest)
                : CreateHandler(contentRequest);

            return contentRequest;
        }

        public static DownloadContentRequest DownloadAssetBundleAsync(DownloadContent content, string cacheFolder = null)
        {
            if (!Resumable)
                content.Clear();

            DownloadContentRequest contentRequest;
            if (Unused.Count > 0)
            {
                contentRequest = Unused.Dequeue();
                contentRequest.Reset();
            }
            else
            {
                contentRequest = new DownloadContentRequest();
            }

            contentRequest.content = content;
            contentRequest.SendRequest();
            contentRequest.handler = new DownloadAssetBundleRequestHandlerUWR(contentRequest, cacheFolder);

            return contentRequest;
        }

        public static DownloadContentRequest DownloadImageAsync(DownloadContent content)
        {
            if (!Resumable)
                content.Clear();

            DownloadContentRequest contentRequest;
            if (Unused.Count > 0)
            {
                contentRequest = Unused.Dequeue();
                contentRequest.Reset();
            }
            else
            {
                contentRequest = new DownloadContentRequest();
            }

            contentRequest.content = content;
            contentRequest.SendRequest();
            contentRequest.handler = new DownloadImageRequestHandlerUWR(contentRequest);

            return contentRequest;
        }

        public static void Pause()
        {
            if (Paused) return;
            foreach (var request in Progressing) request.Pause();
            Paused = true;
        }

        public static void UnPause()
        {
            if (!Paused) return;
            foreach (var request in Progressing) request.UnPause();
            Paused = false;
        }

        private static void UpdateAll()
        {
            if (Paused) return;

            while (Queue.Count > 0 && (Progressing.Count < MaxRequests || MaxRequests == 0))
            {
                var item = Queue.Dequeue();
                if (item.status == DownloadRequest.Status.Wait) item.Start();

                Progressing.Add(item);
            }

            for (var index = 0; index < Progressing.Count; index++)
            {
                var item = Progressing[index];
                if (item.isDone)
                {
                    Progressing.RemoveAt(index);
                    index--;
                    Complete(item);
                    continue;
                }

                item.Update();
            }

            DownloadContentRequestBatch.UpdateAll();
        }

        private static void Complete(DownloadContentRequest request)
        {
            request.Complete();
            switch (request.result)
            {
                case DownloadRequest.Result.Success:
                    Unused.Enqueue(request);
                    break;
                case DownloadRequest.Result.Cancelled:
                    Unused.Enqueue(request);
                    break;
                case DownloadRequest.Result.Failed:
                    break;
                case DownloadRequest.Result.Default:
                    break;
                default:
                    throw new Exception($"Invalid download status {request.status}");
            }
        }

        private static void CancelAll()
        {
            foreach (var request in Progressing) request.Cancel();

            Progressing.Clear();
            Queue.Clear();

            DownloadContentRequestBatch.CancelAll();
        }
    }
}