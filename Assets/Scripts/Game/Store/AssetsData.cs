using Game.Database;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using System.Linq;
using Product;
using UIAgent;
using UnityEngine;
using GameData;

namespace Game.Store
{
    public enum GoodsType
    {
        ErrGoodsType,
        SinglePgc,
        SingleUgc,
        BundleUgc,
        ToolProduct,
        BundlePgc,
    }

    public enum ButtonType
    {
        Assets,
        Design,
        TakeOff,
        EmoteIdle, // 大厅设置动画的默认待机
        UgcEmoteIdle, // 大厅设置动画的Ugc默认待机
        NoMoreTips
    }

    // 商品数据
    public class GoodsData
    {
        // 按钮类型
        public ButtonType ButtonType;

        // 商品唯一Id
        public string Id;

        // 商品购买Id
        public int ProductId;

        // 商品名称
        public string Name;

        //UGC类型
        public int subType;

        // 商品类型
        public GoodsType GoodsType;

        // 商品包含资源列表
        public List<AssetsData> Assets;

        // 则扣
        public int Discount;

        // 来源
        public SourceData SourceData;

        // ugc捆绑包数据
        public RecommendItemData UgcBundleInfo;

        // 背包场景
        public bool IsBagScene;

        // 赠礼场景
        public bool IsGiftScene = false;

        // 是否拥有
        public bool IsOwned;

        // 当前选择
        public bool Selected;

        // 是否已添加到角色的动作列表（用于孵化舱选动作弹窗中标记"已添加"状态，运行时 UI 专用）
        public bool IsAdded;

        // 购买价格 = 扣钱价格
        public CurrencyData Price;

        // 原始价格
        public CurrencyData OriginalPrice;

        // 请求中
        public bool IsPayingRequest;

        // 不可穿上
        public bool CantWear;

        // 排序
        public int SortIndex;

        // 加载动画控制
        public bool IsLoading;

        // 红点
        public bool IsNew;

        // 提示语
        public string UseTips;

        // 跳转语
        public string AddTips;

        // 结束时间(仅仅PGC拥有)
        public long EndTime;

        public GiftType GiftType;

        public Action<bool, GoodsData> LoadingAction { set; private get; }

        public void Loading(bool loading)
        {
            IsLoading = loading;
            LoadingAction?.Invoke(loading, this);
        }

        public void Update()
        {
            UpdateOwned();
            UpdatePrice();
            UpdateNew();
        }

        private void UpdateOwned()
        {
            IsOwned = true;
            if (Assets != null)
                Assets.ForEach(a => IsOwned = IsOwned && a.InventoryData != null && a.InventoryData.OwnedNum > 0);
        }

        private void UpdatePrice()
        {
            if (IsBagScene) return;
            OriginalPrice = new CurrencyData();
            Price = new CurrencyData();

            Assets.ForEach(a =>
            {
                OriginalPrice.CurrencyType = a.Value.CurrencyType;
                OriginalPrice.Value += a.Value.Value;

                if (IsGiftScene && a.Value.CurrencyType == CurrencyType.PinkCoin)
                {
                    Price.CurrencyType = CurrencyType.Gem;
                }
                else
                {
                    Price.CurrencyType = a.Value.CurrencyType;
                }

                if (a.InventoryData == null || a.InventoryData.OwnedNum == 0 || IsGiftScene)
                {
                    Price.Value += a.Value.Value;
                }

            });


            if (GoodsType == GoodsType.BundleUgc && UgcBundleInfo?.skinInfo?.paymentInfo?.price != null)
            {
                Price.Value = UgcBundleInfo.skinInfo.paymentInfo.price - OriginalPrice.Value + Price.Value;
                OriginalPrice.Value = UgcBundleInfo.skinInfo.paymentInfo.price;
            }

            if (Price.CurrencyType == CurrencyType.PinkCoin)
            {
                var priceVa = AssetsDataManager.GetDiscountPinkCoin(this);
                priceVa = Mathf.Round(priceVa * 10f) / 10f; //只保留1位小数
                // 当小数部分是.0 忽略
                if (Mathf.Approximately(priceVa, Mathf.Floor(priceVa)))
                {
                    priceVa = Mathf.Floor(priceVa);
                }
                Price.Value = priceVa;
            }
            else
            {
                Price.Value = Mathf.CeilToInt(Price.Value * (100f - Discount) / 100f);
            }
        }

        private void UpdateNew()
        {
            IsNew = false;
            if (!IsBagScene) return;
            Assets.ForEach(a =>
            {
                if (a.InventoryData != null && a.InventoryData.IsNew)
                {
                    IsNew = true;
                    return;
                }
            });
        }

        public T GetFirstAsset<T>() where T : AssetsData
        {
            return (T)Assets?.FirstOrDefault();
        }

        public string GetPgcId(int index = 0)
        {
            if (GoodsType == GoodsType.SingleUgc)
            {
                return GetFirstAsset<AssetsData>()?.UgcInfo?.UgcInfo?.templateId;
            }
            else if (GoodsType == GoodsType.BundlePgc && Assets.Count > index)
            {
                return Assets[index]?.Id;
            }

            return Id;
        }
    }

    public class CurrencyData
    {
        public CurrencyType CurrencyType;
        public float Value;
    }

    public enum Source
    {
        // 商城直氪
        Mall,

        // UGC商品
        Ugc,

        // 扭蛋
        Gashapon,

        // 跳转？
        Jump,

        // 赛季通行证
        SeasonPass,

        // VIP
        VIP,

        // 活动
        Activity,

        //礼包
        Gift,

        // 新手任务
        BeginnerTask,

        // 创作者奖励
        CreatorReward,

        // 跳转任务
        Task,

        //热销
        HotSales
    }


    public class SourceData
    {
        // 来源
        public Source Source;

        // 来源标识
        public string Id;
    }

    public class AssetsData
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

        public RecommendItemData UgcInfo;
    }

    public class AvatarAssetsData : AssetsData
    {
        // 衣服子分类
        public AvatarSubType AvatarSubType;
    }

    public class EmoteAssetsData : AssetsData
    {
        public EmoteSubType EmoteSubType;
    }

    public class VehicleAssetsData : AssetsData
    {
        public VehicleSubType VehicleSubType;
    }

    public class MusicScoreAssetsData : AssetsData
    {
        public MusicScoreSubType MusicScoreSubType;
    }

    public class UgcPoseAssetsData : AssetsData
    {
        public UgcPoseSubType UgcPoseSubType;
    }

    public class UgcAnimAssetsData : AssetsData
    {
        public UgcAnimSubType UgcAnimSubType;
    }

    public class UgcVehicleAssetsData : UGCAssetsData
    {
        public VehicleSubType VehicleSubType;
    }

    public class PGCAssetsData : AvatarAssetsData
    {
    }

    public class UGCAssetsData : AvatarAssetsData
    {
    }

    public class UgcActorAssetsData : AssetsData
    {
    }

    public class UgcTheatreAssetsData : AssetsData
    {
        
    }

    public class GoodsDataClassifyList
    {
        private List<GoodsData> list;
        private List<GoodsData> classifyList = new();

        public Func<GoodsData, bool> predicate;

        public GoodsData Get(int index)
        {
            if (index < 0 || index >= classifyList.Count)
            {
                return null;
            }
            return classifyList[index];
        }

        public GoodsData Get(string id)
        {
            return classifyList.FirstOrDefault(tmp => tmp.Id == id);
        }


        public void SetData(List<GoodsData> _list, Func<GoodsData, bool> _predicate = null)
        {
            list = _list;
            predicate = _predicate;
            Refresh();
        }

        public void Refresh()
        {
            classifyList.Clear();
            if (predicate == null)
            {
                classifyList.AddRange(list);
            }
            else
            {
                classifyList.AddRange(list.Where(l => predicate(l)));
            }
        }

        public int Count()
        {
            return classifyList.Count;
        }
    }
}