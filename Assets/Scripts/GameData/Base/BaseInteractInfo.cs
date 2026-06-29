using UnityEngine;

namespace GameData.Base
{
    public class BaseInteractInfo
    {
        /// <summary>
        /// 点赞状态 1 - 点赞 0 - 没点赞
        /// </summary>
        public int liked;
        /// <summary>
        /// 点赞数量
        /// </summary>
        public int likeAmount;
        /// <summary>
        /// 消费状态 1-已拥有 0未拥有
        /// </summary>
        public int consumed; 
        /// <summary>
        /// 消费数量 map-访问数 -衣服-购买书
        /// </summary>
        public int consumeAmount;
        /// <summary>
        /// 收藏状态
        /// </summary>
        public int collected;
        /// <summary>
        /// 收藏数量
        /// </summary>
        public int collectAmount;
        /// <summary>
        /// 评论数
        /// </summary>
        public int commentAmount;

        /// <summary>
        /// 打赏金币数量
        /// </summary>
        public int rewardAmount;

        /// <summary>
        /// 分享数量
        /// </summary>
        public int shareAmount;

        /// <summary>
        /// 地图热度值
        /// </summary>
        public int heatAmount;

        /// <summary>
        /// 投票
        /// </summary>
        public int voted;
    }
}