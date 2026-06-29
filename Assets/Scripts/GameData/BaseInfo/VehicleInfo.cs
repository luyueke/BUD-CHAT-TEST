using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using GameData.Base;
using Newtonsoft.Json;
using UnityEngine;


namespace GameData.BaseInfo
{
    public enum VehicleType
    {
        Single = 1,
        Double = 2,
    }

    public enum VehicleAniType
    {
        None = 0,
        Vibration = 1,//震动
        Drift = 2,//浮动
    }

    public class VehicleTryPlayInfo
    {
        public VehicleInfo vehicleInfo;
        public bool isPlay;//是否预览
    }

    public class VehicleBannerInfo
    {
        public bool bannerStatus;

        public string bannerText;

        // 娃娃机抓取补偿（借 BannerJson 持久化给晚加入玩家）：下标=catch slot，值=被劫持玩家uid，空位为 null。
        // 与横幅字段互不干扰；为 null 时表示无抓取数据（不影响纯横幅车）。
        public System.Collections.Generic.List<string> capturedSlots;
    }

    public class VehicleInfo : UgcBaseInfo
    {
        public override string templateId { get; set; } = "80000001";

        public int vehicleType = 1;//1单人，2双人

        [JsonConverter(typeof(StringObjectConverter))]
        public VehicleDetailInfo detailInfo;

        public float vehicleHeight;//驾驶高度

        /// <summary>地图还原的URl</summary>
        public string mapUrl = "";

        public string curPoseData = "";//驾驶姿势

        public string doublePoseData = "";//双人驾驶姿势

        [JsonConverter(typeof(StringObjectConverter))]
        public VehicleAudioDetailInfo vehicleAudio;//声音

        public int runAniType = 0;//运行动画类型

        public int endAniType = 0;//停止动画类型

        [JsonConverter(typeof(StringObjectConverter))]
        public VehicleDetailInfo doubleUserDetail;//第二个玩家的偏移

        public PaymentInfo paymentInfo;

        public int isHidden = 1;//是否隐藏

        public int ugcStyle;//0：正常 1：二次元风格

        /// <summary>
        /// 审核图 URL
        /// </summary>
        public string verifyUrl;

#if UNITY_EDITOR
        /// <summary>
        /// 仅用于运行时传递数据给 TempVehicleDataSave 进行保存，不参与序列化
        /// </summary>
        [JsonIgnore]
        public byte[] metaData;
        #endif
        
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        
        public int CompareTo(VehicleInfo other)
        {
            bool idEqual = this.id == other.id;
            return idEqual ? 0 : -1;
        }
    
    }

    public class VehicleDetailInfo : DetailInfo
    {
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



        public static VehicleDetailInfo FromDetailInfo(DetailInfo detailInfo)
        {
            var vehicleDetailInfo = new VehicleDetailInfo()
            {
                vertexs = detailInfo.vertexs,
                triangles = detailInfo.triangles,
                anchor = detailInfo.anchor,
                materials = detailInfo.materials,
                dTexts = detailInfo.dTexts,
            };
            return vehicleDetailInfo;
        }

        public void Assign(DetailInfo detailInfo)
        {
            vertexs = detailInfo.vertexs;
            triangles = detailInfo.triangles;
            size = detailInfo.size;
            scale = detailInfo.scale;
        }
    }

    public class VehicleAudioDetailInfo
    {
        public string starUrl = "";

        public VehicleWwiseInfo starWwise;

        public string driveUrl = "";

        public VehicleWwiseInfo driveWwise;

        public string hornUrl = "";

        public VehicleWwiseInfo hornWwise;

    }

    public class VehicleWwiseInfo
    {
        public string group = "";

        public string switchs = "";

        public string wwise = "";

        public string wwise3P = "";

        public string stopWwise = "";

        public string stopWwise3P = "";
    }


}
