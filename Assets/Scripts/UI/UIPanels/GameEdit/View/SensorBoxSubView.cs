using System;
using UI.UIPanels.GameEdit;
using UnityEngine.UI;

public class SensorBoxSubView :BasePropertyEditSubView
{
   public Toggle OnceToggle;
   public Toggle UnlimitedToggle;
   
   private Action<int> SetBoxTimesCallback;
   protected override void OnInit()
   {
      OnceToggle.onValueChanged.AddListener(OnOnceSelect);
      UnlimitedToggle.onValueChanged.AddListener(OnUnlimitedSelect);
   }

   public void SetDefaultBoxTimes(int boxTimes)
   {
      if (boxTimes == 1)
      {
         OnceToggle.SetIsOnWithoutNotify(true);
      }
      else
      {
         UnlimitedToggle.SetIsOnWithoutNotify(true);
      }
   }

   public void AddBoxTimesChangeListener(Action<int> callBack)
   {
      SetBoxTimesCallback += callBack;
   }

   public void RemoveBoxTimesChangeListener(Action<int> callBack)
   {
      SetBoxTimesCallback -= callBack;
   }

   public void ClearBoxTimesChangeListener()
   {
      SetBoxTimesCallback = null;
   }

   private void OnOnceSelect(bool isOn)
   {
      if (isOn)
      {
         SetBoxTimesCallback?.Invoke(1);
      }
   }

   private void OnUnlimitedSelect(bool isOn)
   {
      if (isOn)
      {
         SetBoxTimesCallback?.Invoke(-1);
      }
   }

   private void OnDestroy()
   {
      ClearBoxTimesChangeListener();
   }
}
