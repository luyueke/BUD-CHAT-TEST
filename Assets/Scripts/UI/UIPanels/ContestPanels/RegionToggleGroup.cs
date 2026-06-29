using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RegionToggleGroup : MonoBehaviour
{
   public Toggle regionToggle;
   public Toggle allToggle;
   public Text regionText;
   public Text allText;
   public Action<bool> _regionAct;
   private void Awake()
   {
      regionToggle.onValueChanged.AddListener((isOn) =>
      {
         SetTextColor(regionText, isOn);
         _regionAct?.Invoke(isOn);
      });
      allToggle.onValueChanged.AddListener((isOn) =>
      {
         SetTextColor(allText, isOn);
      });
   }
   
   public void Init(Action<bool> regionAction)
   {
      _regionAct = regionAction;
   }

   public void SetTextColor(Text text,bool isOn)
   {
      text.color = isOn ? Color.black : new Color(1,1,1,0.8f);
   }
}
