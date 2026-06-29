using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.EditOperation;
using UI.EditOperation.Rules;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public enum PoseHandleType
    {
        None,
        Move,
        MoveAndRotation,
        All
    }
    
    public class PoseHandleView : PoseBaseView
    {
        public CoToggle moveToggle;
        public CoToggle rotateToggle;
        public CoToggle scaleToggle;
        public CButton bindToggle;
        public CButton changeToggle;
        public CButton removeToggle;
        public CButton deleteToggle;
        public Action SyncPoseData;
        public override void OnCreate()
        {
            base.OnCreate();
            moveToggle.TargetButton.onClick.AddListener(OnMoveClick);
            rotateToggle.TargetButton.onClick.AddListener(OnRotateClick);
            scaleToggle.TargetButton.onClick.AddListener(OnScaleClick);
                bindToggle.onClick.AddListener(OnBindClick);
            changeToggle.onClick.AddListener(OnChangeClick);
            removeToggle.onClick.AddListener(OnRemoveClick);
            deleteToggle.onClick.AddListener(OnDeleteClick);
        }

        public override void OnShow(EnterPanelMode panelMode,UgcPoseSubType poseType)
        {
            base.OnShow(panelMode,poseType);
            curPoseType = poseType;
            bool isAnimEnter = panelMode == EnterPanelMode.AnimEnter;
            scaleToggle.gameObject.SetActive(isAnimEnter);
            bindToggle.gameObject.SetActive(isAnimEnter);
            deleteToggle.gameObject.SetActive(isAnimEnter);
        }

        public void SwitchHandleType(PoseEditOperation poseEditData)
        {
            moveToggle.gameObject.SetActive(poseEditData.Move == 1);
            rotateToggle.gameObject.SetActive(poseEditData.Rotate == 1);
            scaleToggle.gameObject.SetActive(poseEditData.Scale == 1);
            bindToggle.gameObject.SetActive(poseEditData.Bind == 1);
            deleteToggle.gameObject.SetActive(poseEditData.Delete == 1);
            changeToggle.gameObject.SetActive(poseEditData.Change == 1);
            removeToggle.gameObject.SetActive(poseEditData.Remove == 1);
        }

        public void OnMoveClick()
        {
            GizmoManager.Inst.CurGizmoCtrl?.SetMoveCtr();
            moveToggle.IsOn = true;
        }

        private void OnRotateClick()
        {
            GizmoManager.Inst.CurGizmoCtrl?.SetRotateCtr();
            rotateToggle.IsOn = true;
        }
        
        private void OnScaleClick()
        {
            var target = GizmoManager.Inst.CurGizmoCtrl?.GetCurrentTarget();
            if (target != null)
            {
                var poseEditData = DataTables.GetPoseEditOperation(target.name);
                GizmoManager.Inst.CurGizmoCtrl?.SetScaleCtr(null);
                GizmoManager.Inst.CurGizmoCtrl.scaleGizmo.Gizmo.ObjectTransformGizmo.isSpecialScale = poseEditData.IsSpecialScale;
                scaleToggle.IsOn = true;
            }
        }

        private void OnChangeClick()
        {
            var propPanel = UIManager.Inst.OpenPanel<AnimPropToolPanel>(PanelId.AnimPropToolPanel);
            propPanel.SetSelectPropClick(OnChangeProp);
        }

        private void OnRemoveClick()
        {
            var propTarget = AnimPoseEditPanel.propTarget;
            propTarget.propIk.SetBindIndex(0);
            var propTransform = propTarget.propIk.transform;
            propTransform.localPosition = Vector3.zero;
            propTransform.localEulerAngles = Vector3.zero;
            propTransform.gameObject.SetActive(false);
            GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
            AnimPoseEditPanel.propTarget.curViewItem.RemovePropNode();
        }
        
        private void OnDeleteClick()
        {
            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetText($"删除道具", $"确定要删除该道具吗？动作中所有关键帧使用该道具的地方都会删除哦！",
                "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                OnRemoveClick();
                var propIk = AnimPoseEditPanel.propTarget.propIk;
                GameObject.Destroy(propIk.gameObject);
                AnimPoseEditPanel.Creater.RemovePropIK(propIk);
                AnimPoseEditPanel.propTarget.curViewItem.RemovePropIK();
                SyncPoseData?.Invoke();
                this.gameObject.SetActive(false);
            }, () => { });
        }
        
        private void OnChangeProp(PropInfo info)
        {
            var curPropIK = AnimPoseEditPanel.propTarget.propIk;
            AnimPoseEditPanel.propTarget.curViewItem.ChangePropClick(info,curPropIK);
        }

        private void OnBindClick()
        {
            var curIndex = AnimPoseEditPanel.propTarget.propIk.bindIndex;
            var bindPanel = UIManager.Inst.OpenPanel<PoseBindNodePanel>(PanelId.PoseBindNodePanel,curPoseType,curIndex);
            bindPanel.OnSelect = OnSelect;
        }

        private void OnSelect(int index)
        {
            var propIK = AnimPoseEditPanel.propTarget.propIk;
            if (propIK != null)
            {
                propIK.SetBindIndex(index);
                var propTransform = propIK.transform;
                propTransform.localPosition = Vector3.zero;
                propTransform.localEulerAngles = Vector3.zero;
                AnimPoseEditPanel.propTarget.curViewItem.curPropData.bindIndex = index;
                GizmoManager.Inst.CurGizmoCtrl.RefreshPositionAndRotation();
                SyncPoseData?.Invoke();
            }
        }

    }

}