using System;
using AIGame.Base;
using DG.Tweening;
using Game.Utils;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIYandereGuidePanel : BasePanel<AIYandereGuidePanel>
{
   public GameObject[] ViewNode;
   public string[] chatContents;
   public GameObject View1;
   public GameObject View2;
   
   public Button IncomingCallBtn;
   public Button EndCall;
   public GameObject EndDialog;

   public GameRemoteImageBehaviour LeftImage;
   public GameRemoteImageBehaviour RightImage;

   public GameObject LeftDialog;
   public Text LeftContent;
   public GameObject RightDialog;
   public Text RightContent;

   public Button NextBtn;
   
   public Button ExitStep;
   
   public Text NextTip;

   [Header("Ugc引导")] public Text ugcGuideText;
   public Text ugcGuideText1;
   public GameObject ugcNextNode;
   public Button ugcNextBtn;
   
   public Action EnterGame;

   private GameObject NextNode;
   private GameObject LeftHead;
   private GameObject RightHead;
   
   private string AIYandereGuideTag = "AIYandere_Guide_Tag_New";

   private enum GuideEnum
   {
      Step1 = 0,
      Step2,
      Step3,
      Step4,
      Step5
   }

   private GuideEnum curStep = GuideEnum.Step1;
   private GuideEnum curUgcStep = GuideEnum.Step1;
   private string ugcContent ="当你一觉醒来，发现自己和{0}一同被困在一栋公寓里，每当你想离开公寓时总会遭到他的阻拦。";
   private string ugcContent1 ="努力说服{0}打开公寓大门并让你离开这栋公寓...";
   public override void OnCreate()
   {
      base.OnCreate();
      NextTip.SetText("点击任意地方继续");
      NextNode = NextTip.transform.parent.gameObject;
      LeftHead = LeftImage.transform.parent.gameObject;
      RightHead = RightImage.transform.parent.gameObject;
      IncomingCallBtn.onClick.AddListener(OnIncomingCallClick);
      NextBtn.onClick.AddListener(OnNextClick);
      EndCall.onClick.AddListener(OnCloseClick);
      ExitStep.onClick.AddListener(OnCloseClick);
      ugcNextBtn.onClick.AddListener(OnUgcNextClick);
      RightImage.Load(AccountDataManager.Inst.UserInfo.portraitUrl);
      SetExitVisible();
   }

   public override void OnShow(params object[] args)
   {
      base.OnShow(args);
      if(args != null && args.Length > 1)
      {
         bool isPgc = (bool)args[0];
         string npcName = (string)args[1];
         ViewNode[0].SetActive(isPgc);
         ViewNode[1].SetActive(!isPgc);
         ugcContent = string.Format(ugcContent,npcName);
         ugcContent1 = string.Format(ugcContent1,npcName);
         Action step = isPgc ? ChangeStep : OnUgcChangeStep;
         step.Invoke();
      }
   }

   private void SetExitVisible()
   {
      if (PlayerPrefs.HasKey(AIYandereGuideTag) && PlayerPrefs.GetInt(AIYandereGuideTag) == 1)
      {
         ExitStep.gameObject.SetActive(true);
      }
      else
      {
         PlayerPrefs.SetInt(AIYandereGuideTag, 1);
         ExitStep.gameObject.SetActive(false);
      }
   }


   private void OnIncomingCallClick()
   {
      curStep = GuideEnum.Step2;
      ChangeStep();
   }

   private void OnNextClick()
   {
      curStep++;
      ChangeStep();
   }

   private void OnCloseClick()
   {
      AIGameSoundUtils.Inst.StopAllSound();
      EnterGame?.Invoke();
      CloseSelf();
   }

   private void ChangeStep()
   {
      switch (curStep)
      {
         case GuideEnum.Step1:
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_CALL);
            View1.gameObject.SetActive(true);
            View2.gameObject.SetActive(false);
            break;
         case GuideEnum.Step2:
            AIGameSoundUtils.Inst.StopSound(YandereConfig.SOUND_CALL);
            LeftHead.SetActive(true);
            RightHead.SetActive(true);
            View1.gameObject.SetActive(false);
            View2.gameObject.SetActive(true);
            LeftDialog.SetActive(true);
            RightDialog.SetActive(false);
            string userName = AccountDataManager.Inst.UserInfo.nickname;
            PlayTextAnim(LeftContent,userName + chatContents[0]);
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Chat_Vocal + "1");
            break;
         case GuideEnum.Step3:
            LeftDialog.SetActive(false);
            RightDialog.SetActive(true);
            PlayTextAnim(RightContent,chatContents[1]);
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Chat_Vocal + "1");
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Chat_Vocal + "2");
            break;
         case GuideEnum.Step4:
            LeftDialog.SetActive(true);
            RightDialog.SetActive(false);
            PlayTextAnim(LeftContent,chatContents[2]);
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Chat_Vocal + "2");
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Chat_Vocal + "3");
            break;
         case GuideEnum.Step5:
            LeftDialog.SetActive(false);
            RightDialog.SetActive(true);
            NextBtn.gameObject.SetActive(false);
            PlayTextAnim(RightContent,chatContents[3]);
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Chat_Vocal + "3");
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Chat_Vocal + "4");
            break;
      }
   }

   private void PlayTextAnim(Text curText, string content)
   {
      NextNode.SetActive(false);
      curText.text = string.Empty;
      NextBtn.gameObject.SetActive(false);
      curText.DOText(content, 2).OnComplete(() =>
      {
         NextNode.SetActive(true);
         NextBtn.gameObject.SetActive(true);
         if (curStep == GuideEnum.Step5)
         {
            NextNode.SetActive(false);
            EndCall.gameObject.SetActive(true);
            EndDialog.gameObject.SetActive(true);
            NextBtn.gameObject.SetActive(false);
         }
      });
   }

   private void OnUgcNextClick()
   {
      curUgcStep++;
      OnUgcChangeStep();
   }

   private void OnUgcChangeStep()
   {
      switch (curUgcStep)
      {
         case GuideEnum.Step1:
            ugcGuideText.text = "";
            ugcGuideText1.text = "";
            ugcGuideText.DOText(ugcContent, ugcContent.Length * 0.1f).SetEase(Ease.Linear).OnComplete(() =>
            {
               ugcGuideText1.DOText(ugcContent1, ugcContent1.Length * 0.1f).SetEase(Ease.Linear).OnComplete(() =>
               {
                  curUgcStep++;
                  ugcNextNode.SetActive(true);
                  AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
               });
            });
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Typing_Loop);
            View1.gameObject.SetActive(true);
            View2.gameObject.SetActive(false);
            break;
         case GuideEnum.Step2:
            ugcGuideText.DOKill();
            ugcGuideText.text = ugcContent;
            ugcGuideText1.text = ugcContent1;
            ugcNextNode.SetActive(true);
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
            break;
         default:
            OnCloseClick();
            break;
      }
   }

}
