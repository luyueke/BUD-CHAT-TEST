using Com.TheFallenGames.OSA.Util.IO;
using Es;
using EventTracking;
using Game.Audio;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CommonRewardItemData
{
    public Sprite IconSp;
    public string UgcCover;  // ugc商品图片的url
    public string rewardName;
    public int RewardAmount;
    public int rewardType = 0; //物品类型
    public string pgcId;
    public string bundleId;
    // 是否显示暴击提示
    public bool isCrit;

    //是否是限时
    // 是否显示已转化提示
    public bool isLimitedtime = false;
    public bool isConverted;
    public string rewardSpecial;
}


[Serializable]
public class TaskRewardData
{
    public int rewardType;
    public int num;
}

public class CommonRewardPanel : BasePanel<CommonRewardPanel>
{
    public Transform bg;
    public Transform additionalBg;
    public Transform additionalBg_tableDevice;

    public Button closeBtn;
    public Button bgCloseBtn;
    public Button purchaseBtn;

    public Transform content;
    public GameObject rewardItem;
    public GameObject seasonPassItem;
    public Text hintTxt;
    public Text replaceTips;
    public GameObject seasonPassReward;
    public Transform seasonPassItemRoot;
    public ScrollRect seasonPassScrollRect;
    private Action _onCloseAct;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";

    public Image CustomIcon;
    public Text CustomNum;
    public RectTransform TitleRect;
    public Transform CommonLight;
    public Transform CustomLight;

    public Button GoOfficialBtn;
    public Button GoTrunBtn;

    public Button GoPosBtn;

    public Button CommonBtn;
    public Text CommonTxt;
    public Action CommonAc;
    public override void OnCreate()
    {
        base.OnCreate();
        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            ?.Instantiate(bg);
        if (itemObj)
        {
            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item?.InitCustomBgItem("#A982FF", atlasPath,
                new List<string>()
                {
                    "color_bg_icon1001", "color_bg_icon1002", "color_bg_icon1003", "color_bg_icon1004",
                    "color_bg_icon1005"
                });
            item?.gameObject.SetActive(true);
        }

        closeBtn.onClick.AddListener(OnCloseBtnClick);
        bgCloseBtn.onClick.AddListener(OnCloseBtnClick);
        purchaseBtn.onClick.AddListener(OnPurchaseBtnClick);

        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_GetRewards_A3");

        CustomIcon.gameObject.SetActive(false);

        GoOfficialBtn.onClick.AddListener(OnGoOfficialBtn);
        GoTrunBtn.onClick.AddListener(OnGoTrunBtn);
        GoPosBtn.onClick.AddListener(OnGoPosBtn);
        GoOfficialBtn.gameObject.SetActive(false);
        GoTrunBtn.gameObject.SetActive(false);
        GoPosBtn.gameObject.SetActive(false);

        CommonBtn.onClick.AddListener(() => { OnCloseBtnClick(); });
        CommonBtn.gameObject.SetActive(false);
    }

    public void ShowAdditionalBg(Sprite sprite){
        additionalBg.GetComponent<Image>().sprite = sprite;
        additionalBg.gameObject.SetActive(true);
    }

    public void ShowCommonBtn(string str,Action ac) {
        CommonAc = ac;
        CommonTxt.text = str;
        CommonBtn.gameObject.SetActive(true);
        GoOfficialBtn.gameObject.SetActive(false);
        GoTrunBtn.gameObject.SetActive(false);
        GoPosBtn.gameObject.SetActive(false);
    }

    private void OnGoOfficialBtn()
    {
        OnCloseBtnClick();
        var panel = UIManager.Inst.FindPanel<UI.UIPanels.FittingRoom.FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (panel == null)
        {
            panel = UIManager.Inst.OpenPanel<UI.UIPanels.FittingRoom.FittingRoomPanel>(PanelId.FittingRoomPanel);
        }
            
        panel.JumpTo(UI.UIPanels.FittingRoom.MainTabs.Tab.BUD , 2);

    }

    private void OnGoTrunBtn()
    {
        OnCloseBtnClick();
        UIManager.Inst.OpenPanel<StoreMallPanel>(PanelId.StoreMallPanel, "lottery.coin");
        LoadEvent.ReportPopupStatus("ClickedGashapon", "GameHall");
    }

    private void OnGoPosBtn()
    {
        OnCloseBtnClick();
        //var panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        //panel.JumpTo(MainTabs.Tab.Action);
        if (!HallCharacterManager.IsHidden)
        {
            var idleAnimPanel = UIManager.Inst.OpenPanel<UI.UIPanels.LobbyCharacterIdlePanel.LobbyNpcIdlePanel>(PanelId.LobbyNpcIdlePanel);
            idleAnimPanel.UpdateHallAnim = null;
        }
        else
        {
            var idleAnimPanel = UIManager.Inst.OpenPanel<UI.UIPanels.LobbyCharacterIdlePanel.LobbyCharacterIdlePanel>(PanelId.LobbyCharacterIdlePanel);
            idleAnimPanel.UpdateHallAnim = null;
        }
    }


    public void OnCloseBtnClick()
    {
        CommonAc?.Invoke();
        this._onCloseAct?.Invoke();
        CloseSelf();
        var panel = UIManager.Inst.FindPanel(PanelId.FittingRoomPanel);
    }

    private void OnPurchaseBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.SeasonPurchaseView);
        //if (panel != null)
        //{
        //    var curSelect = panel.GetCurSeasonPassType();
        //    panel.SwitchView(curSelect, "SeasonPurchaseView");
        //}

        OnCloseBtnClick();
    }

    /// <summary>
    /// 用于vip奖励
    /// </summary>
    /// <param name="rewards"></param>
    public void ShowRewards(List<CommonRewardItemData> rewards, bool IsResize = false, bool isShowName = false, string repleaceTips = "" , bool isShowSeason = false)
    {
        if (rewards == null || rewards.Count == 0) return;

        additionalBg_tableDevice.gameObject.SetActive(isShowSeason);

        // 显示新的奖励项
        for (int i = 0; i < rewards.Count; i++)
        {
            FillCommonRewardItem(rewards[i], IsResize);
        }

        if (rewards.Count == 1)
        {
            if (rewards[0].RewardAmount > 1)
            {
                string rewardName = LocalizationManager.Inst.GetLocalizedText(rewards[0].rewardName);
                hintTxt.SetLocalText("你获得了 <color=#FFD400>{0} {1}</color>", rewards[0].RewardAmount, rewardName);
            }
            else
            {
                string rewardName = LocalizationManager.Inst.GetLocalizedText(rewards[0].rewardName);
                hintTxt.SetLocalText("你获得了 <color=#FFD400>{0}</color>", rewardName);
            }
        }
        else
        {
            if (isShowName)
            {
                var tipFormat = "你获得了{0}!";
                var tipValue = "";
                foreach (var tmpReward in rewards)
                {
                    if (!string.IsNullOrEmpty(tipValue))
                    {
                        tipValue += LocalizationManager.Inst.GetLocalizedText("和");
                    }
                    tipValue += $"<color=#FFD400>{LocalizationManager.Inst.GetLocalizedText(tmpReward.rewardName)}</color>";
                }
                hintTxt.SetLocalText(tipFormat, tipValue);
            }
            else
            {
                hintTxt.SetLocalText("你获得了以下的商品！");
            }
        }

        replaceTips?.SetText(repleaceTips);
    }


    public void ShowPgcRewards(List<String> pgcIds, string singleResouceName)
    {
        if (pgcIds == null || pgcIds.Count == 0) return;

        List<CommonRewardItemData> rewards = new List<CommonRewardItemData>();
        for (int i = 0; i < pgcIds.Count; i++)
        {
            var data = new CommonRewardItemData();
            data.IconSp = PgcUtils.GetIconSpriteByPgcId(pgcIds[i], gameObject);
            if (pgcIds.Count == 1)
            {
                data.rewardName = singleResouceName;

                if (PgcUtils.GetTypeByPgcId(pgcIds[i]) == ResourceType.Pose || PgcUtils.GetTypeByPgcId(pgcIds[i]) == ResourceType.UgcPose)
                {
                    GoPosBtn.gameObject.SetActive(true);
                }
            }
            rewards.Add(data);
        }
        ShowRewards(rewards);
    }

    /// <summary>
    /// 用于task奖励
    /// </summary>
    /// <param name="rewards"></param>
    public void ShowRewards(List<TaskRewardData> rewards)
    {
        if (rewards == null || rewards.Count == 0) return;

        
        Dictionary<int, int> rewardsDict = new Dictionary<int, int>();

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewardsDict.ContainsKey(rewards[i].rewardType))
            {
                rewardsDict[rewards[i].rewardType] += rewards[i].num;
            }
            else
            {
                rewardsDict.Add(rewards[i].rewardType, rewards[i].num);
            }
        }

        foreach (var reward in rewardsDict)
        {
            FillTaskRewardItem(reward.Key, reward.Value);
        }

        if (rewardsDict.Count == 1)
        {
            TaskRewardData data = null;
            foreach (var kv in rewardsDict)
            {
                data = new TaskRewardData()
                {
                    rewardType = kv.Key,
                    num = kv.Value,
                };
            }

            FillSingleHint(data);
        }
        else
        {
            hintTxt.SetLocalText("你获得了以下的商品！");
        }

        rewardsDict.Clear();
    }

    /// <summary>
    /// 用于晴天娃娃奖励或者自定义描述奖励
    /// </summary>
    /// <param name="rewards"></param>
    public void ShowRewards(TaskRewardData reward,string hintStr)
    {
        if (reward == null) return;

        CustomIcon.gameObject.SetActive(true);

        CustomNum.text = reward.num.ToString();

        hintTxt.SetLocalText(hintStr);

        TitleRect.anchoredPosition = new Vector2(0, -300);

        CommonLight.gameObject.SetActive(false);
        CustomLight.gameObject.SetActive(true);
    }
    /// <summary>
    /// 自定义显示标题文本内容
    /// </summary>
    /// <param name="hintStr"></param>
    public void SetHintContent(string hintStr)
    {
        hintTxt.SetLocalText(hintStr);
    }

    /// <summary>
    /// 用于赛季通行证奖励
    /// </summary>
    /// <param name="rewards"></param>
    public void ShowSeasonPassRewards(List<CommonRewardItemData> rewards ,bool isSeason = false)
    {
        if (rewards == null || rewards.Count == 0) return;


        // 显示季票奖励UI
        additionalBg_tableDevice.gameObject.SetActive(isSeason);
        seasonPassReward.SetActive(true);
        //赛季奖励隐藏前往按钮
        GoOfficialBtn.gameObject.SetActive(false);
        GoTrunBtn.gameObject.SetActive(false);
        GoPosBtn.gameObject.SetActive(false);

        // 合并相同类型的奖励
        Dictionary<string, CommonRewardItemData> mergedRewards = new Dictionary<string, CommonRewardItemData>();
        
        foreach (var reward in rewards)
        {
            // 为PGC资源创建唯一键
            string key;

            if (reward.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(reward.pgcId))
            {
                key = $"pgc_{reward.pgcId}";

            }
            else
            {
                key = $"type_{reward.rewardType}";
            }
            
            // 合并相同类型的奖励
            if (mergedRewards.ContainsKey(key))
            {
                mergedRewards[key].RewardAmount += reward.RewardAmount;
            }
            else
            {
                // 创建新的奖励数据副本
                CommonRewardItemData newReward = new CommonRewardItemData
                {
                    IconSp = reward.IconSp,
                    rewardName = reward.rewardName,
                    RewardAmount = reward.RewardAmount,
                    rewardType = reward.rewardType,
                    pgcId = reward.pgcId,
                    bundleId = reward.bundleId,
                    isCrit = reward.isCrit,
                    isConverted = reward.isConverted,
                    rewardSpecial = reward.rewardSpecial
                };

                if(reward.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(reward.bundleId)) {
                    newReward.IconSp = PgcUtils.LoadBundleIcon(reward.bundleId, gameObject);
                }
                else if(reward.rewardType == (int)BUDRewardType.RewardSeasonPassRandomPack) {
                    newReward.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "icon_36", this.gameObject);
                }

                mergedRewards.Add(key, newReward);
            }
        }
        
        // 添加合并后的奖励项
        foreach (var reward in mergedRewards.Values)
        {
            FillSeasonPassRewardItem(reward);
        }

        if(seasonPassScrollRect != null){
            seasonPassScrollRect.normalizedPosition = new Vector2(0, 0);
        }
        
    }


    public void ShowReplaceTex(List<GameData.Rewards.RewardInfo> replaceRewardList)
    {
        if (replaceRewardList != null && replaceRewardList.Count > 0)
        {
            replaceTips.text = "重复获得的奖励已经转化为";
            for (int i = 0, C = replaceRewardList.Count; i < C; i++)
            {
                if (i > 0) replaceTips.text += "和";
                replaceTips.text += $"{replaceRewardList[i].amount}{PgcUtils.GetRewardName((BUDRewardType)replaceRewardList[i].rewardType)}";
            }
        }
        else
        {
            replaceTips.text = "";
        }
    }

    private void FillCommonRewardItem(CommonRewardItemData data, bool isResize = false)
    {   
        //实例化物体预制体
        var rewardGo = GameObject.Instantiate(rewardItem, content).transform;
        rewardGo.gameObject.SetActive(true);
        //找到图片transform
        var imageGo = rewardGo.transform.Find("TopUpContentImage");
        var rawImageGo = rewardGo.transform.Find("TopUpContentRawImage");
        var newImage = rewardGo.transform.Find("New");
        var timerImage = rewardGo.transform.Find("Timer");
        timerImage.gameObject.SetActive(data.isLimitedtime);
        if (imageGo == null || rawImageGo == null)
        {
            LoggerUtils.LogError("找不到图片组件节点");
            return;
        }
        //获得图片组件
        var rewardImg = imageGo.GetComponent<Image>();
        var remoteRewardRawImg = rawImageGo.GetComponent<RemoteImageBehaviour>();
        //获得文本组件
        var numText = rewardGo.transform.Find("GemNum").GetComponent<Text>();

        //rewardGo.gameObject.SetActive(true);

        //显示文本
        if (data.RewardAmount > 0)
        {
            numText.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(data.rewardSpecial))
            {
                numText.text = data.rewardSpecial;
            }
            else
            {
                numText.text = "x" + data.RewardAmount;
            }
        }
        else
        {
            numText.gameObject.SetActive(false);
        }

        
        if (data.rewardType == (int)BUDRewardType.RewardChatBubbles && !string.IsNullOrEmpty(data.pgcId))
        {
            imageGo.gameObject.SetActive(true);
            rawImageGo.gameObject.SetActive(false);
            UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.pgcId, rewardGo.gameObject, sp => {
                rewardImg.sprite = sp;
            });
        }
        else if(data.rewardType == (int)BUDRewardType.RewardUgcResource) // 显示ugc奖励
        {
            
            // 隐藏Image组件，显示RawImage组件
            imageGo.gameObject.SetActive(false);
            rawImageGo.gameObject.SetActive(true);

            // 加载ugc封面图片
            if (!string.IsNullOrEmpty(data.UgcCover))
            {
                remoteRewardRawImg.Load(
                    data.UgcCover,
                    true,
                    (fromCache, success) => {
                        //如果加载成功
                        if (success)
                        {
                        }
                        else
                        {
                            LoggerUtils.LogError("无法读取ugc图片");
                            // 加载失败时显示默认Image
                            imageGo.gameObject.SetActive(true);
                            rawImageGo.gameObject.SetActive(false);
                            rewardImg.sprite = PgcUtils.LoadRewardIcon(BUDRewardType.RewardUgcResource, gameObject);
                        }
                    }
                );
            }
            else
            {
                LoggerUtils.LogError("UGC封面URL为空");
                // URL为空时显示默认Image
                imageGo.gameObject.SetActive(true);
                rawImageGo.gameObject.SetActive(false);
                rewardImg.sprite = PgcUtils.LoadRewardIcon(BUDRewardType.RewardUgcResource, gameObject);
            }

            //显示图片
            if (data.IconSp != null)
            {
                rewardImg.sprite = data.IconSp;
            }


        }
        else //显示普通奖励
        {
            // 显示Image组件，隐藏RawImage组件
            imageGo.gameObject.SetActive(true);
            rawImageGo.gameObject.SetActive(false);

            if (data.pgcId == "40100499")
            {
                if (!seasonPassReward.activeSelf)
                {
                    GoPosBtn.gameObject.SetActive(true);
                    newImage.gameObject.SetActive(true);
                }
            }

            if (data.IconSp != null)
            {
                
                rewardImg.sprite = data.IconSp;
                if (data.rewardName == "金币")
                {
                    if (!seasonPassReward.activeSelf)
                    {
                        //GoOfficialBtn.gameObject.SetActive(true);
                        //GoTrunBtn.gameObject.SetActive(true);
                    }
                }

                if(data.rewardType == (int)BUDRewardType.RewardTypeTitle)
                {
                    rewardImg.SetNativeSize();
                    rewardImg.transform.localScale *= 0.5f;
                        
                }
            }
            else
            {
                if (data.rewardType != (int)BUDRewardType.ErrRewardType)
                {
                    if (data.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(data.pgcId))
                    {
                        var selfieCfg = DataTables.GetCameraSelfiePose(data.pgcId);
                        if (selfieCfg != null)
                        {
                            const string selfiePoseAtlas = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";
                            Sprite sp = null;
                            if (!string.IsNullOrEmpty(selfieCfg.iconString))
                            {
                                sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(selfiePoseAtlas, selfieCfg.iconString, rewardGo.gameObject);
                                if (sp == null && selfieCfg.iconString.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                                    sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(selfieCfg.iconString, rewardGo.gameObject);
                            }
                            if (sp == null && !string.IsNullOrEmpty(selfieCfg.resourcePath) && selfieCfg.resourcePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                                sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(selfieCfg.resourcePath, rewardGo.gameObject);
                            if (sp != null) rewardImg.sprite = sp;
                        }
                        else
                        {
                            PgcUtils.GetIconSpriteByPgcIdAsync(data.pgcId, rewardGo.gameObject, sp => {
                                rewardImg.sprite = sp;
                            });
                        }
                    }
                    else if (data.rewardType == (int)BUDRewardType.RewardAvatarFrame && !string.IsNullOrEmpty(data.pgcId))
                    {
                        UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.pgcId, rewardGo.gameObject, sp => {
                            rewardImg.sprite = sp;
                        });
                    }
                    else if (data.rewardType == (int)BUDRewardType.RewardChatBubbles && !string.IsNullOrEmpty(data.pgcId))
                    {
                        UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.pgcId, rewardGo.gameObject, sp => {
                            rewardImg.sprite = sp;
                        });
                    }
                    else if (data.rewardType == (int)BUDRewardType.RewardPgcBundle && !string.IsNullOrEmpty(data.bundleId))
                    {
                        rewardImg.sprite = PgcUtils.LoadBundleIcon(data.bundleId, gameObject);
                    }
                    else if(data.rewardType == (int)BUDRewardType.RewardTypeNicknameFrame && !string.IsNullOrEmpty(data.pgcId))
                    {
                        var sprite = UserUIWidgetManager.Inst.GetNicknameBg(int.Parse(data.pgcId), this.gameObject);
                        if (sprite != null)
                        {
                            rewardImg.sprite = sprite;
                            rewardImg.SetNativeSize();
                            rewardImg.transform.localScale *= 0.5f;
                        }
                    }
                    else if(data.rewardType == (int)BUDRewardType.RewardTypeTitle && !string.IsNullOrEmpty(data.pgcId))
                    {
                        var sprite = UserUIWidgetManager.Inst.GetTitleBg(int.Parse(data.pgcId), this.gameObject);
                        if (sprite != null)
                        {
                            rewardImg.sprite = sprite;
                            rewardImg.SetNativeSize();
                            rewardImg.transform.localScale *= 0.5f;
                        }
                    }
                    else
                    {
                        try
                        {
                            rewardImg.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }

                if (data.rewardType == (int)BUDRewardType.RewardCoin || data.rewardType == (int)BUDRewardType.RewardBadge)
                {
                    if (!seasonPassReward.activeSelf)
                    {
                        //GoOfficialBtn.gameObject.SetActive(true);
                        //GoTrunBtn.gameObject.SetActive(true);
                    }
                }
            }
        }

        if (data.isCrit) {
            var critGo = rewardGo.transform.Find("Crit").gameObject;
            critGo.SetActive(true);
        }

        if (data.isConverted) {
            var convertedGo = rewardGo.transform.Find("Converted").gameObject;
            convertedGo.SetActive(true);
        }

        if (isResize)
        {
            SetSpriteSize(rewardImg);
        }
    }

    private void FillSeasonPassRewardItem(CommonRewardItemData data)
    {
        var seasonPassGo = GameObject.Instantiate(seasonPassItem, seasonPassItemRoot).transform;
        var seasonPassImg = seasonPassGo.transform.Find("TopUpContentImage").GetComponent<Image>();
        var seasonPassNumText = seasonPassGo.transform.Find("GemNum").GetComponent<Text>();
        seasonPassGo.gameObject.SetActive(true);

        if (data.RewardAmount > 0)
        {
            seasonPassNumText.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(data.rewardSpecial))
            {
                seasonPassNumText.text = data.rewardSpecial;
            }
            else
            {
                seasonPassNumText.text = "x" + data.RewardAmount;
            }
        }
        else
        {
            seasonPassNumText.gameObject.SetActive(false);
        }
        
        if (data.IconSp != null) {
            seasonPassImg.sprite = data.IconSp;
        } else {
            if (data.rewardType != (int)BUDRewardType.ErrRewardType) {
                if (data.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(data.pgcId)) {
                    PgcUtils.GetIconSpriteByPgcIdAsync(data.pgcId, seasonPassGo.gameObject, sp => {
                        seasonPassImg.sprite = sp;
                    });
                }
                else if(data.rewardType == (int)BUDRewardType.RewardAvatarFrame && !string.IsNullOrEmpty(data.pgcId))
                {
                    UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.pgcId,seasonPassGo.gameObject,sp => {
                        seasonPassImg.sprite = sp;
                    });
                } else if(data.rewardType == (int)BUDRewardType.RewardChatBubbles && !string.IsNullOrEmpty(data.pgcId))
                {
                    UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.pgcId,seasonPassGo.gameObject,sp => {
                        seasonPassImg.sprite = sp;
                    });
                }
                else if (data.rewardType == (int)BUDRewardType.RewardPgcBundle && !string.IsNullOrEmpty(data.bundleId))
                {
                    seasonPassImg.sprite = PgcUtils.LoadBundleIcon(data.bundleId, gameObject);
                    // 套装需要单独调整比例
                    var sprite = seasonPassImg.sprite;
                    var width = sprite.rect.width;
                    var height = sprite.rect.height;
                    if (width > 200 || height > 200) {
                        var scale = 200f / Mathf.Max(width, height);
                        seasonPassImg.rectTransform.sizeDelta = new Vector2(width * scale, height * scale);
                    } else {
                        seasonPassImg.rectTransform.sizeDelta = new Vector2(width, height);
                    }
                }
                else {
                    try {
                        seasonPassImg.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
                    } catch (Exception e) {
                        Debug.LogError(e);
                    }
                }
            }
        }
    }

    private void FillTaskRewardItem(int rewardType, int num)
    {
        var rewardGo = Instantiate(rewardItem, content).transform;
        var rewardImg = rewardGo.transform.Find("TopUpContentImage").GetComponent<Image>();
        var numText = rewardGo.transform.Find("GemNum").GetComponent<Text>();
        rewardImg.gameObject.SetActive(true);
        rewardGo.gameObject.SetActive(true);

        if (num > 0)
        {
            numText.gameObject.SetActive(true);
            numText.text = "x" + num;
        }
        else
        {
            numText.gameObject.SetActive(false);
        }

        var path = string.Format("ic_rewards_big_{0}", rewardType);
        var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, path, gameObject);

        if (sprite == null)
        {
            bool canConvert = Enum.IsDefined(typeof(BUDRewardType), rewardType);
            if (canConvert)
            {
                sprite = PgcUtils.LoadRewardIcon((BUDRewardType)rewardType, gameObject);
            }
        }

        if (sprite != null)
        {
            rewardImg.sprite = sprite;
        }
    }

    private void FillSingleHint(TaskRewardData rewardData)
    {
        string rewardName = LocalizationManager.Inst.GetLocalizedText(GetRewardPresentName((BUDRewardType)rewardData.rewardType));
        hintTxt.SetLocalText("你获得了 <color=#FFD400>{0} {1}</color>",rewardData.num,rewardName);
    }

    private void SetSpriteSize(Image rewardImg)
    {
        if (rewardImg.sprite == null)
        {
            rewardImg.transform.localScale = Vector3.one;
            return;
        }

        rewardImg.rectTransform.sizeDelta = rewardImg.sprite.rect.size;

        float spSize = rewardImg.sprite.rect.size.x;
        if (rewardImg.sprite.rect.size.y > spSize)
        {
            spSize = rewardImg.sprite.rect.size.y;
        }

        if (spSize > 250)
        {
            var scale = 250 / spSize;
            rewardImg.transform.localScale = Vector3.one * scale;
        }
        else
        {
            rewardImg.transform.localScale = Vector3.one;
        }
    }

    private string GetRewardPresentName(BUDRewardType type)
    {
        if (PgcUtils.RewardName.ContainsKey(type))
        {
            return PgcUtils.RewardName[type];
        }

        return "";
    }

    private bool IsAspectRatioCloseToTable(float tolerance = 0.01f)
    {
        // 计算当前屏幕的长宽比
        float aspectRatio = (float)Screen.width / Screen.height;

        // 定义 4:3 的长宽比
        float targetAspectRatio = 4f / 3f;

        // 判断当前长宽比是否在目标长宽比的误差范围内
        return Mathf.Abs(aspectRatio - targetAspectRatio) < tolerance;
    }

    public void SetCloseAct(Action closeAct)
    {
        this._onCloseAct = closeAct;
    }

    #region 对外工具方法
    public static bool HasRepleaceReward(ActivityEventClaimResponse rewardRsp)
    {
        return rewardRsp != null && rewardRsp.replaceRewardList != null && rewardRsp.replaceRewardList.Count > 0;
    }
    
    public static string GetRepleaceTips(ActivityEventClaimResponse rewardRsp)
    {
        string resultStr = "";
        if (HasRepleaceReward(rewardRsp))
        {
            resultStr = "重复获得的奖励已经转化为";
            for (int i = 0, C = rewardRsp.replaceRewardList.Count; i < C; i++)
            {
                if (i > 0) resultStr += "和";
                resultStr += $"{rewardRsp.replaceRewardList[i].amount}{PgcUtils.GetRewardName((BUDRewardType)rewardRsp.replaceRewardList[i].rewardType)}";
            }
        }
        return resultStr;
    }
    
    #endregion
   
}
