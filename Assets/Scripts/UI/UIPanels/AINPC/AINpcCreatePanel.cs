using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIWidgets;
using UnityEngine;

public class AINpcCreatePanel : BasePanel<AINpcCreatePanel>
{
   public Transform BG;
   public CButton BackBtn;
   public CButton ChangeOCBtn;
   public LoadingButton NextBtn;
   public TextInputView eitNameBox;
   public TextInputView descEditBox;
   public Transform CharacterRoot;
   private Camera photoCamera;
   private string avatarJson;
   [SerializeField] private AvatarCameraController cameraController;
   private CharacterWrap characterWrap;
   public override void OnCreate()
   {
      base.OnCreate();
      BackBtn.onClick.AddListener(OnBackClick);
      NextBtn.onClick.AddListener(OnNextClick);
      ChangeOCBtn.onClick.AddListener(OnChangeOcClick);
   }

   public override void OnShow(params object[] args)
   {
      base.OnShow(args);
      InitBG();
      avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
      ShowCharacter();
      DateTime currentDate = DateTime.Now;
      string formattedDate = currentDate.ToString("yyyy-MM-dd");
      eitNameBox.SetInputWithoutNotify("Npc-" + formattedDate);
   }

   private void InitBG()
   {
      if (BG == null)
      {
         return;
      }

      string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
      var itemObj = Loader
         .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
         .Instantiate(BG);
      var item = itemObj.GetComponent<ActivityCenterBgItem>();
      item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
      {
         "avatar_icon_1", "avatar_icon_2", "avatar_icon_3","avatar_icon_4"
      });
      item.gameObject.SetActive(true);
   }
   
   private void OnBackClick()
   {
      CloseSelf();
   }

   private void OnChangeOcClick()
   {
      var ocPanel = UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.EditNpc);
      ocPanel.SetAvatarChangeOc();
      ocPanel.OnChooseAvatarAction = ChangeOtherOc;
   }

   private void ChangeOtherOc(BaseAvatarData baseAvatarData)
   {
      if (baseAvatarData != null)
      {
         try
         {
            var data = (CharacterData)baseAvatarData;
            avatarJson = JsonConvert.SerializeObject(data);
            characterWrap.SetCharacterData(data);
         }
         catch { }
      }
   }
   
   public void ShowCharacter()
   {
      var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
      if (saveCharacterData != null)
      {
         characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
         characterWrap.SetParent(CharacterRoot, true);
         cameraController.RotateTarget = CharacterRoot;
         cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
      }
   }

   private void OnNextClick()
   {
      if (string.IsNullOrEmpty(eitNameBox.Input))
      {
         TipPanel.ShowToast("输入不能为空");
         return;
      }

      TimerManager.Inst.RunOnce("CreateNpc", 5, () =>
      {
         if(this == null)
            return;
         
         NextBtn.HideLoading();
      });
      
      NextBtn.ShowLoading();
      SaveNpcInfo();
   }

   private void SaveNpcInfo()
   {
      photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab")
         .Instantiate(characterWrap.Avatar.transform).GetComponent<Camera>();
      photoCamera.transform.localPosition = new Vector3(0, 0.5f, 1);
      photoCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
      photoCamera.orthographicSize = photoCamera.orthographicSize * ResolutionAutoFit.CameraScale *
                                     characterWrap.Avatar.transform.localScale.x;
      StartCoroutine("TakeMatchPhoto");
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
         Debug.LogError(fileName);
         var uri = $"AINpc/characterInfo/{AccountDataManager.Inst.Uid}/{System.IO.Path.GetFileName(fileName)}";
         CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
         {
            AINpcInfo info = new()
            {
               npcName = eitNameBox.Input,
               npcDesc = descEditBox.Input,
               npcAvatarJson = avatarJson,
               cover = url,
               npcAge = -1
            };
            CreateNpcByServer(info);
         });
      }
      catch (Exception e)
      {
         NextBtn.HideLoading();
         LoggerUtils.LogError(e.Message);
      }
   }

   private void CreateNpcByServer(AINpcInfo npcInfo)
   {
      SetAINpcInfoReq req = new SetAINpcInfoReq();
      req = new SetAINpcInfoReq
      {
         npc = npcInfo,
         setType = (int)SetType.Create
      };
      NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcSet, HttpMethod.POST, JsonConvert.SerializeObject(req), OnCreateSuccess, OnCreateFail);
   }

   private void OnCreateSuccess(string content)
   {
      NextBtn.HideLoading();
      if (string.IsNullOrEmpty(content))
      {
         LoggerUtils.LogError("Npc OnCreateSuccess content is null");
         TipPanel.ShowToast("保存失败，请再试一次!");
         return;
      }
       var rep = JsonConvert.DeserializeObject<SetAINpcInfoRep>(content);
       if (rep != null && rep.npc != null)
       {
          UIManager.Inst.OpenPanel<AINpcEditPanel>(PanelId.AINpcEditPanel,rep.npc);
          CloseSelf();
       }
       else
       {
          LoggerUtils.LogError($"Npc OnCreateSuccess rep or rep.npc is null {(rep != null).ToString()}");
          TipPanel.ShowToast("保存失败，请再试一次!");
       }
   }

   private void OnCreateFail(string err)
   {
      NextBtn.HideLoading();
      LoggerUtils.LogError($"Npc OnCreateFail");
      TipPanel.ShowToast("保存失败，请再试一次!");
   }

   private Rect GetScreenShotRect()
   {
      Rect rect = new Rect(0, 0, photoCamera.pixelWidth, photoCamera.pixelHeight);
      return rect;
   }
}
