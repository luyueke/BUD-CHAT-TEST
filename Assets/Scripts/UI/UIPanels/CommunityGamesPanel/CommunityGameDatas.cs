using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Network.Http;
using UnityEngine;

namespace Game.CommunityGame
{
    public class RecommendUgcInfo : UgcBaseInfo
    {
        public int ugcStyle;
        public PaymentInfo paymentInfo;
    }
    public class RecommendItemData
    {
        public RecommendUgcInfo ugcInfo;
        public BaseCreator creatorInfo;
        public BaseInteractInfo interactInfo;
        //兼容V2数据结构
        public string ugcId;
        public int ugcType;
        //序列化的UgcBaseInfo
        public string ugcData;
    }
    
    #region 官方游戏section 展示/recommend/ugc/map

    public class OfficalRecommendItem:SectionItemData
    {
        public List<RecommendItemData> ugcList;
    }
    
    public class OfficalRecommendRsp
    {
        public List<OfficalRecommendItem> sections;
    }
    #endregion
    
    #region 精选section recommend/ugc/section回包的数据
    public class SectionItemData
    {
        public string sectionId;
        public string sectionName;
    }

    public class SectionListRsp
    {
        public List<SectionItemData> list;
    }
    #endregion

    #region 精选具体section列表/recommend/ugc/sectionInfo
    public class SectionInfoRsp : HttpPageBaseData
    {
        public List<RecommendItemData> list;
    }
    #endregion
    
    public class StartEndData
    {
        public float start;
        public float end;
    }
}
