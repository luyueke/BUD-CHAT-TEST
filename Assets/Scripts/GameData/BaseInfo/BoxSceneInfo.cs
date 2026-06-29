using GameData.Base;
using Newtonsoft.Json;

namespace GameData.BaseInfo
{
    /// <summary>
    /// 特殊列表项类型，仅在本地构造特殊 Item 时使用，不参与 JSON 序列化。
    /// </summary>
    public enum BoxSceneSpecialType
    {
        /// <summary>普通服务器场景</summary>
        None = 0,

        /// <summary>去购买入口（点击后跳转 AIPartnerShopPanel 伙伴盒子页签）</summary>
        GoPurchase = 1,

        /// <summary>默认场景（点击后向外传递 id="" 表示清除设备场景）</summary>
        DefaultScene = 2,
    }

    /// <summary>
    /// 盒子场景 UV 编辑会话元数据，作为 UGCBoxSceneEditorPanel.OnShow 的 args[1]。
    /// 对应材质编辑器中 MaterialInfo 的角色，继承 UgcBaseInfo 获取通用 id/name/cover 等字段。
    /// </summary>
    public class BoxSceneInfo : UgcBaseInfo
    {
        /// <summary>
        /// 盒子场景模板 ID，对应 UgcPartData.xlsx 和 UGCClothEditorConfig.xlsx 中的 uId 键。
        /// 固定值，不从服务器动态获取（盒子场景不进背包）。
        /// </summary>
        public override string templateId { get; set; } = "ugcBreedingFarm_1";

        /// <summary>画板格子数：0=32×32，1=64×64</summary>
        public int canvasType;

        /// <summary>风格：0=正常，1=二次元</summary>
        public int ugcStyle;

        /// <summary>是否已删除：0=否，1=是</summary>
        public int isDelete;

        /// <summary>付费信息</summary>
        public PaymentInfo paymentInfo;

        /// <summary>UGC 状态：1=草稿，2=已发布，3=已下架，4=已购买</summary>
        public int ugcclass;

        /// <summary>
        /// 特殊 Item 类型标识，仅本地列表构造时赋值，[JsonIgnore] 保证不影响服务器数据反序列化。
        /// </summary>
        [JsonIgnore]
        public BoxSceneSpecialType specialType = BoxSceneSpecialType.None;
    }
}
