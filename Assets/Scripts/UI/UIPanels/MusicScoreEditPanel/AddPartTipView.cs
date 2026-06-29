using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class AddPartTipView : MonoBehaviour
{
   public CButton closeViewBtn;
   public CButton yesBtn;
  
   
   public void Init()
   {
      closeViewBtn.onClick.AddListener(Close);
      yesBtn.onClick.AddListener(OnYesBtnClick);
    
   }
   public void Show()
   {
      gameObject.SetActive(true);
   }
   public void Close()
   {
      gameObject.SetActive(false);
   }

   public void OnYesBtnClick()
   {
      Close();
   }
}
