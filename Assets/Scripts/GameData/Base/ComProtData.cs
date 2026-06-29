namespace GameData.Base.Common
{
    public class SetTypeReqeust
    {
        public string id;
        public int setType;
    }

    public class CollectRequest : SetTypeReqeust
    {
        /// <summary>
        /// 1是pgc
        /// </summary>
        public int isPgc;
    }
}