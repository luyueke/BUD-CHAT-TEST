// @Author: YangJie
// @Description:
// @Date:  2023/09/13
// @Modify:

using GameData.Base;
using GameData.BaseInfo;
using Newtonsoft.Json;

namespace GameData.UGCData
{
    public static class UGCExtension
    {

        public static SkinInfo ToSkinInfo(this UgcBaseInfo data)
        {
            if (data is SkinInfo skinInfo)
            {
                return skinInfo.Clone();
            }
            skinInfo = JsonConvert.DeserializeObject<SkinInfo>(JsonConvert.SerializeObject(data));
            skinInfo.clothesUrl = data.textureUrl;
            return skinInfo;
        }

        public static PropInfo ToPropInfo(this UgcBaseInfo data)
        {
            if (data is PropInfo propInfo) {
                return propInfo.Clone();
            }
            var info = JsonConvert.DeserializeObject<PropInfo>(JsonConvert.SerializeObject(data));
            return info;
        }

        public static T Clone<T>(this T data) where T : UgcBaseInfo
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data));
        }
        
        public static T CloneSkinActionInfo<T>(this T data) where T : SkinActionInfo
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data));
        }

        public static void CopyTo(this UgcBaseInfo from, UgcBaseInfo to) {
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(from), to);
        }

    }
}
