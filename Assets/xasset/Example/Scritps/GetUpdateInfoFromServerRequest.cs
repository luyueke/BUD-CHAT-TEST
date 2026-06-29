using Newtonsoft.Json;
using UnityEngine;

namespace xasset
{
//     public class GetUpdateInfoFromServerRequest : Request
//     {
//         public ABUpdateInfo info { get; private set; }
//         public bool LocalDebug = false;
//         public int Quality = 0;
//         protected override void OnStart()
//         {
// #if LOCAL_BUILD
//             SetResult(Result.Failed);
//             return;
// #endif
//             if (Assets.SimulationMode || Assets.OfflineMode)
//             {
//                 SetResult(Result.Failed);
//                 return;
//             }
//
//             if (LocalDebug)
//             {
//                 info = new ABUpdateInfo()
//                 {
//                     quality = 0,
//                     file = "versions_v133481203436500998.json",
//                     hash = "f1a92cad09a05e0aa8a4c57119357281",
//                     size = 11710,
//                     timestamp = 133481203436500998,
//                     downloadURL = "http://localhost:8080/"
//                 };
//                 Assets.DownloadURL = $"{info.downloadURL}{Assets.Bundles}/{Assets.Platform}";
//                 SetResult(Result.Success);
//                 return;
//             }
//             var para = new ABResourcePara()
//             {
//                 quality = Quality,
// #if UNITY_ANDROID
//                 platform = "android"
// #else
//                 platform = "ios"
// #endif
//             };
//             HttpUtils.MakeHttpRequestWithRetry("/other/ugcHotUpdateConfig", (int)HTTP_METHOD.GET, JsonConvert.SerializeObject(para), OnServerMsg, OnServerError);
//         }
//
//         private void OnServerMsg(string msg)
//         {
//             try
//             {
//                 Debug.LogError("请求服务器AB版本配置 + " + msg);
//                 var msgObj = JsonConvert.DeserializeObject<ABResourceConfig>(msg);
//                 info = JsonConvert.DeserializeObject<ABUpdateInfo>(msgObj.data);
//                 if (string.IsNullOrEmpty(info.file)) SetResult(Result.Failed);
//                 Assets.DownloadURL = $"{info.downloadURL}{Assets.Bundles}/{Assets.Platform}";
//                 SetResult(Result.Success);
//             }
//             catch
//             {
//                 SetResult(Result.Failed, $"JsonConvert Failed :{msg}");
//             }
//         }
//
//         private void OnServerError(string error)
//         {
//             SetResult(Result.Failed, error);
//         }
//     }

    public class ABResourceConfig
    {
        public int result;
        public string data;
    }

    public class ABResourcePara
    {
        public string platform;
        public int quality;
    }
}
