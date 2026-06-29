using System;
using GameData.Base;

namespace UGCAsset {

    public enum CacheStatus {
        None,
        Requesting,
        Success,
        Failed,
    }

    public class CacheUGCData<T> where T : UgcBaseInfo {
        public T info;
        public CacheStatus status = CacheStatus.None;
        public Action<T> callBack;

        public CacheUGCData(Action<T> callBack) {
            this.callBack = callBack;
        }

        public void AddCallback(Action<T> onCallBack) {
            callBack += onCallBack;
        }

        public void SetInfo(T tmpInfo) {
            info = tmpInfo;
            if (info != null) {
                status = CacheStatus.Success;
            } else {
                status = CacheStatus.Failed;
            }
        }

        public void InvokeCallBack() {
            try {
                callBack?.Invoke(info);
            } catch (Exception e) {
                LoggerUtils.LogError("InvokeCallBack Error:" + e.Message + "," + e.StackTrace);
            }
            callBack = null;
        }


    }
}
