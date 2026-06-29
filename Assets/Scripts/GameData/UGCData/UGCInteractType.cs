// @Author: YangJie
// @Description:
// @Date:  2023/09/12
// @Modify:

namespace GameData.UGCData
{
    public enum UGCInteractType
    {
        
        None = 0,
        
        // 1 体验地图
        ExperienceMap = 1,

        // 2 点赞地图
        LikeMap = 2,

        // 3 购买皮肤
        BuySkin = 3,

        // 4 点赞皮肤
        LikeSkin = 4,

        // 5 收藏设子(此接口无效)
        CollectSetting = 5,

        // 6 购买素材
        BuyUGCItem = 6,

        // 7 点赞素材
        LikeUGCItem = 7,

        // 8 收藏皮肤
        CollectSkin = 8,

        // 9 购买材质
        BuyMaterial = 9,

        // 10 点赞材质
        LikeMaterial = 10,
        
        //11 收藏地图
        CollectMap = 11,
        
        //购买npc
        BuyNpc = 32,

        //伙伴音色
        CharacterTone=43,

        //box盒子
        BoxScene=54,
    }
}