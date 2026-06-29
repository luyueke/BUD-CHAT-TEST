using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Config;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class AINpcEditPanel : BasePanel<AINpcEditPanel>
{
   public enum NpcInfoEnum
   {
      Nick = 0,
      Gender,
      Age,
      Image,
      Anim,
      Catchphrases,
      Des
   }
   
   public Transform BG;
   public Transform EditContent;
   public Text NpcName;
   public Text GenderText;
   public Text AgeText;
   public Text DesText;
   public LoadingButton ReturnBtn;
   [Header("动画")]
   public TabView AnimTabView;
   public TabView EmoContentView;
   private List<TabItem> allItems;
   private CButton[] editBtns;
   public Transform CharacterRoot;
   [SerializeField] 
   private AvatarCameraController cameraController;
   private CharacterWrap characterWrap;
   private AINpcInfo npcInfo;
   private PgcNpcIdleBehaviour pgcIdleBehaviour;
   private UgcNpcIdleBehaviour ugcIdleBehaviour;
   private AINpcAnimType curNpcType = AINpcAnimType.Idle;

   private Dictionary<NpcInfoEnum, string> editDic = new()
   {
      {NpcInfoEnum.Nick,"编辑昵称"},
      {NpcInfoEnum.Gender,"编辑性别"},
      {NpcInfoEnum.Age,"编辑年龄"},
      {NpcInfoEnum.Image,"编辑形象"},
      {NpcInfoEnum.Anim,"编辑动画"},
      {NpcInfoEnum.Catchphrases,"编辑口头禅"},
      {NpcInfoEnum.Des,"编辑人物简介"}
   };

   public override void OnCreate()
   {
      base.OnCreate();
      ReturnBtn.onClick.AddListener(OnReturnClick);
      InitEditPanel();
      CreateEmoContent();
   }

   public void OnReturnClick()
   {
      CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
      commonConfirmPanel.SetLocalText("确认保存","保存当前的创作进度吗？", "保存", "不保存");
      commonConfirmPanel.SetIsCloseSelf(false);
      commonConfirmPanel.SetOnClickAction(() =>
      {
         commonConfirmPanel.SetConfirmLoadingVisible(true);
         SaveNpcInfo();
      }, () =>
      {
         if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
         {
            commonConfirmPanel.Close();
         }
         CloseSelf();
      });
      
   }

   public override void OnShow(params object[] args)
   {
      base.OnShow(args);
      npcInfo = args[0] as AINpcInfo;
      InitBG();
      ShowCharacter();
      SetDescription(npcInfo.npcDesc);
      SetNpcName(npcInfo.npcName);
      SetGender(npcInfo.npcGender);
      SetAge(npcInfo.npcAge);
      ChangeAnim();
      CreateAnimTabs();
      SetNpcEmoType(curNpcType);
   }

   private void CreateAnimTabs()
   {
      for (var i = 0; i < GameConsts.AnimTabs.Length; i++)
      {
         int index = i;
         var item = AnimTabView.CreateItem(i.ToString(),GameConsts.AnimTabs[i]);
         item.RemoveAllListener();
         item.AddValueChangeCallListener(isOn =>
         {
            if (isOn)
            {
               AINpcAnimType npcType = (AINpcAnimType) index;
               SetNpcEmoType(npcType);
            }
         });
      }
   }

   private void CreateEmoContent()
   {
      allItems = new List<TabItem>();
      for (int i = 0; i < 5; i++)
      {
         var item = EmoContentView.CreateItem(i.ToString());
         item.Init();
         allItems.Add(item);
      }
   }

   private void SetNpcEmoType(AINpcAnimType npcType)
   {
      curNpcType = npcType;
      allItems.ForEach(x =>
      {
         x.RemoveAllListener();
         x.SetIsSelectWithoutCallback(false);
         x.gameObject.SetActive(false);
      });

      if (npcInfo.animResType == (int) AnimResType.PGC)
      {
         var data = npcInfo.npcAnimations?.Find(x => x.npcAnimationType == (int) npcType);
         if (data == null || data.pgcIdleList == null || data.pgcIdleList.Count == 0)
         {
            return;
         }

         for (var i = 0; i < data.pgcIdleList.Count; i++)
         {
            string id = data.pgcIdleList[i];
            var uiConfig = Es.DataTables.GetEmoUIConfig(id);
            string aniName = string.Empty;
            if (id.Equals("leisure"))
            {
               aniName = LocalizationManager.Inst.GetLocalizedText("默认");
            }
            else if (id.Equals("default"))
            {
               aniName = LocalizationManager.Inst.GetLocalizedText("站立");
            }
            else
            {
               aniName = uiConfig == null ? LocalizationManager.Inst.GetLocalizedText("默认"):uiConfig.name;
            }
            allItems[i].gameObject.SetActive(true);
            allItems[i].SetShowName(aniName);
            allItems[i].AddValueChangeCallListener(isOn =>
            {
               if (isOn)
               {
                  Action<string> playAnim = curNpcType == AINpcAnimType.Idle
                     ? pgcIdleBehaviour.PlayMainAnimByUI
                     : pgcIdleBehaviour.PlaySubAnimByUI;
                  playAnim?.Invoke(id);
               }
            });
         }
      }
      else
      {
         var data = npcInfo.npcAnimations?.Find(x => x.npcAnimationType == (int) npcType);
         if (data == null || data.ugcIdleList == null || data.ugcIdleList.Count == 0)
         {
            return;
         }

         for (var i = 0; i < data.ugcIdleList.Count; i++)
         {
            var ugcData = data.ugcIdleList[i];
            allItems[i].gameObject.SetActive(true);
            allItems[i].SetShowName(ugcData.aniName);
            allItems[i].AddValueChangeCallListener(isOn =>
            {
               if (isOn)
               {
                  Action<NpcUgcIdleData> playAnim = curNpcType == AINpcAnimType.Idle
                     ? ugcIdleBehaviour.PlayMainAnimByUI
                     : ugcIdleBehaviour.PlaySubAnimByUI;
                  playAnim?.Invoke(ugcData);
               }
            });
         }
      }
   }


   private void SetNpcName(string npcName)
   {
      npcInfo.npcName = npcName;
      NpcName.text = npcName;
   }

   private void SetGender(int gender)
   {
      npcInfo.npcGender = gender;
      GenderText.SetText("性别：" + (gender == 1 ? "男" : "女"));
   }

   private void SetCatchword(List<string> words)
   {
      npcInfo.npcPetPhrases = words;
   }

   private void SetAge(int age)
   {
      npcInfo.npcAge = age;
      if (age < 0)
      {
         AgeText.SetText("不详");
      }
      else
      {
         AgeText.text = age.ToString();
      }
   }

   
   private void SetDescription(string des)
   {
      DesText.text = des;
      npcInfo.npcDesc = des;
      
      if (string.IsNullOrEmpty(npcInfo.npcDesc))
      {
         DesText.text = "在人物简介中，你可以详细描述NPC的人物背景、性格、身份和出处。信息越丰富，对话效果就会越好！";
      }
   }
   
   private void InitBG()
   {
      string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
      var itemObj = Loader
         .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
         .Instantiate(BG);
      var item = itemObj.GetComponent<ActivityCenterBgItem>();
      item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
      {
         "ainpc_icon1", "ainpc_icon2", "ainpc_icon3","ainpc_icon4"
      });
      item.SetImagesColor(new Color(1,1,1,0.2f));
      item.SetBgImageVisible(false);
      item.gameObject.SetActive(true);
   }

   private void InitEditPanel()
   {
      editBtns = EditContent.GetComponentsInChildren<CButton>(true);
      for (var i = 0; i < editBtns.Length; i++)
      {
         NpcInfoEnum editType = (NpcInfoEnum) i;
         var editBtn = editBtns[i];
         editBtn.GetComponentInChildren<Text>().text = editDic[editType];
         editBtn.onClick.AddListener(() =>
         {
            EditNpcInfo(editType);
         });
      }
   }
   
   private void ShowCharacter()
   {
      var chaData = CharacterData.DeserializeObject(npcInfo.npcAvatarJson);
      if (chaData != null)
      {
         characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(chaData,CharacterRoot);
         cameraController.RotateTarget = CharacterRoot;
         cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
         
         var animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
         pgcIdleBehaviour = characterWrap.Avatar.AddComponent<PgcNpcIdleBehaviour>();
         pgcIdleBehaviour.Init(animationCtrl, true);
         
         ugcIdleBehaviour = characterWrap.Avatar.AddComponent<UgcNpcIdleBehaviour>();
         var playerIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
         ugcIdleBehaviour.Init(playerIkController);
         
      }
   }
   
   private void ChangeOtherOc(BaseAvatarData baseAvatarData)
   {
      if (baseAvatarData != null)
      {
         try
         {
            var data = (CharacterData)baseAvatarData;
            characterWrap.RefreshAvatar(data);
            npcInfo.npcAvatarJson = CharacterData.SerializeObject(data);
         }
         catch { }
      }
   }

   private void ChangeAnim()
   {
      bool isPgcRes = npcInfo.animResType == (int) AnimResType.PGC;
      var ikController = characterWrap.Avatar.GetComponent<AnimIKController>();
      ikController.ChangeAnimResType(isPgcRes ? AnimResType.PGC : AnimResType.UGC);
      ikController.RemovePropIks();
      if (isPgcRes)
      {
         pgcIdleBehaviour.SetData(npcInfo.npcAnimations);
         pgcIdleBehaviour.PlayMainAnim();
      }
      else
      {  ugcIdleBehaviour.SetData(npcInfo.npcAnimations);
         ugcIdleBehaviour.PlayMainAnim();
      }
      SetNpcEmoType(curNpcType);
   }

   private void EditNpcInfo(NpcInfoEnum editType)
   {
      switch (editType)
      {
         case NpcInfoEnum.Nick:
            var namePanel = UIManager.Inst.OpenPanel<ChangeNamePanel>(PanelId.ChangeNamePanel,npcInfo.npcName);
            namePanel.OnComplete = SetNpcName;
            break;
         case NpcInfoEnum.Gender:
            var genderPanel =  UIManager.Inst.OpenPanel<ChangeGenderPanel>(PanelId.ChangeGenderPanel,npcInfo.npcGender);
            genderPanel.OnComplete = SetGender;
            break;
         case NpcInfoEnum.Age:
            var agePanel =  UIManager.Inst.OpenPanel<ChangeAgePanel>(PanelId.ChangeAgePanel,npcInfo.npcAge);
            agePanel.OnComplete = SetAge;
            break;
         case NpcInfoEnum.Catchphrases:
            var catchPanel = UIManager.Inst.OpenPanel<EditCatchphrasesPanel>(PanelId.EditCatchphrasesPanel,npcInfo.npcPetPhrases);
            catchPanel.OnComplete = SetCatchword;
            break;
         case NpcInfoEnum.Image:
            var ocPanel =  UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.EditNpc);
            ocPanel.SetAvatarChangeOc();
            ocPanel.OnChooseAvatarAction = ChangeOtherOc;

            break;
         case NpcInfoEnum.Anim:
            var idlePanel =  UIManager.Inst.OpenPanel<AINpcIdlePanel>(PanelId.AINpcIdlePanel,npcInfo);
            idlePanel.OnComplete = ChangeAnim;
            break;
         case NpcInfoEnum.Des:
            var desPanel =  UIManager.Inst.OpenPanel<EditAvatarDescPanel>(PanelId.EditAvatarDescPanel,npcInfo.npcDesc);
            desPanel.OnComplete = SetDescription;
            break;
      }
   }

   #region 截屏相关
   private Camera photoCamera;
   private void SaveNpcInfo()
   {
      if (photoCamera == null)
      {
         photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab").Instantiate(characterWrap.Avatar.transform).GetComponent<Camera>();
         photoCamera.transform.localPosition = new Vector3(0, 0.5f, 1);
         photoCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
         photoCamera.orthographicSize = photoCamera.orthographicSize * ResolutionAutoFit.CameraScale * characterWrap.Avatar.transform.localScale.x;
      }
      
      if(characterWrap == null)
         return;
      
      CharacterPartData partData = null;
      AvatarCommonData bData = null;
      pgcIdleBehaviour.PlayMainAnimByUI("leisure");
      
      partData = characterWrap.GetPartData(UniqueType.GetAvatar(AvatarSubType.Eyes));
      
      if (partData == null || partData.IsNull())
      {
         StartCoroutine("TakeMatchPhoto");
         return;
      }

      bData = DataTables.GetAvatarCommonData(partData.Id);
      
      Loader.LoadAsyncOrSync<AnimationClip>(bData.aniPath + ".anim", (isSuc, wrapper) =>
      {
         if (isSuc && wrapper != null)
         {
            StartCoroutine("TakeMatchPhoto");
         }
      });
   }

   private IEnumerator TakeMatchPhoto()
   {
      yield return new WaitForEndOfFrame();
      try
      {
         Rect rect = GetScreenShotRect();
         byte[] imgBytes = ScreenShotUtils.TakeShotGamma(photoCamera, rect);
         // Destroy(photoCamera.gameObject);
         string userId = AccountDataManager.Inst.Uid;
         userId = string.IsNullOrEmpty(userId) ? "shotTemplate" : userId;
         string fileName = LocalDataUtils.Inst.SaveTempImgRes(userId,imgBytes);

         var uri = $"AINpc/characterInfo/{AccountDataManager.Inst.Uid}/{System.IO.Path.GetFileName(fileName)}";
         CommonConfirmPanel commonConfirmPanel = UIManager.Inst.FindPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
         CosXmlUploadManager.UploadFile(uri, fileName, (uploadedUrl, err) =>
         {
            
            File.Delete(fileName);
            
            npcInfo.cover = uploadedUrl;
            SetAINpcInfoReq ugcNpcInfoReq = new SetAINpcInfoReq()
            {
               npc = npcInfo,
               setType = (int)SetType.Edit
            };
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcSet, HttpMethod.POST,
               JsonConvert.SerializeObject(ugcNpcInfoReq), (content) =>
               {
                  if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                  {
                     commonConfirmPanel.Close();
                  }
               
                  MessageHelper.Broadcast(MessageName.OnAINpcStudioDraftListChange);
                  if (this != null)
                  {
                     CloseSelf();
                  }
               }, (error) =>
               {
                  if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                  {
                     commonConfirmPanel.SetConfirmLoadingVisible(false);
                  }
               });
         });
      }
      catch (Exception e)
      {
         CommonConfirmPanel commonConfirmPanel = UIManager.Inst.FindPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);

         if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
         {
            commonConfirmPanel.SetConfirmLoadingVisible(false);
         }
         LoggerUtils.LogError(e.Message);
      }
   }

   private Rect GetScreenShotRect()
   {
      Rect rect = new Rect(0, 0, photoCamera.pixelWidth, photoCamera.pixelHeight);
      return rect;
   }
   #endregion

}
