
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ApartmentTipPanel : BasePanel<ApartmentTipPanel>
{
   [SerializeField] private CButton backBtn;
   [SerializeField] private Button blankBtn;
   public override void OnCreate()
   {
      base.OnCreate();
      backBtn.onClick.AddListener(OnBackBtnClick);
      blankBtn.onClick.AddListener(OnBackBtnClick);
   }

   public override void OnShow(params object[] args)
   {
      base.OnShow(args);
  
   }

   private void OnBackBtnClick()
   {
      CloseSelf();
   }

}
