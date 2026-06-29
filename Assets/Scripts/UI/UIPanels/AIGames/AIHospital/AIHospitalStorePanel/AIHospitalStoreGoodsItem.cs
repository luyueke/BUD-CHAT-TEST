using Game.Store;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AIHospitalStoreGoodsItem : MonoBehaviour
{
    private AIHospitalStoreItemData itemData;
    [SerializeField] Image _discountImg;
    [SerializeField] Text _discountTxt;
    [SerializeField] Text _nameTxt;
    [SerializeField] Text _goodsCountTxt;
    [SerializeField] Toggle _tipsTog;
    [SerializeField] CButton _exchangeBtn;
    [SerializeField] CommonSpriteSwitch _exchangeCostCurrency;
    [SerializeField] Text _exchangeCost;
    [SerializeField] Image _sallOutImg;
    [SerializeField] CommonSpriteSwitch _iconImg;
    [SerializeField] CButton _iconClickBtn;

    private bool _sendMsg;

    public void Init(AIHospitalStoreItemData data)
    {
        //todo 按照传入的数据进行初始化
        //0 用ID来选择_iconImg
        //1 用discount初始化折扣文本，如果没有就隐藏折扣img
        //2 初始化剩余数量 _goodsCountTxt，如果剩余数量不足就让兑换按钮置灰且修改文本已兑完，否则用兑换消耗货币数量来初始化
        //3 根据货币类型来初始化
        //3 初始化tipsTog状态
        //4 初始化兑换按钮
        itemData = data;
        _iconImg.Switch((uint)data.ID-1);
        if (data.nRewardAmount != 0)
        {
            _nameTxt.text = data.strName + "x" + data.nRewardAmount;
        }
        else
        {
            _nameTxt.text = data.strName;
        }
        _discountTxt.text = data.strDiscount;
        _discountImg.gameObject.SetActive(!string.IsNullOrEmpty(data.strDiscount));
        _tipsTog.isOn = AIHospitalStoreManager.Inst.GetItemTipsState(data.ID);
        CheckRemainCount();
        AddListener();
        if (itemData.ID==1&&itemData.nRemainCostTimes>0)
        {
            OnClickIcon();
        }
    }

    public void AddListener()
    {
        _exchangeBtn.onClick.AddListener(OnExchangeBtnClick);
        _iconClickBtn.onClick.AddListener(OnClickIcon);
        _tipsTog.onValueChanged.AddListener(OnTogValueChanged);
    }

    private void OnExchangeBtnClick()
    {
        if (_sendMsg)
        {
            return;
        }

        if (itemData.nGoodsType==(int)EGoodsType.Clothes)
        {
            var clothes = AIHospitalStoreManager.Inst.GetClothesData(itemData.ID);
            if (clothes!=null&&clothes.Count>0)
            {
                if (AssetsDataManager.IsOwned(clothes[0]))
                {
                    TipPanel.ShowToast("您已拥有此物品");
                    return;
                }
            }
        }
        else if (itemData.nGoodsType == (int)EGoodsType.HeadFram)
        {
            if (UserUIWidgetManager.Inst.CheckIsOwnedAvatarFramePgc(AIHospitalStoreManager.HeadFramePgcID))
            {
                TipPanel.ShowToast("您已拥有此物品");
                return;
            }
        }


            _sendMsg = true;

        LoggerUtils.Log($"尝试点击兑换商品 {itemData.strName} 商品类型 = {(EGoodsType)itemData.nGoodsType}");

        JObject req = new JObject()
        {
            ["activityId"] = ActivityId.AbandonedHospital.ToString(),
            ["rewardId"] = itemData.ID,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityRedeemReward, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            //todo 获得奖励不确定是否要展示
            //更新剩余数量
            AccountDataManager.Inst.BalanceInfo.Refresh();
            //TipPanel.ShowToast($"恭喜你兑换了 {itemData.strName}");
            _sendMsg = false;
            AIHospitalStoreManager.Inst.UpdateItemRemainCount(itemData.ID, --itemData.nRemainCostTimes);
            CheckRemainCount();
            var rsp = JsonConvert.DeserializeObject<ActivityRewardConvertResponse>(content);
            if (rsp != null && rsp.rewardList != null)
            {
                var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                List<CommonRewardItemData> rewardList = new();
                foreach (var reward in rsp.rewardList)
                {
                    CommonRewardItemData commonRewardItem = new CommonRewardItemData()
                    {
                        rewardType = reward.rewardType,
                        RewardAmount = reward.amount
                    };
                    commonRewardItem.rewardName = itemData.strName;
                    if (reward.rewardType == (int)BUDRewardType.RewardAvatarFrame)
                    {
                        commonRewardItem.pgcId = AIHospitalStoreManager.HeadFramePgcID;
                        AccountDataManager.Inst.RefreshUserInfo((resData) =>
                        {
                        });
                    }
                    else if (itemData.nGoodsType == (int)EGoodsType.GiftBox)
                    {
                        commonRewardItem.rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType);
                    }
                    commonRewardItem.bundleId = reward.bundleId;
                    rewardList.Add(commonRewardItem);
                }
                rewardPanel.ShowRewards(rewardList);
            }
        },
        (error) =>
        {
            LoggerUtils.LogError(error);
            _sendMsg = false;
        });
    }

    private void OnClickIcon()
    {
        LoggerUtils.Log($"点击商品 {itemData.strName} 商品类型 = {(EGoodsType)itemData.nGoodsType}");
        EGoodsType eGoodsType = (EGoodsType)itemData.nGoodsType;

        if (eGoodsType== EGoodsType.Currency)
        {
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel,(CurrencyType)itemData.nCurrencyType);
        }   
        else if (eGoodsType == EGoodsType.Clothes)
        {

        }   
        else if (eGoodsType == EGoodsType.GiftBox)
        {
            var sprite = this._iconImg.GetComponent<Image>().sprite;
            var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
            panel.UpdateUI(sprite, "每日福袋", null, "打开后有机会获得以下奖励之一：1个幸运币；1个优优币；10个徽章；100个金币");
        }
        else if (eGoodsType == EGoodsType.HeadFram)
        {
            var sprite = this._iconImg.GetComponent<Image>().sprite;
            var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
            panel.UpdateUI(sprite, "海盗绯焰头像框", null, "通过逃离废弃医院兑换商店兑换获得");
        }
        MessageHelper.Broadcast(MessageName.OnS9StoreItemClick,itemData);
    }

    private void OnTogValueChanged(bool isOn)
    {
        AIHospitalStoreManager.Inst.SetItemTipsState(itemData.ID,isOn);
    }

    private void CheckRemainCount()
    {
        bool clickble = itemData.nRemainCostTimes > 0;
        _exchangeBtn.SetClickAble(clickble);
        _sallOutImg.gameObject.SetActive(!clickble);
        if (clickble)
        {
            _exchangeCost.text = (itemData.nCostAmount).ToString();
            //todo 这里需要根据货币类型来选择
            //_exchangeCostCurrency.gameObject.SetActive(itemData.nCurrencyType != (int)CurrencyType.Free);
            _exchangeCostCurrency.Switch((uint)(itemData.nCostCurrencyType == 24 ? 0: 1));
        }
        else
        {
            _exchangeCost.text = "已兑完";
            //_tipsTog.SetIsOnWithoutNotify(false);
        }
        _goodsCountTxt.text = string.Format($"{itemData.strUpdateCD}:{itemData.nMaxCostTimes - itemData.nRemainCostTimes}/{itemData.nMaxCostTimes}");
        CheckCurrencyAmountEnough();
    }


    private void CheckCurrencyAmountEnough()
    {
        //todo 发起兑换请求 检查用户自身的资源数量是否足够
        var currencyAmount = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)itemData.nCostCurrencyType);
        _exchangeBtn.interactable = currencyAmount >= itemData.nCostAmount;
        //if (currencyAmount < itemData.nCostAmount)
        //{
        //}
    }

    private void OnDestroy()
    {
    }


}
