using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace xasset
{
    /// <summary>
    ///     负载均衡调度器。
    /// </summary>
    public class Scheduler : MonoBehaviour
    {
        private static readonly Dictionary<string, RequestQueue> _Queues = new Dictionary<string, RequestQueue>();
        private static readonly List<RequestQueue> Queues = new List<RequestQueue>();
        private static readonly Queue<RequestQueue> Append = new Queue<RequestQueue>();
        private static List<string> ImageRequest = new List<string>()
        {
            "RemoteImageRequest",
            "UGCPartRemoteRequest"
        };
        
        private static string OfflineRequest = "OfflineRequest";
        private static float _realtimeSinceStartup;

        [SerializeField] [Tooltip("每个队列最大单帧更新数量。")]
        private int maxRequests = 10000;
        [SerializeField] [Tooltip("最大单帧更新时间片，值越大处理的请求数量越多，值越小处理请求的数量越小，可以根据目标帧率分配。")]
        private float maxUpdateTimeSlice = 1 / 30f;
        [SerializeField] [Tooltip("是否开启自动切片")] private bool autoSlicingEnabled = true;
        public static bool AutoSlicingEnabled { get; set; } = true;
        public static bool Working => Queues.Exists(o => o.working);
        public static bool Busy => Time.realtimeSinceStartup - _realtimeSinceStartup > MaxUpdateTimeSlice && AutoSlicingEnabled;
        public static float MaxUpdateTimeSlice { get; set; }
        public static int MaxRequests { get; set; } = 10000;



        private void Awake()
        {
            AutoSlicingEnabled = autoSlicingEnabled;
            MaxUpdateTimeSlice = maxUpdateTimeSlice;
            MaxRequests = maxRequests;
        }

        private void Update()
        {
            _realtimeSinceStartup = Time.realtimeSinceStartup;
            while (Append.Count > 0)
            {
                var item = Append.Dequeue();
                Queues.Add(item);
            }
            foreach (var queue in Queues) {
                if (!queue.Update())
                    break;
            }
        }

        public static void Enqueue(Request request)
        {
            var key = request.GetType().Name;
            if (!_Queues.TryGetValue(key, out var queue))
            {
                if (ImageRequest.Contains(key))
                {
                    queue = new ImageRequestQueue {key = key, maxRequests = MaxRequests};
                } else if (key == OfflineRequest) {
                    queue = new OfflineRequestQueue { key = key, maxRequests = MaxRequests };
                } else
                {
                    queue = new RequestQueue { key = key, maxRequests = MaxRequests};
                }
                _Queues.Add(key, queue);
                Append.Enqueue(queue);
            }

            queue.Enqueue(request);
        }
    }
}
