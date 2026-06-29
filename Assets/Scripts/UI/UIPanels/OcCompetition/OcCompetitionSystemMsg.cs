using GameData;
using GameData.Base;
using GameData.BaseInfo;
using System.Collections.Generic;


namespace GameUI
{
    public class OcCptListMsg
    {
        public List<OcCptListMsgItem> list;

        public OcCptUserRankData userRankData;

        public string cookie;

        public int isEnd;
    }

    public class OcCptListMsgItem
    {
        public ContestCreationInfo creationInfo;
        public ScoreInfo scoreInfo;
        public BaseCreator creator;
        public BaseInteractInfo interactInfo;
    }

    public class OcCptUserRankData
    {
        public BaseCreator creator;
        public ScoreInfo scoreInfo;
    }

    public class ContestCreationInfo
    {
        public string creationId;
        public string cover;
        public int creationType; // 1:皮肤创作，2:oc 创作
        //public long createTime;
        //public string creationName;
        public PaymentInfo paymentInfo;

        public PoseInfo poseInfo;
        public ContestOcInfo ocInfo;
    }

}