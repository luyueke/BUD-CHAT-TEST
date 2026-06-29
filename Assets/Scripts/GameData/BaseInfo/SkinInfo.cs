// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using Basic.Utils;
using GameData.Base;
using GameData.OfflineRender;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace GameData.BaseInfo {
    public enum CanvasType
    {
        Canvas_32 = 0,
        Canvas_64 = 1
    }
    public enum SkinType
    {
        Avatar = 0,
        Pet = 1
    }
    public class SkinInfo : UgcBaseInfo {
        public override string templateId {
            get;
            set;
        } = "52100001";
        public int skinType;// 0 默认用户skin 1 宠物皮肤
		public int isBan;
        public string[] imgs;

        public int subType;
        public PaymentInfo paymentInfo;

        #region 模板皮肤

        /// <summary>衣服贴图URL</summary>
        public string clothesUrl;

        #endregion

        #region 素材皮肤相关

        /// <summary>地图还原的URl</summary>
        public string mapUrl = "";


        [JsonConverter(typeof(ABConfigConverter))]
        // ReSharper disable once UnassignedField.Global
        public ABConfig abConfig;


        [JsonConverter(typeof(StringObjectConverter))]
        public SkinDetailInfo skinDetailInfo;

        /// <summary>
        /// 审核图 URL
        /// </summary>
        public string verifyUrl;

        public bool isProp = false;
        
        //画板格子数：32*32，64*64
        public int canvasType;

        #endregion

        #region 捆绑包
        // ugcIds;
        public List<string> bundleIdList;
        // 详细信息
        public List<string> bundleItems;
        #endregion

        public int isPrivateOrder;//是否私单
        public int ugcStyle;//0：正常 1：二次元风格
    }

    public class SkinDetailInfo : DetailInfo {
        /// <summary>
        /// 默认位置
        /// </summary>
        public Vector3 pDef;

        /// <summary>
        /// 默认旋转
        /// </summary>
        public Vector3 rDef;

        /// <summary>
        /// 默认缩放
        /// </summary>
        public Vector3 sDef = Vector3.one;

        /// <summary>
        /// 大小限制
        /// </summary>
        public Vector3 sLimit;



        public static SkinDetailInfo FromDetailInfo(DetailInfo detailInfo) {
            var skinDetailInfo = new SkinDetailInfo() {
                vertexs = detailInfo.vertexs,
                triangles = detailInfo.triangles,
                anchor = detailInfo.anchor
            };
            return skinDetailInfo;
        }
        
        public void Assign(DetailInfo detailInfo) {
            vertexs = detailInfo.vertexs;
            triangles = detailInfo.triangles;
            size = detailInfo.size;
        }

    }
    
    
    //皮肤拓展信息 乐器 武器 载具等
    public class SkinActionInfo
    {
        //乐器信息
        public InstrumentInfo instrumentInfo;

        //载具信息
        //public VehicleInfoData vehicleInfo;
    }
}
