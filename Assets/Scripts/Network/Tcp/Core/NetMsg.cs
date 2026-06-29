// NetMsg.cs
// Create by xiaojl Mar/15/2023
// 网络消息编号

namespace Network.Tcp.Core
{
    public enum NetMsg
    {
        MSG_PONG = 1, // 心跳
        MSG_TIMESTAMP = 2, // 时间戳

        // 业务相关[1000~9999]
        MSG_LOGIN = 1000, // 登录
        MSG_SYNC_USER_FRIEND_REFRESH = 1002, // 刷新好友在线列表
        MSG_CHAT = 1003,//好友聊天
        MSG_CONFIGURATION_UPDATE = 1004, // 配置更新
        MSG_UPDATE_TASK = 1005, // 更新大厅任务

        MSG_MUTE_POPUP = 1006, //禁言弹窗
        MSG_ACCOUNT_SUSPENSION = 1007,//封号弹窗

        // MSG_ANTI_ADDICTION              = 1001, // 防沉迷
        // MSG_SUBSCRIBE                   = 1002, // 订阅
        // MSG_CHAT_MSG                    = 1003, // 大厅聊天
        // MSG_BATCH_USER_INFO_LITE        = 1004, // 批量获取用户信息
        // MSG_SYNC_USER_DATA              = 1005, // 同步玩家数据
        // MSG_SYNC_AVATAR_DATA            = 1006, // 同步试衣间列表数据
        // MSG_SYNC_VENDING_MACHINE_DATA   = 1007, // 同步售卖机列表数据
        // MSG_SYNC_LOTTERY_DATA           = 1008, // 同步抽奖列表数据(转盘、扭蛋机)
        // MSG_SYNC_STORE_DATA             = 1009, // 同步商城列表数据
        // MSG_SYNC_VENDING_SERIES_DATA    = 1011, // 售卖机series列表
        // MSG_SYNC_VENDING_PRODUCTS_DATA  = 1012, // 售卖机商品列表
        // MSG_SYNC_NEW_AVATAR_RESOURCE    = 1013, // 获得新的pgc avatar资源
        // MSG_SYNC_RED_DOT                = 1014, // 红点推送
        // MSG_SYNC_TASK_FINISH            = 1016, //任务完成
        // MSG_SYNC_TASK_SNAPSHOT          = 1017, //任务快照数据
        MSG_S_CMD_AI_GAME_AMUSEMENT_PARK_SYNC = 2001, //服务端同步CMD
    }
}