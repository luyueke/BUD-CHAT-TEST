using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CreatorWayView : MonoBehaviour
{
    [SerializeField] private CreatorWayItem itemPrefab;
    private CreatorCenterPanel Panel;
    private List<CreatorCenterTaskLocalInfo> creatorCenterTaskLocalInfo;
    private List<CreatorWayItem> viewItems = new List<CreatorWayItem>();
    [SerializeField] private Text progressText;
    [SerializeField] private Text levelText;
    [SerializeField] private Slider slider;

    [SerializeField] private Button claimAllBtn;
    [SerializeField]private CreatorWayEntry _entry;
    private Action<CreatorCenterData> cliamAction;
    public void Init(CreatorCenterPanel panel, Action<CreatorCenterData> cliamAction)
    {
        this.cliamAction = cliamAction;
        Panel = panel;
        var configAsset =
            Loader.Load<TextAsset>("Assets/Loadable/UI/UIPanel/CreatorCenterPanel/CreatorWayTaskConfig.json",
                gameObject);
        var tmpEventInfos = JsonConvert.DeserializeObject<List<CreatorCenterTaskLocalInfo>>(configAsset.text);
        if (tmpEventInfos!=null)
        {
            tmpEventInfos.Sort((x,y)=>int.Parse(x.taskId).CompareTo(int.Parse(y.taskId)));
            creatorCenterTaskLocalInfo = tmpEventInfos;
            for (int i = 0; i < creatorCenterTaskLocalInfo.Count; i++)
            {
                creatorCenterTaskLocalInfo[i].rewardStatus = (int)BudRewardStatus.Lock;
            }
        }
        claimAllBtn.onClick.AddListener(ClaimAllReward);
    }
    bool isInit = false;
    private void OnEnable()
    {
        if (creatorCenterTaskLocalInfo!=null&&!isInit)
        {
            isInit = true;
            _entry.adapter.Init();
            _entry.SetActions(OnCliam);
            Refrash();
        }
        
    }

    public void InitData()
    {
        Refrash();
    }

    public void RefrashLocalInfo()
    {
        if (Panel.Data!=null)
        {
            if (Panel.Data.creatorsPath.taskList!=null)
            {
                for (int i = 0; i < Panel.Data.creatorsPath.taskList.Count; i++)
                {
                    var info = creatorCenterTaskLocalInfo.Find(x => x.taskId == Panel.Data.creatorsPath.taskList[i].taskId);
                    if (info == null)
                    {
                        Debug.LogError(Panel.Data.creatorsPath.taskList[i].taskId);
                    }else
                    {
                        info.rewardStatus = Panel.Data.creatorsPath.taskList[i].rewardStatus;
                    }
  
                }
            }
            levelText.text = Panel.Data.creatorsPath.level+"级";
            progressText.text = Panel.Data.creativeTasks.creatorLevelInfo.point + "/" +
                                Panel.Data.creatorsPath.progressInfo.end + "积分";
            slider.maxValue = Panel.Data.creatorsPath.progressInfo.end;
            slider.minValue = Panel.Data.creatorsPath.progressInfo.start;
            slider.value = Panel.Data.creativeTasks.creatorLevelInfo.point;
        }
       
    }
    public void Refrash()
    {
        RefrashLocalInfo();
        if (isInit)
        {
            _entry.OnReceivedNewModelsForInsert(creatorCenterTaskLocalInfo);
            int moveInt = 0;
            if (creatorCenterTaskLocalInfo!=null)
            {
                for (int i = 0; i < creatorCenterTaskLocalInfo.Count; i++)
                {
                    if (creatorCenterTaskLocalInfo[i].rewardStatus == (int)BudRewardStatus.Unlocked)
                    {
                        moveInt =i;
                        break;
                    }

                    if (creatorCenterTaskLocalInfo[i].rewardStatus == (int)BudRewardStatus.Lock)
                    {
                        moveInt =i;
                        break;
                    }
                }
            }
            _entry.MoveTo(moveInt);
            RefrashGetAllBtn();
        }
    }
    private void RefrashGetAllBtn()
    {
        bool isCanTakeAll = false;
        if( Panel.Data.creatorsPath==null||Panel.Data.creatorsPath.taskList==null)
            return;
        for (int i = 0; i < Panel.Data.creatorsPath.taskList.Count; i++)
        {
            var taskList = Panel.Data.creatorsPath.taskList;
            for (int j = 0; j < taskList.Count; j++)
            {
                if (taskList[j].rewardStatus == (int)BudRewardStatus.Unlocked)
                {
                    isCanTakeAll = true;
                    break;
                }
            }
        }
        claimAllBtn.interactable = isCanTakeAll;
    }
    private bool isSending;
    public void OnCliam(CreatorCenterTaskLocalInfo info)
    {
        if (isSending) {
            return;
        }
        isSending = true;
        JObject jObject = new JObject();
        jObject["pageType"] = 2;
        jObject["taskId"] = info.taskId;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) => {
                isSending = false;
                OnClaimSuccess(content);
            },
            (error) => {
                isSending = false;
            });
    }
    public void ClaimAllReward() {
        if (isSending) {
            return;
        }
        isSending = true;
        JObject jObject = new JObject();
        jObject["isAll"] = 1;
        jObject["pageType"] = 2;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                OnClaimSuccess(content);
                isSending = false;
            },
            (error) => {
                isSending = false;
            });
    }
    private void OnClaimSuccess(string content){
        CreatorRewardGetData creatorRewardGetData = JsonConvert.DeserializeObject<CreatorRewardGetData>(content);
        CreatorCenterData creatorCenterData = new CreatorCenterData()
        {
            creatorsPath = creatorRewardGetData.creatorsPath,
            creativeTasks = creatorRewardGetData.creativeTasks
        };
        var commonRewardData = new List<CommonRewardItemData>();
        for (int i = 0; i < creatorRewardGetData.rewardList.Count; i++)
        {
            var commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.RewardAmount = creatorRewardGetData.rewardList[i].amount;
            commonRewardItemData.rewardType = (int)creatorRewardGetData.rewardList[i].rewardType;
            commonRewardItemData.pgcId = creatorRewardGetData.rewardList[i].pgcId;
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)creatorRewardGetData.rewardList[i].rewardType);
            commonRewardData.Add(commonRewardItemData);
        }
        var rewardPanel =  UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        rewardPanel.ShowRewards(commonRewardData);
                
        cliamAction?.Invoke(creatorCenterData);
       
    }
    // 创建一个列表项
    private void CreateItem(int index)
    {
        // var itemObj = Instantiate(itemPrefab,content);
        // var itemComp = itemObj.GetComponent<CreatorWayItem>();
        CreatorTaskData taskData = null;
        if (Panel.Data.creatorsPath.taskList==null||Panel.Data.creatorsPath.taskList.Find(x=>x.taskId == creatorCenterTaskLocalInfo[index].taskId)==null)
        {
            taskData = new CreatorTaskData()
            {
                taskId = creatorCenterTaskLocalInfo[index].taskId,
                rewardStatus = (int)BudRewardStatus.Lock,
            };
        }
        else
        {
            taskData = Panel.Data.creatorsPath.taskList.Find(x => x.taskId == creatorCenterTaskLocalInfo[index].taskId);
        }
       
    }
  

   
}
