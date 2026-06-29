using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.AINPCStudio;
using Game.Avatar;
using GameData;
using GameData.Account;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyProfilePanel : BasePanel<AIBuddyProfilePanel>
{
   [SerializeField] protected Transform BG;
   [SerializeField] protected Text NpcName;
   [SerializeField] protected Text GenderText;
   [SerializeField] protected Text AgeText;
   [SerializeField] protected AIBuddyIntimacyView IntimacyView;
   [SerializeField] protected CButton GiftBtn;
   [SerializeField] protected CButton ChatBtn;
   [SerializeField] protected CButton BackBtn;
   [SerializeField] protected CButton IntimacyBtn;
   [SerializeField] protected GameObject IntimacyRedDot;
   [SerializeField] protected NpcDescWidget DescWidget;
   [SerializeField] protected NpcAnimWidget NpcAnimWidget;
   [SerializeField] protected Transform CharacterRoot;
   [SerializeField] protected AvatarCameraController cameraController;
   [SerializeField] protected AIBuddyGiftView GiftView;
   private CharacterWrap characterWrap;
   private AIBuddyInfo _aiBuddyInfo;
   private PgcNpcIdleBehaviour pgcIdleBehaviour;
   private UgcNpcIdleBehaviour ugcIdleBehaviour;
   private AINpcAnimType curNpcType = AINpcAnimType.Idle;
   
   public override void OnCreate()
   {
      base.OnCreate();
      BackBtn.onClick.AddListener(OnBackBtnClick);
      GiftBtn.onClick.AddListener(OnGiftBtnClick);
      ChatBtn.onClick.AddListener(OnChatBtnClick);
      IntimacyBtn.onClick.AddListener(OnIntimacyBtnClick);
      GiftView.Hide();
      MessageHelper.AddListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated, OnRefreshInfoSuccess);
      MessageHelper.AddListener(MessageName.OnAINpcChatPanelClose, OnChatPanelClose);
   }

   protected override void OnDestroy()
   {
      base.OnDestroy();
      MessageHelper.RemoveListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated, OnRefreshInfoSuccess);
      MessageHelper.RemoveListener(MessageName.OnAINpcChatPanelClose, OnChatPanelClose);
   }

   public override void OnShow(params object[] args)
   {
      base.OnShow(args);
      _aiBuddyInfo = args[0] as AIBuddyInfo;
      InitBG();
      InitUI();
      AIBuddyDataManager.Inst.RequestAIBuddyInfo(_aiBuddyInfo.id);
   }

   public override void OnWindowShow()
   {
      base.OnWindowShow();
      if (_aiBuddyInfo != null)
      {
         AIBuddyDataManager.Inst.RequestAIBuddyInfo(_aiBuddyInfo.id);
      }
   }

   private void InitUI()
   {
      var npcInfo = _aiBuddyInfo.npc;
      SetIntimacy(_aiBuddyInfo.intimacyRate);
      NpcAnimWidget.InitData(npcInfo);
      characterWrap = NpcAnimWidget.CharacterWrap;
      GiftView.InitData(_aiBuddyInfo, characterWrap);
      GiftView.AddSendGiftSuccessListener(OnSendGiftSuccess);
      DescWidget.SetDescription(_aiBuddyInfo.npc.npcDesc);
      SetNpcName(npcInfo.npcName);
      SetGender(npcInfo.npcGender);
      SetAge(npcInfo.npcAge);

   }

   private void SetNpcName(string npcName)
   {
      NpcName.text = npcName;
   }

   private void SetGender(int gender)
   {
      GenderText.SetLocalText("性别：" + (gender == 1 ? "男" : "女"));
   }

   private void SetAge(int age)
   {
      if (age < 0)
      {
         AgeText.SetLocalText("年龄：不详");
      }
      else
      {
         AgeText.SetLocalText("年龄：{0}", age);
      }
   }

   private void SetIntimacy(int value)
   {
      IntimacyView.SetValue(value);
   }

   private void InitBG()
   {
      string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
      var itemObj = Loader
         .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
         .Instantiate(BG);
      var item = itemObj.GetComponent<ActivityCenterBgItem>();
      item.InitCustomBgItem("#8860F8", atlasPath, new List<string>()
      {
         "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
      });
      item.SetImagesColor(new Color(1, 1, 1, 0.4f));
      item.gameObject.SetActive(true);
   }
   
   private void OnSendGiftSuccess(AIBuddyInfo aiBuddyInfo)
   {
      if (aiBuddyInfo == null)
      {
         LoggerUtils.Log("OnSendGiftSuccess aiBuddyInfo is null");
         return;
      }

      AIBuddyDataManager.Inst.RequestAIBuddyInfo(aiBuddyInfo.id);
   }
   

   private void OnRefreshInfoSuccess(AIBuddyInfoRsp infoRsp)
   {
      if (infoRsp == null || infoRsp.info == null)
      {
         LoggerUtils.Log("OnRefreshInfoSuccess aiBuddyInfo is null");
         return;
      }

      if (infoRsp.info.id == _aiBuddyInfo.id)
      {
         _aiBuddyInfo = infoRsp.info;
         SetIntimacy(_aiBuddyInfo.intimacyRate);
         
         //刷新红点
         bool hasRedDot = infoRsp.taskReddot > 0;
         IntimacyRedDot.SetActive(hasRedDot);
      }
   }
   

   private void OnGiftBtnClick()
   {
      GiftView.Show();
   }

   private void OnChatBtnClick() {
       UIManager.Inst.OpenPanel<AINpcChatPanel>(PanelId.AINpcChatPanel, _aiBuddyInfo);
   }

   private void OnChatPanelClose()
   {
      if (!this) return;
      //关闭聊天弹窗的时候，刷新一下外部红点，因为可能触发完成：聊天任务
      // ReddotManagerUtils.Inst.RefreshRedDot();
      AIBuddyDataManager.Inst.RequestAIBuddyInfo(_aiBuddyInfo.id);
   }

   private void OnIntimacyBtnClick()
   {
      UIManager.Inst.OpenPanel(PanelId.AIBuddyTaskPanel,_aiBuddyInfo);
   }

   private void OnBackBtnClick()
   {
      CloseSelf();
   }

}
