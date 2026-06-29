// @Author: YangJie
// @Description:
// @Date:  2023/09/25
// @Modify:

using System;
using Game.Config;
using Newtonsoft.Json;
using UnityEngine;

namespace GameData.OfflineRender {
    public class ABConfig {
        public MeshState state;
        public string highABUrl;
        public string lowABUrl;
        public string combABUrl;
    }

    public class ABConfigConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue(JsonConvert.SerializeObject(value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            var abConfigObj = JsonConvert.DeserializeObject(reader.Value as string ?? string.Empty, objectType);
            if (abConfigObj is ABConfig abConfig) {
                var suffix = ".a";
#if UNITY_IOS
                suffix = ".i";
#endif
                if (!string.IsNullOrEmpty(abConfig.highABUrl)) {

                    if (abConfig.highABUrl.Contains(GameConsts.BusinessBaseUrl)) {
                        abConfig.highABUrl =
                            abConfig.highABUrl.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
                    }

                    if (!abConfig.highABUrl.EndsWith(suffix)) {
                        abConfig.highABUrl = (abConfig.highABUrl + suffix).Replace("\\", "/");
                    }
                }

                if (!string.IsNullOrEmpty(abConfig.lowABUrl)) {
                    if (abConfig.lowABUrl.Contains(GameConsts.BusinessBaseUrl)) {
                        abConfig.lowABUrl =
                            abConfig.lowABUrl.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
                    }
                    if (!abConfig.lowABUrl.EndsWith(suffix)) {
                        abConfig.lowABUrl = (abConfig.lowABUrl + suffix).Replace("\\", "/");
                    }
                }

                if (!string.IsNullOrEmpty(abConfig.combABUrl)) {
                    if (abConfig.combABUrl.Contains(GameConsts.BusinessBaseUrl)) {
                        abConfig.combABUrl =
                            abConfig.combABUrl.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
                    }
                    if (!abConfig.combABUrl.EndsWith(suffix)) {
                        abConfig.combABUrl = (abConfig.combABUrl + suffix).Replace("\\", "/");
                    }
                }
            }

            return abConfigObj;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(string);
        }
    }
}
