using System.Collections.Generic;
using System.Linq;

namespace xasset
{
    public class ImageRequestQueue: RequestQueue
    {
        public override bool working => false;
    }

    public class OfflineRequestQueue : RequestQueue {
        public override void Enqueue(Request request) {
            
            var lastIndex = queue.FindIndex(r => request.priority >= r.priority);
            if (lastIndex != -1) {
                queue.Insert(lastIndex, request);
            }
            else {
                queue.Add(request);
            }
        }
    }


    public class RequestQueue
    {
        protected readonly List<Request> processing = new List<Request>();
        protected readonly List<Request> queue = new List<Request>();
        public string key;
        public int maxRequests { get; set; } = 10000;
        public virtual bool working => processing.Count > 0 || queue.Count > 0;

        public virtual void Enqueue(Request request)
        {
            queue.Add(request);
        }

        public int Count => queue.Count + processing.Count;

        public bool Update() {
            
            while (queue.Count > 0 && (processing.Count < maxRequests || maxRequests == 0))
            {
                var item = queue.FirstOrDefault();
                queue.Remove(item);
                if (item == null)
                {
                    continue;
                }
                processing.Add(item);
                if (item.status == Request.Status.Wait) item.Start();
                if (Scheduler.Busy) return false;
            }

            for (var index = 0; index < processing.Count; index++)
            {
                var item = processing[index];
                if (item.Update()) continue;
                processing.RemoveAt(index);
                index--;
                item.Complete();
                if (Scheduler.Busy) return false;
            }
            return true;
        }
    }
}
