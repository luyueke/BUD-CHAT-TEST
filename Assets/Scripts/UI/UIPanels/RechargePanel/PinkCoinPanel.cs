using System.Collections.Generic;
using Game.Event;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


public class PinkCoinPanel : BasePanel<PinkCoinPanel>
{
    [SerializeField] private Transform bg;
    [SerializeField] private CButton backBtn;
    [SerializeField] private PinkTaskItem pinkTaskItem;

    public Transform taskContent;
    private List<PinkTaskItem> pinkTaskItems = new List<PinkTaskItem>();
    private List<RewardItem> _rewardItems = new List<RewardItem>();

    private string _taskId = "NewbieCheckIn";

    private TaskInfoData _taskInfoData;
    
    private bool isInit = false;

    public override void OnCreate()
    {
        base.OnCreate();

        var itemObj = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            ?.Instantiate(bg);
        if (itemObj)
        {
            string atlasPath = RechargePanel.RechargePanelAtlas;
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item?.InitCustomBgItem("#a645f6", atlasPath,
                new List<string>()
                {
                    "pinktask_bg1", "pinktask_bg2"
                });
            item?.gameObject.SetActive(true);
        }

        InitClickListener();
        taskContent.gameObject.SetActive(false);
        RefreshTaskStatus();
    }


    private void GenerateContent()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        string jsonPath = "Assets/Loadable/UI/RechargePanel/config/S6PinkCoinConfig.json";
        var ugcAsset =
            Loader.Load<TextAsset>(
                jsonPath, this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

        pinkTaskItems.Clear();
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }

        for (var i = 0; i < _rewardItems.Count; i++)
        {
            var taskItem = Instantiate(pinkTaskItem, taskContent);
            taskItem.OnInitCreate(_rewardItems[i]);
            pinkTaskItems.Add(taskItem);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


    private void RefreshTaskStatus()
    {
        IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
        {
            if (taskInfoResponse == null)
            {
                return;
            }

            List<TaskInfoData> taskInfoDatas = taskInfoResponse.list;
            if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
            {
                return;
            }

            taskContent.gameObject.SetActive(true);

            TaskInfoData tsTaskInfoData = taskInfoDatas[0];
            if (tsTaskInfoData == null)
            {
                return;
            }

            this._taskInfoData = tsTaskInfoData;
            GenerateContent();

            List<TaskItemData> eventList = tsTaskInfoData.eventList;
            for (int i = 0; i < pinkTaskItems.Count; i++)
            {
                pinkTaskItems[i].SetData(tsTaskInfoData.taskId, eventList[i], taskItemData => { RefreshTaskStatus(); });
            }
        });
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        RefreshTaskStatus();
    }

    private void InitClickListener()
    {
        backBtn.onClick.AddListener(() => {
            CloseSelf(); 
        });
    }
    
}