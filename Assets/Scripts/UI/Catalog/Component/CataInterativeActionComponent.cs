using Game.Database;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Catalog;
using UI.Catalog.Components;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class Reward
{
    public int rewardType;
    public int amount;
    public List<string> pgcIdList;
}

// 完整响应类
public class BackpackResponse
{
    public List<Reward> rewards;
}



public class CataInterativeActionComponent : CatalogComponentBase
{
    //public override CatalogComponentType ComponentType => throw new System.NotImplementedException();
    List<RoleEmote> _roleEmotes;
    Gallery _gallery;
    int haveCnt;
    int cnt;

    public int isClaim;

    public Slider slider;
    public Text sliderText;

    public CButton bagButon;

    public GameObject BagImg;
    public GameObject claimBagImg;
    public GameObject InterativeAction;

    public Transform center;

    List<GameObject> Items;
    
    // 角色动作ID列表 - 用于红点系统
    private List<string> _actionIds = new List<string>();
    
    void Awake()
    {
        Items = new List<GameObject>();
    }

    public override void updateUI(Gallery gallery)
    {
        _roleEmotes = gallery.roleGallery.roleEmote;
        _gallery = gallery;
        isClaim = gallery.isClaim;
        Init();
    }
    
    public void Init()
    {
        RemoveData();
        


        bagButon.interactable = false;
        _actionIds.Clear();
        
        foreach (var emote in _roleEmotes) {
            cnt++;
            if (emote.islock!=1) { haveCnt++; }
            
            
            var item = Instantiate(InterativeAction, center);
            var interactiveItem = item.GetComponent<InteractiveItem>();
            
            // 初始化UI
            interactiveItem.InitUI(emote , _gallery.galleryId , _gallery.roleGallery.roleAvatar);
            
            Items.Add(item);
        }

        sliderText.text = haveCnt.ToString() + "/" + cnt.ToString();
        slider.value = (float)haveCnt / cnt;

        if(slider.value == 1)
        {
            //获取奖励
            bagButon.interactable = true;
        }




        if (isClaim == 1)
        {
            BagImg.SetActive(false);
            claimBagImg.SetActive(true);
        }
        else
        {
            BagImg.SetActive(true);
            claimBagImg.SetActive(false);
        }
    }
    
    

    
    void RemoveData()
    {
        cnt = 0;
        haveCnt = 0;

        if (Items != null) {
            foreach (var item in Items)
            {
                Destroy(item);
            }
        }
        Items = new List<GameObject>();
    }

    public void OnClickBag()
    {
        JObject k = new JObject
        {
            ["mapId"] = "",
            ["gameId"] = (int)PGCGameType.AIHospital,
            ["galleryId"] = _gallery.galleryId
        };

        var paramStr = JsonConvert.SerializeObject(k);

        NetworkManager.Inst.SendHttpRequest(
                "/aigame/gallery/claimReward",
                HttpMethod.POST,
                paramStr,
                onReceive: msg =>
                {
                    // 打开通用奖励展示面板
                    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

                    // 反序列化
                    var response = JsonConvert.DeserializeObject<BackpackResponse>(msg);
                    var rewards = response.rewards;

                    // 创建用于显示的奖励数据列表
                    var taskRewardDatas = new List<CommonRewardItemData>();

                    foreach (var reward in rewards) {
                        var item = new CommonRewardItemData();
                        item.rewardType = reward.rewardType;
                        item.RewardAmount = reward.amount;
                        //item.rewardName = ;
                        item.pgcId = reward.pgcIdList[0];
                        taskRewardDatas.Add(item);
                    }

                    BagImg.SetActive(false);
                    claimBagImg.SetActive(true);

                    // 显示所有奖励
                    panel.ShowRewards(taskRewardDatas);
                    // 刷新账户余额信息
                    AccountDataManager.Inst.BalanceInfo.Refresh();

                },
                (error) =>
                {
                    LoggerUtils.LogError($"获取奖励失败: {error}");
                }
            );
    }
}

// 用于传递角色动作数据的类
public class CharacterActionData
{
    public string CharacterId;
    public List<string> ActionIds;
}
