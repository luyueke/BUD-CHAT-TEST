// NetCmd.cs
// Create by xiaojl Mar/15/2023
// 网络命令编号

namespace Network.Tcp.Core
{
    public enum NetCmd
    {
        CMD_PING = 1, // 心跳
        CMD_TIMESTAMP = 2, // 时间戳

        // 业务相关[100~999]
        CMD_LOGIN = 100, // 登录

        C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC = 201, // 客户端同步CMD


        // CMD_ANTI_ADDICTION            = 101, // 防沉迷
        // CMD_SUBSCRIBE                 = 102, // 订阅
        // CMD_BATCH_USER_INFO_LITE      = 103, // 批量获取用户信息
        // CMD_SYNC_USER_DATA            = 104, // 同步玩家数据
        // CMD_SYNC_AVATAR_DATA          = 105, // 同步试衣间列表数据
        // CMD_SYNC_VENDING_MACHINE_DATA = 106, // 同步售卖机列表数据
        // CMD_SYNC_LOTTERY_DATA         = 107, // 同步抽奖列表数据(转盘、扭蛋机)
        // CMD_SYNC_STORE_DATA           = 108, // 同步商城列表数据
        // C_CMD_VENDING_SERIES          = 110, // 售卖机series列表
        // C_CMD_VENDING_PRODUCTS        = 111, // 售卖机商品列表
        // C_CMD_RED_DOT                 = 112, // 请求红点
        // C_CMD_TASK_SNAPSHOT           = 113, // 任务快照数据
    }
}