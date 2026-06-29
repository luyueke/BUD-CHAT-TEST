namespace GameData
{
    public enum TheatreEnterType
    {
        None = 0,
        Scene = 1,
        Editor = 2,
        Store = 3,
        Preview = 4,
        Share = 5,  // 分享游玩，跳过拥有权判断，试玩限制不适用
        Room = 6,   // 多人房间模式，跳过本地进度/Lineup读写
    }
}