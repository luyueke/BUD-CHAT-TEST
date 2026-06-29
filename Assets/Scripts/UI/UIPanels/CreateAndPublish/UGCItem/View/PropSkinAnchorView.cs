using GameData.BaseInfo;
using UI.Manager;
using UnityEngine;

namespace UI {
    public class PropSkinAnchorView : BaseAnchorView {
        protected override void SetPropAndGizmo() {
            base.SetPropAndGizmo();

            if (editData.GetInfo() is SkinInfo skinInfo) {
                skinInfo.skinDetailInfo ??= new SkinDetailInfo();
                // 若锚点超过 素材编辑器的一半，则重置锚点
                var maxAxisValue = Mathf.Max(skinInfo.skinDetailInfo.anchor.x, skinInfo.skinDetailInfo.anchor.y,
                    skinInfo.skinDetailInfo.anchor.z);
                if (maxAxisValue > 20) {
                    anchorTarget.transform.localPosition = Vector3.zero;
                } else {
                    anchorTarget.transform.localPosition = skinInfo.skinDetailInfo.anchor;
                }
            } else {
                anchorTarget.transform.localPosition = Vector3.zero;
            }
        }

        protected override void OnNextBtnClick() {
            if (editData.GetInfo() is SkinInfo skinInfo) {
                skinInfo.skinDetailInfo.anchor = anchorTarget.transform.localPosition;
            }
            nextCallback?.Invoke();
        }
    }
}
