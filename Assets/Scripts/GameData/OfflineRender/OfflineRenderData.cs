// @Author: YangJie
// @Description: 离线渲染数据
// @Date:  2023/09/19
// @Modify:

using Game.OfflineRender;
using Newtonsoft.Json;

namespace GameData.OfflineRender {
    public class OfflineRenderData {
        public string id;

        [JsonConverter(typeof(ABConfigConverter))]
        public ABConfig abConfig;

        public string metaDataUrl;

        public int optimizeLevel;

        public string GetKey(ModelLODType lodType) {
            if (abConfig == null) {
                return id;
            }
            if (IsSample()) {
                return $"{id}_{ModelLODType.LOW.ToString()}";
            }

            if (IsCombined()) {
                return $"{id}_Combined";
            }

            return $"{id}_{lodType.ToString()}";
        }

        public string GetPath(ModelLODType lodType) {
            if (IsSample()) {
                return abConfig.lowABUrl;
            }

            if (IsCombined()) {
                return abConfig.combABUrl;
            }

            return lodType == ModelLODType.LOW ? abConfig.lowABUrl : abConfig.highABUrl;
        }


        public bool IsValid() {
            return abConfig != null &&
                   (!string.IsNullOrEmpty(abConfig.highABUrl) || !string.IsNullOrEmpty(abConfig.lowABUrl));
        }

        public bool IsSample() {
            if (abConfig == null) {
                return false;
            }
            return string.IsNullOrEmpty(abConfig.highABUrl) && !string.IsNullOrEmpty(abConfig.lowABUrl);
        }

        public bool IsCombined() {
            if (abConfig == null) {
                return false;
            }
            return !string.IsNullOrEmpty(abConfig.combABUrl);
        }

    }
}
