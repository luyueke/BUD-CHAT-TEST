using System;
using System.Collections;
using Basic;
using Game;
using Game.Base;
using Game.Config;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.UGCData;
using RTG;
using UI.Manager;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI {
    public class PropAnchorView : BaseAnchorView {


        protected override void SetPropAndGizmo() {
            base.SetPropAndGizmo();
            if (editData.GetInfo() is PropInfo propInfo) {
                propInfo.detailInfo ??= new DetailInfo();
                // 若锚点超过 素材编辑器的一半，则重置锚点
                var maxAxisValue = Mathf.Max(propInfo.detailInfo.anchor.x, propInfo.detailInfo.anchor.y,
                    propInfo.detailInfo.anchor.z);
                if (maxAxisValue > 20) {
                    anchorTarget.transform.localPosition = Vector3.zero;
                } else {
                    anchorTarget.transform.localPosition = propInfo.detailInfo.anchor;
                }
            } else {
                anchorTarget.transform.localPosition = Vector3.zero;
            }
        }



        protected override void OnNextBtnClick() {
            if (editData.GetInfo() is PropInfo propInfo) {
                propInfo.detailInfo.anchor = anchorTarget.transform.localPosition;
            }
            base.OnNextBtnClick();
        }



    }
}
