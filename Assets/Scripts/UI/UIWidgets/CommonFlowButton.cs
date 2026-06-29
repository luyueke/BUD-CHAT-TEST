// @Author: YangJie
// @Description:
// @Date:  2023/08/31
// @Modify:

using DG.Tweening;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    public class CommonFlowButton : CButton
    {

        public enum FlowStatus
        {
            MutualFlow,
            Self,
            Flowed,
            Flowing,
            UnFlow,
        }
        
        
        
        private string targetUserId;
        private FlowStatus flowStatus = FlowStatus.UnFlow;
        
        [SerializeField]
        public Sprite hollowSp;
        
        [SerializeField]
        public Sprite solidSp;

        [SerializeField] public Image btnImage;
        [SerializeField] public Image flowingImage;
        
        
        protected override void Awake()
        {
            base.Awake();
            onClick.AddListener(OnFlowButtonClicked);
            SetStatus(FlowStatus.UnFlow);
        }
        
        
        
        private void OnFlowButtonClicked()
        {
            if (flowStatus != FlowStatus.UnFlow)
            {
                return;
            }
            //TODO: 调用关注API
            SetStatus(FlowStatus.Flowing);
        }

        public void SetStatus(FlowStatus status)
        {
            flowStatus = status;
            switch (status)
            {
                case FlowStatus.MutualFlow:
                    interactable = false;
                    flowingImage.gameObject.SetActive(false);
                    SetText("互关");
                    break;
                case FlowStatus.Self:
                    interactable = false;
                    flowingImage.gameObject.SetActive(false);
                    SetText("我");
                    break;
                case FlowStatus.Flowed:
                    interactable = false;
                    flowingImage.gameObject.SetActive(false);
                    SetText("已关注");
                    break;
                case FlowStatus.Flowing:
                    interactable = true;
                    flowingImage.transform.DOKill();
                    flowingImage.transform.DOLocalRotate(new Vector3(0, 0, -720), 2f).SetLoops(-1);
                    flowingImage.gameObject.SetActive(true);
                    SetText("");
                    break;
                case FlowStatus.UnFlow:
                    interactable = true;
                    flowingImage.gameObject.SetActive(false);
                    SetText("关注");
                    break;
            }
        }
        
        
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state,instant);
            if (!gameObject.activeInHierarchy)
                return;
            if (btnImage)
            {
                btnImage.sprite = interactable ? solidSp : hollowSp;
            }
            
        }
        
    }
}