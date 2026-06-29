using System.Collections.Generic;
using GameData.Gashapon;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
     public class BaseEventItem : MonoBehaviour
     {
          protected Image Img_Bg;
          protected Text Txt_EventName;
          protected Text Txt_EventCount;
          protected Image Img_RewardIcon;
          protected Text Txt_RewardCount;
          protected CButton Btn_Claim;
          protected GameObject Go_Finished;

          protected string _taskId;
          protected const string atlasPath = "Assets/Loadable/UI/UIPanel/EventCenter/EventCenter.spriteatlas";
          protected TaskItemData _curData;
          
          //绑定UI的操作
          public virtual void BindUI()
          {
               Img_Bg = GameObjectEx.FindChildByName(this.transform, "Img_Bg").GetComponent<Image>();
               Txt_EventName = GameObjectEx.FindChildByName(this.transform, "Txt_EventName").GetComponent<Text>();
               Txt_EventCount = GameObjectEx.FindChildByName(this.transform, "Txt_EventCount").GetComponent<Text>();
               Img_RewardIcon = GameObjectEx.FindChildByName(this.transform, "Img_RewardIcon").GetComponent<Image>();
               Txt_RewardCount = GameObjectEx.FindChildByName(this.transform, "Txt_RewardCount").GetComponent<Text>();
               Btn_Claim = GameObjectEx.FindChildByName(this.transform, "Btn_Claim").GetComponent<CButton>();
               Btn_Claim.onClick.RemoveAllListeners();
               Btn_Claim.onClick.AddListener(OnBtnClaimClick);
               Go_Finished = GameObjectEx.FindChildByName(this.transform, "Go_Finished").gameObject;
          }

          //初始化与数据绑定
          public virtual void InitData(string taskId, TaskItemData data)
          {
               BindUI();

               this._taskId = taskId;
               this._curData = data;
               SetItemBg(this._taskId);
               SetEventName(this._curData.eventName);
               SetEventCount(this._curData.targetAmount);
               SetRewardIcon(this._curData.rewardList);
               SetRewardCount(this._curData.rewardList);
               SetClaimState((TaskClaimState)this._curData.eventStatus);
          }

          #region 各类方法
          public void SetItemBg(string taskId)
          {
               var spriteName = "TaskItemBg_" + taskId;
               Img_Bg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
          }
          
          public void SetEventName(string eventName)
          {
               if (eventName.Contains("{0}") && _curData != null && _curData.targetAmount > 0)
               {
                    Txt_EventName.SetLocalText(eventName,this._curData.targetAmount);
               }
               else
               {
                    Txt_EventName.SetLocalText(eventName);
               }
          }

          public virtual void SetEventCount(int count)
          {
               if (count > 1)
                    Txt_EventCount.text =  "x" +count.ToString();
               else
                    Txt_EventCount.text = "";
          }

          public virtual void SetRewardIcon(List<TaskClaimRewardData> rewardList)
          {
               var data = rewardList[0];
               var spriteName = "RewardIcon_" + data.rewardType;
               Img_RewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
          }

          public virtual void SetRewardCount(List<TaskClaimRewardData> rewardList)
          {
               var data = rewardList[0];
               Txt_RewardCount.text = "x" + data.amount.ToString();
          }

          public virtual void SetClaimState(TaskClaimState state)
          {
               switch (state)
               {
                    case TaskClaimState.Unable:
                         Go_Finished.SetActive(false);
                         Btn_Claim.gameObject.SetActive(false);
                         break;
                    case TaskClaimState.Enable:
                         Go_Finished.SetActive(false);
                         Btn_Claim.gameObject.SetActive(true);
                         break;
                    case TaskClaimState.Finished:
                         Go_Finished.SetActive(true);
                         Btn_Claim.gameObject.SetActive(false);
                         break;
               }
          }

          public virtual void OnBtnClaimClick()
          {
               EventCenterDataManager.Inst.CliamReward(this._taskId, this._curData.eventId, 1, 0,(claimRspData) =>
               {
                    if (claimRspData == null || claimRspData.eventList == null)
                         return;

                    // var itemData = claimRspData.eventList[0];
                    // this._curData.RefreshEventStatus(itemData.eventStatus);
                    // SetClaimState((TaskClaimState)this._curData.eventStatus);
                    
                    //1.刷新本地数据
                    EventCenterDataManager.Inst.GetTaskInfo();

                    //2.弹出领奖弹窗
                    var rewardList = new List<CommonRewardItemData>();

                    foreach (var rewardData in _curData.rewardList)
                    {
                         CommonRewardItemData commonRewardItemData = null;
                         commonRewardItemData = rewardList.Find(x => x.rewardType == rewardData.rewardType);
                         if (commonRewardItemData == null)
                         {
                              commonRewardItemData = new CommonRewardItemData();
                              rewardList.Add(commonRewardItemData);
                         }
                    
                         commonRewardItemData.rewardType = rewardData.rewardType;
                         var spriteName = "RewardIcon_" + rewardData.rewardType;
                         var sp  = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
                         commonRewardItemData.IconSp = sp;
                         commonRewardItemData.rewardName = PgcUtils.GetTokenName((CurrencyType)rewardData.rewardType);
                         commonRewardItemData.RewardAmount += rewardData.amount;
                    }
                    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    panel.ShowRewards(rewardList);
                    
                    AccountDataManager.Inst.BalanceInfo.Refresh();
               });
          }
          #endregion
     }
}
