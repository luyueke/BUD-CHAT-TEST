

using System.Collections.Generic;
using Game.Database;
using Game.Store;
using GameData.PgcData;

public class AIPartnerGoodsData
{
    // 商品唯一Id
    public string Id;

    // 商品购买Id
    public int ProductId;

    // 商品名称
    public string Name;

    //商品类型(社区，官方)
    public int GoodsType;

    //折扣
    public int Discount;

    // 是否拥有
    public bool IsOwned;

    // 购买价格 = 扣钱价格
    public CurrencyData Price;

    // 原始价格
    public CurrencyData OriginalPrice;

    public List<AIPartnerAssetsData> AssetsData;

}

public class AIPartnerAssetsData
{
    // 唯一标识
    public string Id;

    // 单品名称
    public string Name;

    // 资源类型
    public ResourceType ResourceType;

    // 背包数据 外部只读
    public InventoryData InventoryData { internal set; get; }

    // 价值 用于抵扣捆绑包
    public CurrencyData Value;

    public CabinCharacterUgcInfo UgcInfo;
}