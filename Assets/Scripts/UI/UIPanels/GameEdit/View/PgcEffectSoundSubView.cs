using System;
using UI.UIPanels.GameEdit;
using UnityEngine;
using UnityEngine.UI;

public class PgcEffectSoundSubView :BasePropertyEditSubView
{
   public Toggle SoundToggle;
   private Action<int> soundChangeCallback;
   protected override void OnInit()
   {
      SoundToggle.onValueChanged.AddListener(OnSoundChange);
   }

   public void SetSoundWithoutNotify(int playSound)
   {
      SoundToggle.SetIsOnWithoutNotify(playSound == 1);
   }

   public void SetToggleEnable(bool enableValue)
   {
      if (enableValue == false)
      {
         SoundToggle.SetIsOnWithoutNotify(false);
      }
      SoundToggle.interactable = enableValue;
   }

   public void AddSoundChangeListener(Action<int> callBack)
   {
      soundChangeCallback += callBack;
   }

   public void RemoveSoundChangeListener(Action<int> callBack)
   {
      soundChangeCallback -= callBack;
   }

   public void ClearSoundChangeListener()
   {
      soundChangeCallback = null;
   }

   private void OnSoundChange(bool isOn)
   {
      int playSound = isOn ? 1 : 0;
      soundChangeCallback?.Invoke(playSound);
   }

   
   private void OnDestroy()
   {
      ClearSoundChangeListener();
   }
}
