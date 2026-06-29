using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class SelectReward : MonoBehaviour
{
    [SerializeField] private CButton claimBtn;
    [SerializeField] private List<GameObject> reward;

    private HashSet<string> has = new();
    private string selectedId;
    private GashaponData mGashaponData;
    Action _claimAct;
    public void Init(GashaponData gashaponData, List<string> bundleIds,Action claimAct)
    {
        mGashaponData = gashaponData;
        var flag = false;
        _claimAct = claimAct;
        for (int i = 0, C = bundleIds.Count; i < C; i++)
        {
            var icon = GameObjectEx.FindComponentByName<Image>(reward[i], "Icon");
            icon.sprite = PgcUtils.LoadBundleIcon(bundleIds[i], gameObject);
            var isOwned = GameObjectEx.FindComponentByName<Text>(reward[i], "Text");
            var id = bundleIds[i];
            var toggle = reward[i].GetComponentInChildren<Toggle>();
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    selectedId = id;
                    claimBtn.interactable = !has.Contains(id);
                }
            });

            if (IsOwned(gashaponData, bundleIds[i]))
            {
                isOwned.transform.parent.gameObject.SetActive(true);
                isOwned.text = "已拥有";
                has.Add(bundleIds[i]);
            }
            else
            {
                isOwned.transform.parent.gameObject.SetActive(false);
                isOwned.text = "";

                if (!flag)
                {
                    flag = true;
                    toggle.SetIsOnWithoutNotify(true);
                    selectedId = id;
                    claimBtn.interactable = !has.Contains(id);
                }
            }
        }

        claimBtn.onClick.AddListener(OnClaimClick);
    }
    private void OnClaimClick()
    {   
        JObject req = new JObject()
        {
            ["lotteryId"] = mGashaponData.Id,
            ["buttonType"] = (int)GashaponSpecialButton.PgcOptionalBox,
            ["bundleId"] = Convert.ToInt32(selectedId)
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GashaponSpecialButton, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
        {
            gameObject.SetActive(false);
            OnGiftBoxOpen(JsonConvert.DeserializeObject<LuckyStarGiftBoxRsp>(response));
            _claimAct.Invoke();
        }, (fail) =>
        {
            gameObject.SetActive(false);
            LoggerUtils.LogError("pgc自选礼盒失败");
        });
    }

    private void OnGiftBoxOpen(LuckyStarGiftBoxRsp rsp)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var rewardData in rsp.rewardList)
        {
            var itemData = new CommonRewardItemData()
            {
                rewardName = rewardData.rewardName,
                RewardAmount = rewardData.amount,
                rewardType = rewardData.rewardType,
                pgcId = rewardData.pgcId,
                bundleId = rewardData.bundleId.ToString(),
                isConverted = rewardData.isReplaced,
                isCrit = rewardData.isCritical,
            };
            rewardItemDatas.Add(itemData);
        }
        panel.ShowRewards(rewardItemDatas, true);
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    public bool IsOwned(GashaponData gashaponData, string bundleId)
    {

        for (int i = 0; i < gashaponData.RewardList.Count; i++)
        {
            if (gashaponData.RewardList[i].BundleId == bundleId)
            {
                var reward = gashaponData.RewardList[i];

                if (reward.PgcDatas == null || reward.PgcDatas.Count == 0)
                {
                    return false;
                }

                foreach (var pgcData in reward.PgcDatas)
                {
                    if (pgcData?.InventoryData == null || pgcData.InventoryData.OwnedNum <= 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        return false;
    }
}
