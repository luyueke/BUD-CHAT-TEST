using System;
using System.Collections.Generic;
using NetEngine.src.Broadcast;

namespace NetEngine.src.Util
{
    public class BstCallbacks
    {
        // 房间广播
        public InnerRoomBstHandler InnerRoomBst = new InnerRoomBstHandler();

        // 清除全部广播回调函数
        public void ClearCallbacks()
        {
            this.InnerRoomBst.ClearCallbacks();
        }

        // 本地网络变化
        public void OnNetwork(string tag,ResponseEvent eve)
        {
            this.InnerRoomBst.OnNetwork(tag,eve);
        }
    }

    public class CallbackHandler<T>
    {
        private readonly HashSet<T> _broadcasts = new HashSet<T>();

        public void BindCallbacks(T broadcast)
        {
            if (!this._broadcasts.Contains(broadcast))
            {
                this._broadcasts.Add(broadcast);
            }
        }

        public void UnbindCallbacks(T broadcast)
        {
            if (this._broadcasts.Contains(broadcast))
            {
                this._broadcasts.Remove(broadcast);
            }
        }

        public void ClearCallbacks()
        {
            this._broadcasts.Clear();
        }

        protected void HandleBst(Action<T> action)
        {
            foreach (var broadcast in this._broadcasts)
            {
                action(broadcast);
            }
        }
    }

    public class InnerRoomBstHandler : CallbackHandler<RoomBroadcast>
    {
        public void OnPlayerEnter(BroadcastEvent eve)
        {
            this.HandleBst(broadcast => broadcast?.OnPlayerEnter(eve));
        }

        public void OnPlayerLeave(BroadcastEvent eve)
        {
            this.HandleBst(broadcast => broadcast?.OnPlayerLeave(eve));
        }

        public void OnBstFrameData(BroadcastEvent eve)
        {
            this.HandleBst(broadcast => broadcast?.OnBstFrameData(eve));
        }

        public void OnNetwork(string tag,ResponseEvent eve)
        {
            this.HandleBst(broadcast => broadcast?.OnNetwork(tag,eve));
        }

        public void OnCommonSyncBst(BroadcastEvent eve)
        {
            this.HandleBst(broadcast => broadcast?.OnCommonSyncBst(eve));
        }
    }
}