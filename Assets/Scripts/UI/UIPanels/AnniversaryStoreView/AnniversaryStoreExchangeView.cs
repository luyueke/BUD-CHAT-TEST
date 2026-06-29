using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryStoreExchangeView : MonoBehaviour
{
    public AnniversaryStoreExchangeItem itemPrefab;
    public Transform content;
    public Slider slider;
    public List<Toggle> toggles;

    private List<RewardItem> _rewardItems = new List<RewardItem>();
    private ActivityInfo _activityInfo;

    // --- 核心修改：升级经验配置 ---
    // 这个数组现在代表【升到下一级所需要的经验】
    // prices[0] = 升到2级需要240经验
    // prices[1] = 升到3级需要400经验 ...
    // 注意：为了方便计算，我在最后加了一个很大的值作为满级的封顶
    private List<int> experienceToNextLevel = new List<int> { 240, 400, 600, 800, 1200, int.MaxValue };

    // --- 新增变量来存储计算结果 ---
    private int currentShopLevel = 1;     // 当前商店等级
    private int currentLevelExperience = 0; // 当前等级下的经验值
    private int requiredExperience = 0;   // 当前等级升到下一级所需要的经验
    private int totalProgress = 0;        // 玩家拥有的总进度值

    private void Start()
    {
        string jsonPath = "Assets/Loadable/UI/UIPanel/AnniversaryStoreView/AnniversaryStoreShopData.json";
        var ugcAsset = Loader.Load<TextAsset>(jsonPath, this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

        InitTogles();
        GetActivityCenterInfo();
    }

    public void InitTogles()
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            int index = i + 1;
            toggles[i].onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    GenderTaskItem(index);
                    if (toggles.Count > index - 1 && toggles[index - 1] != null)
                    {
                        toggles[index - 1].transform.Find("reddot").gameObject.SetActive(false);
                    }
                }
            });
            toggles[i].transform.Find("Label").GetComponent<Text>().text = $"{index}级";
        }
    }

    private void GetActivityCenterInfo(bool isInitialLoad = true)
    {
        AnniversaryStoreMgr.Inst.GetStoreExchangeData((info) =>
        {
            _activityInfo = info;
            CalculateCurrentLevelAndExperience();

            if (isInitialLoad)
            {
                if (currentShopLevel > 0 && currentShopLevel <= toggles.Count)
                {
                    toggles[currentShopLevel - 1].isOn = true;
                }
            }
        });
    }

    /// <summary>
    /// 核心逻辑：根据总进度，计算出当前等级和当前等级的经验值 (经验条模式)
    /// </summary>
    private void CalculateCurrentLevelAndExperience()
    {
        // 1. 计算总进度
        totalProgress = 0;
        if (_activityInfo?.rewardList != null)
        {
            foreach (var reward in _activityInfo.rewardList)
            {
                if (reward.rewardStatus == (int)EventStatus.UnClaim)
                {
                    var localItem = _rewardItems.Find(x => x.rewardId == reward.rewardId.ToString());
                    if (localItem != null)
                    {
                        totalProgress += localItem.progress;
                    }
                }
            }
        }

        // 2. 模拟升级过程，计算出最终的等级和剩余经验
        currentShopLevel = 1;
        int experienceLeft = totalProgress; // 剩余经验初始等于总进度

        // 遍历升级所需的经验列表
        for (int i = 0; i < experienceToNextLevel.Count; i++)
        {
            int expNeeded = experienceToNextLevel[i];

            // 如果剩余经验足够升级
            if (experienceLeft >= expNeeded)
            {
                currentShopLevel++;          // 等级提升
                experienceLeft -= expNeeded; // 扣除升级消耗的经验
            }
            // 如果经验不够升级，则循环结束
            else
            {
                break;
            }
        }

        // 3. 存储计算结果
        // 当前等级的经验值就是模拟升级后剩下的经验
        currentLevelExperience = experienceLeft;
        // 升到下一级需要的经验
        // 等级是从1开始，而数组索引是从0开始，所以要-1
        requiredExperience = experienceToNextLevel[currentShopLevel - 1];

        // 4. 更新所有UI显示
        UpdateUI();
    }

    /// <summary>
    /// 统一更新所有相关的UI
    /// </summary>
    private void UpdateUI()
    {
        UpdateTogglesLockState();
        UpdateSliderDisplay();

        int currentPageIndex = 1;
        for (int i = 0; i < toggles.Count; i++) { if (toggles[i].isOn) { currentPageIndex = i + 1; break; } }
        GenderTaskItem(currentPageIndex);
    }

    /// <summary>
    /// 更新进度条和文本显示 (经验条模式)
    /// </summary>
    private void UpdateSliderDisplay()
    {
        // 如果已经满级
        if (currentShopLevel > experienceToNextLevel.Count - 1)
        {
            slider.value = 1f;
            slider.transform.Find("numLevel").GetComponent<Text>().text = $"商店等级5/5";
            slider.transform.Find("numTarget").GetComponent<Text>().text = "1200/1200";
            return;
        }
        else
        {
            slider.value = (float)currentLevelExperience / requiredExperience;
            slider.transform.Find("numTarget").GetComponent<Text>().text = $"{currentLevelExperience}/{requiredExperience}";
        }

        slider.transform.Find("numLevel").GetComponent<Text>().text = $"商店等级{currentShopLevel}/5";
    }

    private void UpdateTogglesLockState()
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            bool shouldBeLocked = (i + 1) > currentShopLevel;
            toggles[i].transform.Find("lock").gameObject.SetActive(shouldBeLocked);
        }
    }

    void GenderTaskItem(int pageIndex)
    {
        foreach (Transform child in content) { Destroy(child.gameObject); }
        if (_activityInfo?.rewardList == null) return;

        int startId = (pageIndex - 1) * 3 + 1;
        int endId = startId + 2;

        var rewardList = _activityInfo.rewardList
            .Where(x => x.rewardId >= startId && x.rewardId <= endId)
            .OrderBy(x => x.rewardId)
            .ToList();

        bool isPageLocked = pageIndex > currentShopLevel;

        foreach (var reward in rewardList)
        {
            var localData = _rewardItems.Find(x => x.rewardId == reward.rewardId.ToString());
            if (localData == null) continue;
            var item = Instantiate(itemPrefab, content);
            item.SetData(localData, reward, OnItemExchanged, isPageLocked);
        }
    }

    void OnItemExchanged(RewardItem item)
    {
        // 检查兑换后是否可以升级
        if (currentShopLevel < toggles.Count)
        {
            // 兑换后的新总进度
            int progressAfterExchange = totalProgress + item.progress;

            // 重新计算一下兑换后的等级
            int tempLevel = 1;
            int tempExp = progressAfterExchange;
            for (int i = 0; i < experienceToNextLevel.Count; i++)
            {
                if (tempExp >= experienceToNextLevel[i])
                {
                    tempLevel++;
                    tempExp -= experienceToNextLevel[i];
                }
                else break;
            }

            // 如果兑换后等级提升了，就在新解锁的页签上显示红点
            if (tempLevel > currentShopLevel)
            {
                toggles[currentShopLevel].transform.Find("reddot").gameObject.SetActive(true);
            }
        }

        // 重新从服务器获取最新数据，成功后会自动调用所有计算和刷新
        GetActivityCenterInfo(false);
    }
}