using Es;
using Game.Event;
using GameData.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryStoreExchangeItem : MonoBehaviour
{
    // UI组件
    public Text buyNum;
    public Text num;
    public Button buyBtn;
    public Image icon;
    public Button previewBtn;
    public GameObject overObj;
    public GameObject lockObj;
    string _taskId;
    public RawImage bgRawTex;
    int _eventId;
    RewardItem _localData;
    public Action<RewardItem> _claimAction;
    string spriteAtlxs = "Assets/Loadable/UI/UIPanel/AnniversaryStoreView/icons.spriteatlas";
    void SetStatus(int statu)
    {   

        if(statu!= (int)EventStatus.Finish)
        {   
            if(statu == (int)EventStatus.Default)
            {
                if (_localData.progress > TokenDataManager.Inst.Data.GetToken(CurrencyType.CelebrationCoin))
                {
                    buyBtn.gameObject.SetActive(true);
                    buyBtn.interactable = false;
                    lockObj.gameObject.SetActive(false);
                    overObj.gameObject.SetActive(false);
                }
                else
                {
                    buyBtn.gameObject.SetActive(true);
                    buyBtn.interactable = true;
                    overObj.gameObject.SetActive(false);
                    lockObj.gameObject.SetActive(false);
                }
            }
            else
            {
                if(statu == (int)EventStatus.UnClaim){
                    buyBtn.gameObject.SetActive(false);
                    buyBtn.interactable = false;
                    overObj.gameObject.SetActive(true);
                    lockObj.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            buyBtn.gameObject.SetActive(false);
            buyBtn.interactable = false;
            overObj.gameObject.SetActive(true);
            lockObj.gameObject.SetActive(false);
        }
        
        
    }

    public void SetData(RewardItem localData ,ActivityRewardInfo serverData, Action<RewardItem> claimAction,bool isLock)
    {
        _localData = localData;
        _claimAction = claimAction;
        buyNum.text = localData.progress.ToString();
        num.text = "x"  + localData.rewardNum1.ToString();
        icon.sprite = LoadIconSprite(localData.rewardType1);
        previewBtn.onClick.AddListener(() => {
            if (localData.rewardType1 < 1000)
            {
                UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, (CurrencyType)localData.rewardType1);
            }
            else
            {
                if(localData.rewardType1 == 9999)
                {
                    var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    bundleShowpanel.SetEventPreview(new List<string>() {
                    "10800118",
                    "11700034",
                    "11200043",
                    "10600125",
                    "11000258",
                    "10100112",
                    "11000258",
                    "10900487",
                    "11300352",
                    "10400487"}, "猩红挽歌提莉亚套装", "Bundle_104", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
                    bgRawTex.texture);
                }
                else
                {
                    var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    bundleShowpanel.SetEventPreview(new List<string>() {
                    "10500078",}, "提莉亚牵线特效", "12", spriteAtlxs, "新年组队消费 领新年好礼", "#BF8DFF",
                    bgRawTex.texture);
                }
            }
            
        });
        if (isLock)
        {
            buyBtn.gameObject.SetActive(true);
            buyBtn.interactable = true;
            overObj.gameObject.SetActive(false);
            lockObj.gameObject.SetActive(true);
        }
        else
        {
            SetStatus(serverData.rewardStatus);
        }
        icon.SetNativeSize();
        
        buyBtn.onClick.AddListener(OnExchangeBtnClick);
    }
    Sprite LoadIconSprite(int type) { 
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteAtlxs, _localData.rewardIcon1,icon.gameObject);
    }
    private void OnExchangeBtnClick()
    {

        JObject req = new JObject()
        {
            ["activityId"] = ActivityId.S11CelebrationStore.ToString(),
            ["rewardId"] = int.Parse(_localData.rewardId),
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityRedeemReward, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
                _claimAction?.Invoke(_localData);
                //todo 获得奖励不确定是否要展示
                //更新剩余数量
                AccountDataManager.Inst.BalanceInfo.Refresh();
                TokenDataManager.Inst.GetTokenData();
                var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                List<CommonRewardItemData> rewardList = new();
                CommonRewardItemData commonRewardItem = new CommonRewardItemData()
                {
                    IconSp = LoadIconSprite(_localData.rewardType1),
                    RewardAmount = _localData.rewardNum1,
                    rewardName = _localData.rewardName1
                };
                rewardList.Add(commonRewardItem);
                rewardPanel.ShowRewards(rewardList);
                SetStatus((int)EventStatus.Finish);
        },
        (error) =>
        {
            LoggerUtils.LogError(error);
        });
    }
}
