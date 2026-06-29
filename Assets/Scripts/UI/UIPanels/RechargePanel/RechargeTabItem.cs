using System;
using Es;
using GameUI;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.RechargePanel {
    public class RechargeTabItem : MonoBehaviour {
        public CButton button;
        public Text title;
        public GameObject redDot;
        public GameObject back;
        [SerializeField] private GameObject selectedObj;

        public RechargeId rechargeId = RechargeId.ErrRechargeId;
        private Action<RechargeId> clickAction;
        private string selectedColor = "#FFD400";
        private string normalColor = "#FFFFFF";


        public void Init(RechargeViewConfig viewConfig, Action<RechargeId> onClick) {
            button.onClick.AddListener(RootClick);
            rechargeId = (RechargeId)viewConfig.ViewId;
            if (viewConfig.ViewId == 20 || viewConfig.ViewId == 21)
            {
                back.gameObject.SetActive(true);
            }
            clickAction = onClick;
            title.SetText(viewConfig.ViewName);
            if (viewConfig.ViewId == 91)
            {
                var v = redDot.AddComponent<RedDotNew>();
                v.customType.Add(ReddotType.SeasonCumulative);
                v.RefreshState();
            }
        }

        private void RootClick() {
            clickAction.Invoke(rechargeId);
        }

        public void SetSelect(bool isSel) {
            var txtColor = isSel ? selectedColor : normalColor;
            title.color = DataUtil.DeSerializeColorCheckHash(txtColor);
            selectedObj.SetActive(isSel);
        }

        public void SetRedDot(bool isRed) {
            if ((int)rechargeId == 91)
            {
                return;
            }
            redDot.SetActive(isRed);
        }

    }
}
