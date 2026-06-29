// @Author: YangJie
// @Description:
// @Date:  2023/08/29
// @Modify: 素材数据

using GameData.Base;
using GameData.OfflineRender;
using Newtonsoft.Json;
using UnityEngine;

namespace GameData.BaseInfo
{
    public class PropInfo : UgcBaseInfo
    {
        /// <summary>地图还原的URl</summary>
        public string mapUrl = "";


        [JsonConverter(typeof(ABConfigConverter))]
        // ReSharper disable once UnassignedField.Global
        public ABConfig abConfig;

        public PaymentInfo paymentInfo;

        public DetailInfo detailInfo;

        /// <summary>
        /// 审核图 URL
        /// </summary>
        public string verifyUrl;
        
        public int ugcStyle;//0：正常 1：二次元风格
    }


    public class DetailInfo
    {
        public int vertexs;
        public int triangles;
        public int materials = 0;
        public int dTexts = 0;
        public Vector3 anchor;
        public Vector3 scale = Vector3.one;
        public Vector3 size = Vector3.one;
        
        public static DetailInfo FromSkinDetailInfo(SkinDetailInfo skinDetailInfo) {
            var detailInfo = new DetailInfo() {
                vertexs = skinDetailInfo.vertexs,
                triangles = skinDetailInfo.triangles,
                anchor = skinDetailInfo.anchor,
                dTexts = skinDetailInfo.dTexts,
            };
            return detailInfo;
        }
    }

}
