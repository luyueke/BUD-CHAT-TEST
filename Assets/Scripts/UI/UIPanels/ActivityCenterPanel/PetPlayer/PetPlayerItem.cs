using System;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class PetPlayerItem : MonoBehaviour
{
    public Button rootBtn;
    public GameObject coverLayout;
    public Image bg;
    public Text progressTxt;
    public Slider progress;
    public GameObject lockObj;
    public Text numTxt;

    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private string _taskId;

    private ActivityEventInfo _info;
    private Action<ActivityEventInfo, TaskClaimState> _claimAction;
    public TaskClaimState eventStatus { get; set; }
    
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";

    private void Start()
    {
    }
    
    public void Init(ActivityEventInfo activityEventInfo, Action<ActivityEventInfo, TaskClaimState> clickAction)
    {
        eventStatus = TaskClaimState.Unable;
        _info = activityEventInfo;
        _claimAction = clickAction;
        UpdateProgress(activityEventInfo);
        rootBtn?.onClick.RemoveAllListeners();
        rootBtn?.onClick.AddListener(() =>
        {
            if (this == null || _info == null)
            {
                return;
            }
            
            _claimAction?.Invoke(_info, eventStatus);
        });
    }
    
    private void UpdateProgress(ActivityEventInfo data)
    {
        if (data == null)
        {
            return;
        }

        var targetAmount =  data.targetAmount;
        var finishAmount = data.finishAmount;
        progressTxt.text = $"{finishAmount}/{targetAmount}";
        progress.value = finishAmount /(targetAmount * 1.0f);
    }


    public void Refresh(ActivityEventInfo data)
    {
        eventStatus = (TaskClaimState)data.eventStatus;
        UpdateProgress(data);
        UpdateState();
       
    }
    
    private void UpdateState()
    {
        if (eventStatus == TaskClaimState.Enable)
        {
            numTxt.color = DataUtil.DeSerializeColorCheckHash("#FFD336");
        }
        lockObj.gameObject.SetActive(eventStatus == TaskClaimState.Unable);
        coverLayout.gameObject.SetActive(eventStatus == TaskClaimState.Finished);
        bg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath,
            eventStatus == TaskClaimState.Enable ? "pet_player_active" : "pet_player_inactive",
            gameObject);
        if (eventStatus == TaskClaimState.Enable)
        {
            rootBtn.transform.GetComponent<Animator>().enabled = true;
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_prompt", 0.1f);
        }
        else
        {
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_done", 0.1f);
        }
        
    }
    
}