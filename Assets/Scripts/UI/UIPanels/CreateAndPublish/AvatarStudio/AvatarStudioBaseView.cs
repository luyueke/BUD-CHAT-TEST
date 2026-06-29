using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarStudioBaseView : MonoBehaviour
{
   private bool isFirstShow = true;

   protected CharacterStyle currentStyle;
   protected List<AvatarStudioConfig.DraftConfig> topBarConfig = new List<AvatarStudioConfig.DraftConfig>();
   //第一次打开
   protected virtual void Init()
   {
      switch (currentStyle)
      {
         case CharacterStyle.Pet:
            topBarConfig = new List<AvatarStudioConfig.DraftConfig>();
            topBarConfig.AddRange(AvatarStudioConfig.petRtConfig);
            break;
         
         default:
         case CharacterStyle.Avatar:
            topBarConfig = new List<AvatarStudioConfig.DraftConfig>();
            topBarConfig.AddRange(AvatarStudioConfig.avatarRtConfig);
            break;
      }
   }
   protected virtual void OnViewShow()
   {
      
   }
   protected virtual void OnViewHide()
   {
      
   }
   public void Show(CharacterStyle style)
   {
      currentStyle = style;
      if (!IsShow())
      {
         gameObject.SetActive(true);
         if (isFirstShow)
         {
            isFirstShow = false;
            Init();
         }
         OnViewShow();
      }
   }
   
   public void Hide()
   {
      if (IsShow())
      {
         OnViewHide();
         gameObject.SetActive(false);
      } 
   }
   public bool IsShow()
   {
      return gameObject.activeSelf;
   }
}
