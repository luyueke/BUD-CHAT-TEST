
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    public class IncubationCabinAddPanel : BasePanel<IncubationCabinAddPanel>
    {

        [SerializeField] private Button backBtn;

        [SerializeField] private GameObject node1Go;
        [SerializeField] private GameObject node2Go;
        [SerializeField] private GameObject node3Go;
    

        [SerializeField] private Button node1_unlockBtn;
        [SerializeField] private Button node1_addCabinBtn;
        [SerializeField] private Button node2_cameraAddCabinBtn;
        [SerializeField] private Button node3_nextStepBtn;





        public override void OnShow(params object[] args)
        {
            InitBtns();
        }

     
        void InitBtns()
        {
            // setDefaultBtn.onClick.AddListener(OnSetDefaultBtnClick);
            backBtn.onClick.AddListener(OnBackBtnClick);
            node1_unlockBtn.onClick.AddListener(OnNode1UnlockBtnClick);
            node1_addCabinBtn.onClick.AddListener(OnNode1AddCabinBtnClick);
            node2_cameraAddCabinBtn.onClick.AddListener(OnNode2CameraAddCabinBtnClick);
            node3_nextStepBtn.onClick.AddListener(OnNode3NextStepBtnClick);
            
        }

        void OnBackBtnClick()
        {
            CloseSelf();
        }

        void OnNode1UnlockBtnClick()
        {
        }

        void OnNode1AddCabinBtnClick()
        {
        }

        void OnNode2CameraAddCabinBtnClick()
        {
        }

        void OnNode3NextStepBtnClick()
        {
        }
    }


}
