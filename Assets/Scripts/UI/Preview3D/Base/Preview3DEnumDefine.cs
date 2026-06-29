using System;

namespace UI.Preview3D.Base
{
    public enum Preview3DType
    {
        Unknown = -1,
        Avatar = 1,
        Emote = 2,
        Gold = 3, //货币预览 //TODO: 后端暂时只有一种金币，待拓展
        ThemeSkin = 999, //主题皮肤
    }

    public enum Preview3DSource
    {
        Any = 0, //任意页面
        Store = 1, //商城
        TopPicks = 2, //top picks
        GashaponThemePage = 3, //扭蛋-主题皮肤页面
        GashaponRewardPreviewPage = 4, //扭蛋-全部奖励预览页面
    }
}