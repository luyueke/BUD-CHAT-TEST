using UI.Base;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    public class IncubationCabinWifiPopPanel : BasePanel<IncubationCabinWifiPopPanel>
    {

        [SerializeField] private TextInputView wifiNameInputView;
        [SerializeField] private TextInputView passwordInputView;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;





        public override void OnShow(params object[] args)
        {
            InitBtns();
        }

     
        void InitBtns()
        {
            // setDefaultBtn.onClick.AddListener(OnSetDefaultBtnClick);
            btnConfirm.onClick.AddListener(OnConfirmBtnClick);
            btnCancel.onClick.AddListener(OnCancelBtnClick);
            
        }

        void OnConfirmBtnClick()
        {
            string wifiName = wifiNameInputView.Input;
            string password = passwordInputView.Input;
            if (string.IsNullOrEmpty(wifiName) || string.IsNullOrEmpty(password))
            {
                TipPanel.ShowToast("请输入wifi名称和密码");
                return;
            }
            // CabinNetManager.Inst.SetWifiInfo(wifiName, password);
            CloseSelf();
        }

        void OnCancelBtnClick()
        {
            CloseSelf();
        }


    }


}
