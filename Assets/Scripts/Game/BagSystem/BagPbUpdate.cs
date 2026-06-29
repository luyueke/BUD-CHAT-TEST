using Network.Http;
using Network;
using Newtonsoft.Json;
using System;
using GameData.PgcData;
using System.Collections.Generic;

namespace Game.Database
{
    /// <summary>
    /// 背包数据检查更新类
    /// </summary>
    public class BagPbUpdate
    {
        public enum State
        {
            Free,
            CheckRequest,
            DownloadPb,
            Dispose
        }

        private State state = State.Free;

        internal void Check(BagCheckData nowData, Action<bool, bool> action)
        {
            state = State.DownloadPb;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.Bag, HttpMethod.POST, JsonConvert.SerializeObject(nowData),
            (string msg) =>
            {
                if (state == State.Dispose) return;
                var data = JsonConvert.DeserializeObject<BagCheckData>(msg);

                var change = false;
                // 这里直接返回了pb的string 所以没有下载的流程
                for (int i = 0, C = data.pairList.Count; i < C; i++)
                {
                    var pairData = data.pairList[i];
                    if (pairData.success == 0 || pairData.data == null) continue;

                    int key = pairData.dataType;
                    int cookie = pairData.version;
                    byte[] bytes = Convert.FromBase64String(pairData.data);
                    if (BagPbFileIO.CheckPbCookieIsOld(key, cookie, BagPbFileIO.ComputeHash(bytes)))
                    {
                        change = true;
                        var success = BagPbFileIO.SetPbFile(key, cookie, bytes);
                        if (success != null) BagDatabase.Inst.UpdateData(key, success, bytes);
                    }
                }

                action?.Invoke(true, change);
            },
            (string failMsg) =>
            {
                if (state == State.Dispose) return;
                state = State.Free;
                action?.Invoke(false, false);
            });
        }

        public void Dispose()
        {
            state = State.Dispose;
        }
    }

    public class PairData
    {
        public int dataType;
        public int version;
        public string data;
        public int success;
    }

    public class BagCheckData
    {
        public List<PairData> pairList;
    }
}
