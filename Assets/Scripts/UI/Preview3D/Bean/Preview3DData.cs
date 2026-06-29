using System;
using GameData.PgcData;
using UI.Manager;
using UI.Preview3D.Base;

namespace UI.Preview3D.Bean
{
    public class Preview3DData
    {
        public Preview3DType PreviewType;
        public int ResType;
        public string PgcIdStr; //衣服/表情/系列皮肤等的唯一id string

        // public Preview3DSource Source; //调起预览的来源

        public PreviewParams PreviewParams; //预览位置调整

        public static Preview3DType ParseTypeByRewardType(int pgcId)
        {
            var pgcCfg = PgcUtils.GetPgcConfigData(pgcId);

            return (ResourceType)pgcCfg.ResourceType switch
            {
                ResourceType.Avatar => Preview3DType.Avatar,
                ResourceType.Emote => Preview3DType.Emote,
                ResourceType.Currency => Preview3DType.Gold,
                _ => Preview3DType.Unknown,
            };
        }
    }
}