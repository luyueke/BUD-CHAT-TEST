using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    public class IncubationCabinSettingPanel : BasePanel<IncubationCabinSettingPanel>
    {

        [SerializeField] private Button backBtn;
        [SerializeField] private Button btnLastStep;
        [SerializeField] private Button btnRefreshWifi;
        [SerializeField] private Button btnCompleteSetting;





        public override void OnShow(params object[] args)
        {
            InitBtns();
        }

     
        void InitBtns()
        {
            // setDefaultBtn.onClick.AddListener(OnSetDefaultBtnClick);
            backBtn.onClick.AddListener(OnBackBtnClick);
            btnLastStep.onClick.AddListener(OnLastStepBtnClick);
            btnRefreshWifi.onClick.AddListener(OnRefreshWifiBtnClick);
            btnCompleteSetting.onClick.AddListener(OnCompleteSettingBtnClick);
            
        }

        void OnBackBtnClick()
        {
            CloseSelf();
        }

        void OnLastStepBtnClick()
        {
        }

        void OnRefreshWifiBtnClick()
        {
        }

        void OnCompleteSettingBtnClick()
        {
        }
    }


}
