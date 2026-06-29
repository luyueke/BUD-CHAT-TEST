using System.Collections.Generic;
using System.Linq;
using Es;
using Game;
using Game.Avatar;
using Game.Pet;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.Manager;
using UI.UIPanels.CommonConfirm;
using UnityEngine;

namespace BUD.AnimPose
{
    public class AnimPoseEditPanel : BasePanel<AnimPoseEditPanel>
    {
        private List<PoseBaseView> allViews = new List<PoseBaseView>();

        public PoseHandleView handleTool;
        public ChangeImageView imageModeView;
        public AnimUndoRedoView undoRedoView;
        public AddItemView itemView;
        public SaveView saveView;
        public static PoseRoleCreater Creater;

        private UgcPoseSubType curPoseType;

        private float orgRate = 1;
        private KeyFrameData curKeyFrameData;
        private EnterPanelMode enterMode;
        private List<GameObject> propOptNode;
        private Vector3 customMinSize = Vector3.one * -500;
        private Vector3 customMaxSize = Vector3.one * 500;
        public class PropTargetData
        {
            public AddPropViewItem curViewItem;
            public PropAnimIK propIk;
        }

        public static PropTargetData propTarget = new PropTargetData();

        public override void OnCreate()
        {
            base.OnCreate();
            Creater = new PoseRoleCreater();
            InitView();
            orgRate = GizmoController.CurRate;
            GizmoController.CurRate *= 0.8f;
            allViews.ForEach(x => x.OnCreate());
            imageModeView.ChangeImageModeAction = ChangeImageModeAction;
            saveView.SaveDataClick = SavePoseData;
            saveView.ChangeWhiteRole = ChangeWhiteImageModeAction;
            saveView.RevertImageRole = RevertImageModeAction;
            saveView.OnResetClick = ResetIkClick;
            saveView.OnSelectPoseClick = SetCurKeyFrameData;
            itemView.OnSelectProp = OnSelectProp;
            handleTool.SyncPoseData = SyncCurrentPoseData;
         
            InputHandlerManager.Inst.AddSelectNodeListener(OnHandTool);
            InputHandlerManager.Inst.AddUnSelectAllListener(OnUnSelectTarget);
            InputHandlerManager.Inst.AddUnSelectAllListener(UnSelectClick);
        }

        
        
       

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            var enterPoseData = AnimDataManager.Inst.animPose;
            enterMode = AnimDataManager.Inst.enterMode;
            curPoseType = (UgcPoseSubType)enterPoseData.poseType;
            allViews.ForEach(x => x.OnShow(enterMode,curPoseType));
            Creater.Create(curPoseType);
            CreateAnimIKs();
            SetCamera(curPoseType);
            GetBatchPropInfo();
            curKeyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(enterPoseData.poseData);
            Creater.SetKeyFrameData(curKeyFrameData);
            Creater.curImageMode = AnimDataManager.Inst.curImageMode;
            imageModeView.SetText(AnimDataManager.Inst.curImageMode);
            Creater.ChangeImageModeAction(AnimDataManager.Inst.curImageMode);
        }

        public override void OnHidden()
        {
            base.OnHidden();
            GizmoManager.Inst.CurGizmoCtrl.IsCustomLimitMoveSize = false;
            GizmoManager.Inst.CurGizmoCtrl.CustomMinSize = customMinSize;
            GizmoManager.Inst.CurGizmoCtrl.CustomMaxSize = customMaxSize;
        }


        private void ResetIkClick()
        {
            GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();   
            Creater.SetCurImageParentDefaultPosition();
            Creater.ResetJointNodes();
        }

        private void GetBatchPropInfo()
        {
            if (enterMode ==  EnterPanelMode.Standard)
            {
                return;
            }
            UpdateUIBindPropIk();
        }

        private void UpdateUIBindPropIk()
        {
            var ikController = Creater.GetCurrentIkController();
            ikController.CreatePropIKs(curPoseType,AnimDataManager.Inst.animInfo.propList);
            var propDir = ikController.GetPropIKs();
            foreach (var keyValue in propDir)
            {
                itemView.SetPropIk(keyValue.Value);
            }
        }

        public void SetCurKeyFrameData(string poseData)
        {
            SyncCurrentPoseData();
            var tempFrameData = JsonConvert.DeserializeObject<KeyFrameData>(poseData);
            curKeyFrameData.keyFrame = tempFrameData.keyFrame;
            Creater.SetKeyFrameData(curKeyFrameData);
            GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
            handleTool.gameObject.SetActive(false);
        }
        
        private void SetCamera(UgcPoseSubType animType)
        {
            var cameraTarget = GameCameraUtils.Inst.GetEditVirtualCamera().Follow.transform;
            var poseModeData = DataTables.GetPoseModeConfig((int) animType);
            cameraTarget.eulerAngles = poseModeData.EditCamRot;
            cameraTarget.position = poseModeData.EditCamPos;

            var shotCamera = GameCameraUtils.Inst.GetShotCamera().transform;
            var tempPosition = shotCamera.position;
            tempPosition.x = poseModeData.EditCamPos.x;
            shotCamera.position = tempPosition;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GizmoController.CurRate = orgRate;
            Creater.Release();
            Creater = null;
            InputHandlerManager.Inst.RemoveSelectNodeListener(OnHandTool);
            InputHandlerManager.Inst.RemoveUnSelectAllListener(UnSelectClick);
        }

        private void InitView()
        {
            allViews.Add(handleTool);
            allViews.Add(imageModeView);
            allViews.Add(undoRedoView);
            allViews.Add(itemView);
            allViews.Add(saveView);
        }

        public void CreateAnimIKs()
        {
            var selfData = AccountDataManager.Inst.UserInfo.avatarInfo;
            var petData = AccountDataManager.Inst.PetInfo.avatarInfo;
            Creater.CreateWhiteRole(this.gameObject);
            Creater.CreateRole(selfData, petData,true);
            Creater.AddFullBodyJointNodeCollider();
            Creater.ChangeImageModeAction(PoseImageType.Current);
        }

        private void ChangeImageModeAction(PoseImageType imageType)
        {
            if (Creater.curImageMode != imageType)
            {
                Creater.ChangeImageModeAction(imageType);
                Creater.curImageMode = imageType;
                UnSelectClick();
            }
        }
        
        private void ChangeWhiteImageModeAction()
        {
            if (Creater.curImageMode !=  PoseImageType.WhiteBody)
            {
                Creater.ChangeImageModeAction(PoseImageType.WhiteBody);
            }
        }

        private void RevertImageModeAction()
        {
            if (Creater.curImageMode != PoseImageType.WhiteBody)
            {
                Creater.ChangeImageModeAction(Creater.curImageMode);
            }
        }


        private void OnUnSelectTarget()
        {
            GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
            handleTool.gameObject.SetActive(false);
        }

        private void OnHandTool(RaycastHit[] raycasts)
        {
            if (GizmoManager.Inst.CurGizmoCtrl == null)
            {
                return;
            }
    
            if (raycasts.Length == 0)
            {
                return;
            }
            
            var curTarget = GizmoManager.Inst.CurGizmoCtrl.GetCurrentTarget();
            Creater.SetJointNodeInVisible();

            bool isSelectNode = false;
            GameObject selectNode = null;
            PoseEditOperation poseEditData = null;
            for (int i = 0; i < raycasts.Length; i++)
            {
                poseEditData = DataTables.GetPoseEditOperation(raycasts[i].collider.name);
                if (poseEditData != null && (poseEditData.RoleType == (int) RoleType.Avatar || poseEditData.RoleType == (int) RoleType.Pet))
                {
                    isSelectNode = true;
                    selectNode = raycasts[i].collider.gameObject;
                    BaseAnimIK animIk = selectNode.GetComponentInParent<BaseAnimIK>(true);
                    SetJointNodeVisible(animIk.gameObject,true);
                    break;
                }
            }

            if (!isSelectNode)
            {
                for (int i = 0; i < raycasts.Length; i++)
                {
                    poseEditData = DataTables.GetPoseEditOperation(raycasts[i].collider.name);
                    if (poseEditData != null && poseEditData.RoleType == (int) RoleType.FullBody)
                    {
                        selectNode = raycasts[i].collider.gameObject;
                        SetJointNodeVisible(selectNode,true);
                        break;
                    }
                }
            }
            if (poseEditData != null && selectNode != null)
            {
                if (selectNode == curTarget)
                {
                    return;
                }
                handleTool.OnMoveClick();
                if (propTarget.curViewItem != null)
                {
                    propTarget.curViewItem.SetSelectIconVisible(false);
                }
                handleTool.gameObject.SetActive(true);
                GizmoManager.Inst.CurGizmoCtrl.IsCustomLimitMoveSize = poseEditData.IsLimit;
                GizmoManager.Inst.CurGizmoCtrl.SetTarget(selectNode);
                handleTool.gameObject.SetActive(true);
                handleTool.OnMoveClick();
                if (poseEditData.PartType == (int) IKPart.Head)
                {
                    AvatarAnimIK animIK = selectNode.transform.parent.parent.GetComponent<AvatarAnimIK>();
                    var headPosition = animIK.GetHeadNodePosition();
                    GizmoManager.Inst.CurGizmoCtrl.CustomMinSize = headPosition + poseEditData.MoveMin;
                    GizmoManager.Inst.CurGizmoCtrl.CustomMaxSize = headPosition + poseEditData.MoveMax;
                }
                else if (poseEditData.IsLimit)
                {
                    GizmoManager.Inst.CurGizmoCtrl.CustomMinSize = poseEditData.MoveMin;
                    GizmoManager.Inst.CurGizmoCtrl.CustomMaxSize = poseEditData.MoveMax;
                }
                ChangeHandleOption(poseEditData.JointName);
                return;
            }
            
            PropAnimIK propIK = raycasts[0].transform.GetComponentInParent<PropAnimIK>(true);
            if (propIK != null)
            {
                if (propIK.gameObject != curTarget)
                {
                    OnSelectProp(propIK);
                }
                return;
            }

        }
        

        private void OnSelectProp(PropAnimIK propIk)
        {
            handleTool.OnMoveClick();
            propTarget.propIk = propIk;
            if (propTarget.curViewItem != null)
            {
                propTarget.curViewItem.SetSelectIconVisible(false);
            }
            propTarget.curViewItem = itemView.GetSelectItem(propIk.curIndex);
            propTarget.curViewItem.SetSelectIconVisible(true);
            GizmoManager.Inst.CurGizmoCtrl.SetTarget(propIk.gameObject);
            ChangeHandleOnSelectProp(propIk.name);
        }

        private void SetJointNodeVisible(GameObject selectNode,bool visible)
        {
            if (selectNode == null)
            {
                return;
            }
            var animIKs = selectNode.GetComponentsInChildren<BaseAnimIK>(true);
            for (var i = 0; i < animIKs.Length; i++)
            {
                animIKs[i].SetJointNodeVisible(visible);
            }
        }

        private void ChangeHandleOnSelectProp(string targetName)
        {
            var poseEditData = DataTables.GetPoseEditOperation(targetName);
            if (poseEditData != null)
            {
                GizmoManager.Inst.CurGizmoCtrl.IsCustomLimitMoveSize = poseEditData.IsLimit;
                GizmoManager.Inst.CurGizmoCtrl.CustomMinSize = poseEditData.MoveMin;
                GizmoManager.Inst.CurGizmoCtrl.CustomMaxSize = poseEditData.MoveMax;
                handleTool.gameObject.SetActive(true);
                handleTool.SwitchHandleType(poseEditData);
            }
        }

        private void ChangeHandleOption(string targetName)
        {
            var poseEditData = DataTables.GetPoseEditOperation(targetName);
            if (poseEditData != null)
            {
                handleTool.gameObject.SetActive(true);
                handleTool.SwitchHandleType(poseEditData);
            }
        }


        private void UnSelectClick()
        {
            Creater.SetJointNodeInVisible();
            handleTool.gameObject.SetActive(false);
            propTarget.propIk = null;
            if (propTarget.curViewItem != null)
            {
                propTarget.curViewItem.SetSelectIconVisible(false);
            }
            GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
        }


        private void SavePoseData()
        {
            if (curKeyFrameData == null)
            {
                return;
            }
            SyncCurrentPoseData();
            if (enterMode == EnterPanelMode.AnimEnter)
            {
                AnimDataManager.Inst.animPose.poseData = JsonConvert.SerializeObject(curKeyFrameData);
                
                var propDir = AnimDataManager.Inst.propDic;
                if (propDir == null || propDir.Count == 0)
                {
                    AnimDataManager.Inst.animInfo.propList = new List<AnimPropData>();
                }
                else
                {
                    List<AnimPropData> propList = new List<AnimPropData>();
                    foreach (var keyValue in propDir)
                    {
                        propList.Add(keyValue.Value);
                    }
                    propList.Sort((a, b) => a.index.CompareTo(b.index));
                    AnimDataManager.Inst.animInfo.propList = propList;
                }
            }
            else
            {
                KeyFrameData bodyFrameData = new KeyFrameData();
                bodyFrameData.keyFrame = curKeyFrameData.keyFrame;
                AnimDataManager.Inst.animPose.poseData = JsonConvert.SerializeObject(bodyFrameData);
            }
        }

        private void SyncCurrentPoseData()
        {
            if (curKeyFrameData != null)
            {
                Creater?.SaveKeyFrameData(curKeyFrameData);
            }
        }

        private void OnSelectProp(AddPropViewItem item)
        {
            propTarget.curViewItem  = item;
            propTarget.propIk = item.propIk;
            handleTool.OnMoveClick();
            GizmoManager.Inst.CurGizmoCtrl.SetTarget(item.propIk.gameObject);
            ChangeHandleOnSelectProp(item.propIk.name);
            SyncCurrentPoseData();
        }
    }
}