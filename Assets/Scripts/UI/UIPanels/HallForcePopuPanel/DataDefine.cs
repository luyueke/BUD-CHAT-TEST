using System;
using System.Collections.Generic;
using UI.UIPanels.RechargePanel;

namespace View.UI.PopupPanelSystem.Data
{
    [Serializable]
    public class BasePopupData
    {
    }


    [Serializable]
    public class PopupRspData : BasePopupData
    {
        public List<WebtoolNewsData> popupList;
    }

    public class WebtoolNewsData
    {
        public int popupId;
        public int skipType;
        public string skipData;
        public string coverUrl;
        public string buttonColor;
        public int eventId;
        // 用户类型：0老用户，1新用户
        public int userType;

        // 标签列表
        public List<LabelData> labels;


        public WebtoolNewsData Clone()
        {
            var clone = new WebtoolNewsData()
            {
                popupId = popupId,
                skipType = skipType,
                skipData = skipData,
                coverUrl = coverUrl,
                buttonColor = buttonColor,
                userType = userType
            };

            // 深拷贝 labels
            if (labels != null)
            {
                clone.labels = new List<LabelData>();
                foreach (var label in labels)
                {
                    clone.labels.Add(label.Clone());
                }
            }
            return clone;
        }
    }
        [Serializable]
    public class LabelData
    {
        public int id;         // 标签ID
        public string name;    // 标签名称
        public int isSelected;

        // 默认构造函数，供序列化和反序列化使用
        public LabelData()
        {
        }

        // 带参数的构造函数
        public LabelData(int _id, string _name)
        {
            id = _id;
            name = _name;
        }

        // 克隆方法
        public LabelData Clone()
        {
            return new LabelData
            {
                id = this.id,
                name = this.name
            };
        }
    }
    public class AnniversarySkipData
    {
        public int eventId;
    }
    public class PaidPackSkipData
    {
        public string taskId;
        public void HandSkip() {
            
            switch (taskId)
            {
                case "4001":
                    var s4LimitPackPanel = UIManager.Inst.OpenPanel<S4LimitCurrencyPackPanel>(PanelId.S4LimitCurrencyPackPanel);
                    s4LimitPackPanel.SetData(null);
                    break;
                case "4002":
                    //var s4ShiYuanPackPanel = UIManager.Inst.OpenPanel<S4ShiYuanPackPanel>(PanelId.S4ShiYuanPackPanel);
                    //s4ShiYuanPackPanel.SetData(null);
                    break;
                case "4003":
                    var s4LiuYuanPackPanel = UIManager.Inst.OpenPanel<S4LiuYuanPackPanel>(PanelId.S4LiuYuanPackPanel);
                    s4LiuYuanPackPanel.SetData(null);
                    break;
            }
            //兼容老礼包，后续可以删除
            if (taskId == "4001")
            {
                var s4LimitPackPanel = UIManager.Inst.OpenPanel<S4LimitCurrencyPackPanel>(PanelId.S4LimitCurrencyPackPanel);
                s4LimitPackPanel.SetData(null);
            }
            else if(taskId == "4002")
            {
                //var s4ShiYuanPackPanel = UIManager.Inst.OpenPanel<S4ShiYuanPackPanel>(PanelId.S4ShiYuanPackPanel);
                //s4ShiYuanPackPanel.SetData(null);
            }
            else if (taskId == "4003")
            {
                var s4LiuYuanPackPanel = UIManager.Inst.OpenPanel<S4LiuYuanPackPanel>(PanelId.S4LiuYuanPackPanel);
                s4LiuYuanPackPanel.SetData(null);
            }
            else
            {
                if (Enum.TryParse<PaidPackageType>(taskId, out var result) && Enum.IsDefined(typeof(PaidPackageType), result))
                {
                    LoggerUtils.Log($"找到对应的礼包：{result} ,taskId:{taskId}");
                    HandlePaidPackage(result);
                }
                else
                {
                    LoggerUtils.LogError("找不到对应的礼包："+taskId);
                }
            }

          
        }
        
        RechargeId? GetRechargeId(PaidPackageType packageType)
        {
            return packageType switch
            {
                PaidPackageType.DreamyVioletPack => RechargeId.DreamyVioletPack,
                PaidPackageType.FurryPack => RechargeId.FurryPack,
                PaidPackageType.PeachContiPack => RechargeId.PeachContiPack,
                PaidPackageType.S7PhoenixPackage => RechargeId.S7PhoenixPackage,
                _ => null
            };
        }

        public void HandlePaidPackage(PaidPackageType result)
        {
            var rechargeId = GetRechargeId(result);
            if (rechargeId.HasValue)
            {
                UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, rechargeId.Value);
            }
        }

    }

    public class SeasonPassData
    {
        public string seasonPassType;
    }
    

    public class LimitPaidPackageSkipData
    {
        public int taskId;
    }
}
