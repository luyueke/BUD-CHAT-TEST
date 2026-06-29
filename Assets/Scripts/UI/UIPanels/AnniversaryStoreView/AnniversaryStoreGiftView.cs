using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Manager;
using Message;
using Newtonsoft.Json.Linq;
using System;
using GameData.Manager;
using GameUI;

public class AnniversaryStoreGiftView : MonoBehaviour, IActivity
{
    public List<BaseLimitPackageData> package;
    public AnniversaryStoreGiftItem itemPrefab;
    public List<AnniversaryStoreGiftItem> items;
    public Transform contont;
    public Button freeGiftBtn; // 这是你的“领取免费礼包”按钮
    public Action<int> _redDot;
    private BaseLimitPackageData freeGiftData; // 用于存储免费礼包的数据

    public void Init(Action<int> redDot)
    {
        _redDot = redDot;
        // 绑定按钮点击事件
        if (freeGiftBtn != null)
        {
            freeGiftBtn.onClick.AddListener(OnFreeGiftBtnClick);
        }
        // 获取数据并生成UI
        GetLimitedData();
    }
    
    public void GetLimitedData()
    {
        package = AnniversaryStoreMgr.Inst.GetStoreGiftData();
        GenderItem();
    }
    void GenderItem()
    {
        freeGiftData = null; // 重置免费礼包数据
        int i = 0;
        // 遍历数据，区分付费礼包和免费礼包
        if(package!=null)
        foreach (var data in package)
        {
            // 如果价格不为0，则是普通付费礼包，正常生成Item
            if (data.price != 0)
            {
                items[i].Init(data);
            }
            // 如果价格为0，则认为是免费礼包
            else
            {
                freeGiftData = data;
            }
            i++;
        }

        // 根据是否有免费礼包以及是否已领取，来更新免费按钮的状态
        UpdateFreeGiftButtonState();
    }

    /// <summary>
    /// 免费礼包按钮的点击事件
    /// </summary>
    void OnFreeGiftBtnClick()
    {
        // 如果没有免费礼包数据，或者已经领取了，则不响应
        if (freeGiftData == null || freeGiftData.isPurchase == 1)
        {
            Debug.Log("没有可领取的免费礼包或已经领取。");
            return;
        }

        Debug.Log("开始领取免费礼包，ID: " + freeGiftData.productId);

        // 构造请求，和 orderType==4 的逻辑一样
        JObject req = new JObject()
        {
            ["productType"] = 7, // 7 代表礼包类型
            ["productId"] = freeGiftData.productId,
        };

        // 发送网络请求
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
        {

            // 刷新玩家货币信息
            AccountDataManager.Inst.BalanceInfo.Refresh();
            TokenDataManager.Inst.GetTokenData();
            // 标记为已购买
            freeGiftData.isPurchase = 1;

            // 更新按钮状态
            UpdateFreeGiftButtonState();

            // 弹出奖励面板
            BuyProductResult rsp = JsonConvert.DeserializeObject<BuyProductResult>(response);
            ShowFreePackReward(rsp);

            // 通知红点系统刷新
            ReddotManagerUtils.Inst.RefreshRedDot();

        }, (error) =>
        {
        });
    }

    /// <summary>
    /// 更新免费礼包按钮的UI状态（比如显示“可领取”或“已领取”）
    /// </summary>
    private void UpdateFreeGiftButtonState()
    {
        if (freeGiftBtn == null) return;

        // 如果存在免费礼包
        if (freeGiftData != null)
        {
            freeGiftBtn.gameObject.SetActive(true);
            var btnText = freeGiftBtn.GetComponentInChildren<Text>();

            // 如果已经领取了
            if (freeGiftData.isPurchase == 1)
            {
                _redDot?.Invoke(0);
                MessageHelper.Broadcast(MessageName.ReddotNotice);
                freeGiftBtn.interactable = false; // 按钮不可点击
                freeGiftBtn.transform.Find("enabletext").gameObject.SetActive(false);
                freeGiftBtn.transform.Find("disabletext").gameObject.SetActive(true);
            }
            // 如果还未领取
            else
            {
                freeGiftBtn.interactable = true; // 按钮可点击
                freeGiftBtn.transform.Find("enabletext").gameObject.SetActive(true);
                freeGiftBtn.transform.Find("disabletext").gameObject.SetActive(false);
            }
        }
        // 如果不存在免费礼包
        else
        {
            freeGiftBtn.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示免费礼包的奖励
    /// </summary>
    private void ShowFreePackReward(BuyProductResult result)
    {
        var rewardItemDatas = new List<CommonRewardItemData>();

        // 优先使用服务器返回的奖励列表，如果为空，则使用本地的
        var rewardsToShow = (result != null && result.rewardList != null && result.rewardList.Count > 0)
            ? result.rewardList
            : freeGiftData.rewardList;

        foreach (var rewardData in rewardsToShow)
        {
            var itemData = new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject),
                RewardAmount = rewardData.amount,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType)
            };
            rewardItemDatas.Add(itemData);
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {
        throw new NotImplementedException();
    }

    public bool IsOpen()
    {
        throw new NotImplementedException();
    }

    public void ShowPanel()
    {
        throw new NotImplementedException();
    }

    public List<ReddotType> GetReddotTypes()
    {
        throw new NotImplementedException();
    }
}