using System.Collections;
using System.Collections.Generic;
using GameData.Config;
using GameData.PgcData;
using UnityEngine;

namespace Game.Config
{
    public enum PackageType
    {
        CN = 0,
        US,
    }

    public class GameConsts
    {
        public static PackageType PackageType = PackageType.CN;
        public static string clothMeshNodeName = "ugcClothNode";
        public static string tailMeshNodeName = "ugcTailNode";
        public static string bagMeshNodeName = "ugcBagNode";
        public static string handMeshNodeName = "ugcHandNode";
        public static string shoeMeshNodeName = "ugcShoeNode";
        public static string hatMeshNodeName = "ugcHatNode";
        public static string glassesMeshNodeName = "ugcGlassesNode";
        public static string mouthMeshNodeName = "ugcMouthNode";
        public static string facePaintMeshNodeName = "ugc_face";
        public static string bodyNodeName =  "UgcBody";
        public static string ClothesAssetDir = "Assets/Loadable/Avatar/UGCRolePart/";
        public static string PetClothesAssetDir = "Assets/Loadable/Pet/UGCRolePart/";
        public static string petFacePaintMeshNodeName = "pet_ugc_pattern";
        public static float TimeScale = 0.0167f;//供Handler等使用，默认一帧的间隔时间
        public static string PlayerID = "selfID"; // 测试用 - 后续联机调通时删掉
        public static string AIBuddyTag = "AIBuddyTag_";

        public static string SpecialAnimDir = "Assets/Loadable/Animations/SpecialSkin/";


#if PACKAGE_TYPE_US
        public static string BusinessBaseHost = "https://us-business-1318932159.cos.na-siliconvalley.myqcloud.com";
        public static string BusinessBaseUrl = "https://us-business-1318932159.cos.na-siliconvalley.myqcloud.com/";
		public static string AccBusinessBaseUrl = "https://us-business-1318932159.cos.accelerate.myqcloud.com/";
#else
        public static string BusinessBaseHost = "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com";
        public static string BusinessBaseUrl = "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com/";
		public static string AccBusinessBaseUrl = "https://u3d-business-data-1318932159.cos.accelerate.myqcloud.com/";
#endif


#if PACKAGE_TYPE_US
        public static string BusinessCdnUrl = "https://cdn-global.joinbudapp.com/";
        public static string BusinessCdnHost = "https://cdn-global.joinbudapp.com";
#else
        public static string BusinessCdnUrl = "https://cdn.budapp.cn/";
        public static string BusinessCdnHost = "https://cdn.budapp.cn";
#endif



        public static float doubleAnimDistance = 0.7f;
        /// <summary>
        /// 场景内发布素材 ID
        /// </summary>
        public static string ScenePropDraftId = "ScenePropDraftId";

        // 素材上传需要忽视的Component
        public static List<uint> UgcItemIngoreComponent = new List<uint>()
        {
            (uint)NodeComponentId.RPAnimComponent,
            (uint)NodeComponentId.MovementComponent,
            (uint)NodeComponentId.ActiveCtrComponent,
        };

        //默认创建道具ID
        public static string CombinePropId = "20100023";
        // 文字道具ID（旧版保留用于兼容已发布数据，新版用于新创建）
        public const string OldDTextPropId = "20100032";
        public const string DTextPropId = "20100071";
        public static string DefaultStoneId = "20200001";
        public static string DefaultPlantId = "20200101";
        public static string DefaultEffectId = "20200201";

        public static readonly Vector2 UGCItemShotSize = new Vector2(846, 846);
        public static readonly Vector2 UGCPoseShotSize = new Vector2(1024, 1024);
        public static readonly Vector2 UGCMapShotSize = new Vector2(1740, 1113);

        public static float NODE_MIN_SCALE = 0.1f;
        public static float NODE_MAX_SCALE = 1000f;
        //默认创建道具ID
        public static string NewbeeDefCloth = "10400001";
        public static string InfoDir => Application.persistentDataPath + "/LocalInfo/";
#if PACKAGE_TYPE_US
        public static string SettingLocalInfo = "settingLocalInfo_us.json";
#else
        public static string SettingLocalInfo = "settingLocalInfo.json";
#endif

        // ugc皮肤卷 购买价格限制
        public static int UgcSkinTicketPriceLimit = 20;

        //UGC透明皮肤模版
        public static List<string> TransparentPart = new List<string>() {"50600001", "51100001", "51200003","80600001", "81100001", "81200001"};
        public const string FullTransparentPartId = "51200003";


        #region

        /// <summary>
        /// 正常背景音乐响度
        /// </summary>
        public const int BGMusicLoudness = -27;

        /// <summary>
        /// 压低背景音乐时响度
        /// </summary>
        public const int BGMusicLoudnessReduce = -41;


        /// <summary>
        /// 音色音乐响度
        /// </summary>

        public const int ToneMusicLoudness = -22;


        #endregion
        public static string[] AnimTabs = {"待机", "待机辅助", "开心", "生气", "沮丧", "惊讶", "害怕", "困惑"};
        public static string EmoteOtherPlayerOcKey = "EmoteOtherPlayerOcKey-";
    }


    /// <summary>
    /// 地图 顶点及内存限制设置
    /// </summary>
    public class MapLimitSetting
    {
        public const int MAX_VERTEX_COUNT = 2000000;
        // 341KB
        public const int MAX_MATERIAL_COUNT = 100;

        // 3D 文字数限制
        public const int MAX_DTEXT_COUNT = 2000;

        public const int MAX_MEMORY = 300*1024*1024;
    }

    public class PropLimitSetting
    {
        // 单素材限定 3万顶点
        public const int MAX_VERTEX_COUNT = 30000;

        // UGC 材质 10 个
        public const int MAX_MATERIAL_COUNT = 10;

        public const int MAX_DTEXT_COUNT = 500;
    }

    public class PropClothLimitSetting {
        // 衣服素材限定 5000顶点
        public const int MAX_VERTEX_COUNT = 5000;

        // UGC 材质 10 个
        public const int MAX_MATERIAL_COUNT = 10;

        public const int MAX_DTEXT_COUNT = 50;
    }

    public class PropBagLimitSetting {
        // 背包素材限定 5000顶点
        public const int MAX_VERTEX_COUNT = 3000;

        // UGC 材质 10 个
        public const int MAX_MATERIAL_COUNT = 10;

        public const int MAX_DTEXT_COUNT = 50;
    }
}

