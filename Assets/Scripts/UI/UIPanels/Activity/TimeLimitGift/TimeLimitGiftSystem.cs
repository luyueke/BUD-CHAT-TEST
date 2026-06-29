using Basic.Utils;
using Game.Database;
using Game.Store;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public enum TimeLimitGiftType 
    {
        None = 0,
        Coin = 1, // 社区币礼包
        Action = 2,       // 动作礼包
        Youyou = 3,        // 悠悠币
        Zimeng = 4,        // 紫梦币
    }

    public class TimeLimitGiftSystem : GlobalInstance<TimeLimitGiftSystem>, IActivity
    {
        public List<BaseLimitPackageData> LimitPackageCoin = new List<BaseLimitPackageData>();

        public List<BaseLimitPackageData> LimitPackageAction = new List<BaseLimitPackageData>();

        public List<BaseLimitPackageData> LimitPackageYouyou = new List<BaseLimitPackageData>();

        public List<BaseLimitPackageData> LimitPackageZimeng = new List<BaseLimitPackageData>();

        public TimeLimitGiftBtn limitGiftBtn;

        public override void Initialize()
        {
            base.Initialize();

            AssetsDataManager.BuyAction += SetTriggerTime;

#if UNITY_ANDROID
            LimitPackageCoin.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang6", // android_
                name = "超值社区币礼包6元",
                price = 6,
            });

            LimitPackageCoin.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang12",
                name = "超值社区币礼包12元",
                price = 12,
            });


            LimitPackageAction.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang30",
                name = "社区动作礼包30元",
                price = 30,
            });

            LimitPackageAction.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang68",
                name = "社区动作礼包68元",
                price = 68,
            });

            LimitPackageYouyou.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangyouyou6",
                name = "特惠优优币礼包6元",
                price = 6,
            });

            LimitPackageYouyou.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangyouyou12",
                name = "特惠优优币礼包12元",
                price = 12,
            });

            LimitPackageZimeng.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangzimeng6",
                name = "特惠紫梦币礼包6元",
                price = 6,
            });

            LimitPackageZimeng.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangzimeng12",
                name = "特惠紫梦币礼包12元",
                price = 12,
            });
#else
            LimitPackageCoin.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang6",
                name = "超值社区币礼包6元",
                price = 6,
            });

            LimitPackageCoin.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang12",
                name = "超值社区币礼包12元",
                price = 12,
            });

            LimitPackageAction.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang30",
                name = "社区动作礼包30元",
                price = 30,
            });

            LimitPackageAction.Add(new BaseLimitPackageData()
            {
                productId = "budtanchuang68",
                name = "社区动作礼包68元",
                price = 68,
            });

            LimitPackageYouyou.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangyouyou6",
                name = "特惠优优币礼包6元",
                price = 6,
            });

            LimitPackageYouyou.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangyouyou12",
                name = "特惠优优币礼包12元",
                price = 12,
            });

            LimitPackageZimeng.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangzimeng6",
                name = "特惠紫梦币礼包6元",
                price = 6,
            });

            LimitPackageZimeng.Add(new BaseLimitPackageData()
            {
                productId = "tanchuangzimeng12",
                name = "特惠紫梦币礼包12元",
                price = 12,
            });
#endif
        }

        public void OpenPanel() 
        {
            UIManager.Inst.OpenPanel(PanelId.TimeLimitGiftPanel);
        }

        public void SetTriggerTime(int type) {
            //if (GetCooling())
            //{
            //    return;
            //}
            //if (GetTriggerTime() > 0)
            //{
            //    return;
            //}
            if (type > 0)
            {
                if (type == (int)TimeLimitGiftType.Coin) {
                    AnalyticsManager.Inst.Track("Budtanchuan6_Show");
                    //LoadEvent.ReportPopupStatus("Budtanchuan6_Show");
                }
                if (type == (int)TimeLimitGiftType.Action)
                {
                    AnalyticsManager.Inst.Track("Budtanchuan30_Show");

                    var pdcId = BagDatabase.Inst.Select("40100513");
                    if ((pdcId != null && pdcId.OwnedNum > 0))
                    {
                        TimeLimitGiftSystem.Inst.SetBuyKey(1); // 动作买过了
                    }
                    else
                    {
                        TimeLimitGiftSystem.Inst.SetBuyKey(0); // 动作没买过
                    }
                }
                if (type == (int)TimeLimitGiftType.Youyou)
                {
                    AnalyticsManager.Inst.Track("Budtanchuanyouyou6_Show");
                }
                if (type == (int)TimeLimitGiftType.Zimeng)
                {
                    AnalyticsManager.Inst.Track("Budtanchuanzimeng6_Show");
                }
                Debug.Log("TimeLimitGift 触发礼包" + (TimeLimitGiftType)type);
                var time = TcpTimeSystem.Inst.ServerTime;
                SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.TimeLimitGift + type, $"{type}_{time}");
                //SetCooling(time + 12 * 60 * 60); // 没有购买时 冷却时间是弹窗消失后的时间
                if (limitGiftBtn != null) limitGiftBtn.gameObject.SetActive(true);
                OpenPanel();
            }
        }

        public long GetTriggerTime()
        {
            var type = GetPackageType();
            long time = 0;
            if (type.Count > 0)
            {
                time = type[type.Count - 1].Item2;
            }

            return time;
        }

        public long GetTriggerTime(TimeLimitGiftType type) {
            var str = SaveGameUtil.Inst.GetStrByPlayerPrefs(SaveGameUtil.TimeLimitGift + (int)type);
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            var time = str.Split("_")[1];
            return long.Parse(time);
        }

        public Dictionary<TimeLimitGiftType,List<BaseLimitPackageData>> GetPackage()
        {
            var dic = new Dictionary<TimeLimitGiftType, List<BaseLimitPackageData>>();
            var type = GetPackageType();
            foreach (var item in type) {
                switch (item.Item1)
                {
                    case TimeLimitGiftType.Coin:
                        dic.Add(item.Item1, LimitPackageCoin);
                        break;
                    case TimeLimitGiftType.Action:
                        dic.Add(item.Item1, LimitPackageAction);
                        break;
                    case TimeLimitGiftType.Youyou:
                        dic.Add(item.Item1, LimitPackageYouyou);
                        break;
                    case TimeLimitGiftType.Zimeng:
                        dic.Add(item.Item1, LimitPackageZimeng);
                        break;
                }
            }
            return dic;
        }

        public List<(TimeLimitGiftType,long)> GetPackageType()
        {
            var ls = new List<(TimeLimitGiftType,long)>();
            var val = Enum.GetValues(typeof(TimeLimitGiftType));
            foreach (var item in val) {
                var str = SaveGameUtil.Inst.GetStrByPlayerPrefs(SaveGameUtil.TimeLimitGift + (int)item);
                if (!string.IsNullOrEmpty(str))
                {
                    var type = str.Split("_")[0];
                    var time = str.Split("_")[1];
                    if (long.Parse(time) + 12 * 60 * 60 <= TcpTimeSystem.Inst.ServerTime)
                    {
                        Debug.Log("TimeLimitGift 清理礼包" + item);
                        SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.TimeLimitGift + (int)item, "");
                    }
                    else
                    {
                        ls.Add(((TimeLimitGiftType)(int.Parse(type)), long.Parse(time)));
                    }
                }
            }
            ls.Sort((x,y) => { return y.Item2.CompareTo(x.Item2); });
            return ls;
        }

        //public bool GetCooling() {
        //    var str = SaveGameUtil.Inst.GetStrByPlayerPrefs(SaveGameUtil.TimeLimitGiftCooling);
        //    if (string.IsNullOrEmpty(str)) 
        //    {
        //        return false;
        //    }
        //    if (long.Parse(str) + 72 * 60 * 60 > TcpTimeSystem.Inst.ServerTime)
        //    {
        //        return true;
        //    }
        //    return false;
        //}
        //public void SetCooling(long time) 
        //{
        //    SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.TimeLimitGiftCooling, time.ToString());
        //}

        public bool GetBuyKey()
        {
            var str = SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.TimeLimitGiftBuy);

            return str == 1;
        }

        public void SetBuyKey(int str)
        {
            SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.TimeLimitGiftBuy, str);
        }

        public void RefreshBtn(ProductRes res)
        {
            if (IAPDataManager.Inst.productRes == null)
            {
                if (limitGiftBtn != null)
                {
                    limitGiftBtn.gameObject.SetActive(false);
                }
                return;
            }

            var ls = IAPDataManager.Inst.productRes.popupPackageList;

            var pack = GetPackage();

            var allBuy = true;
            if (pack != null)
            {
                foreach (var t in pack)
                {
                    bool bo = true;
                    foreach (var item in t.Value)
                    {
                        var page = ls.Find(x => { return x.productInfo.productId.EndsWith(item.productId); });
                        if (page != null && page.isPaid == 0)
                        {
                            bo = false;
                            break;
                        }
                    }
                    if (bo)
                    {
                        Debug.Log("TimeLimitGift 没有礼包" + t.Key);
                        SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.TimeLimitGift + (int)t.Key, "");
                    }
                    else
                    {
                        allBuy = false;
                    }
                }
            }

            if (allBuy)
            {
                Debug.Log("TimeLimitGift 没有礼包了");
                SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.TimeLimitGift, "");
                if (limitGiftBtn != null)
                {
                    limitGiftBtn.gameObject.SetActive(false);
                }
            }
        }

        public bool IsOpen()
        {
            return true;
        }

        public void ShowPanel()
        {
 
        }

        public List<ReddotType> GetReddotTypes()
        {
            return null;
        }

        public void LoginActivityInfo(ActivityInfo activityInfo)
        {

        }
    }
}