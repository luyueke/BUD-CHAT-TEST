using System;
using System.Collections.Generic;
using System.IO;
using Es;
using Game.Base;
using Game.Config;
using Game.Store;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.PgcData;
using GameData.UGCData;
using Google.Protobuf;
using Message;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RTG;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class SaveView : PoseBaseView
    {
        public GameObject ViewNode;
        public LoadingButton QuickSaveBtn;
        public Button ReturnBtn;
        public Button ExitBtn;
        public Button SaveBtn;
        public LoadingButton SaveBudPoseBtn;//仅提供美术创建官方Pose
        public Button LeftUpUploadBtn;
        public LoadingButton UploadBtn;
        public CButton SelectBtn;
        public CButton ResetBtn;
        public UnityAction OnResetClick;
        public Action<string> OnSelectPoseClick;
        private bool isAnimEnter = false;
        public Action SaveDataClick;
        public Action ChangeWhiteRole;
        public Action RevertImageRole;
        private const string TAG = "PoseModeController";
        private int curSet;
        private int totalSet;
        private HttpPageRequestHandle<PoseOcListPageUseData> dataHandle;
        public override void OnCreate()
        {
            base.OnCreate();
            ExitBtn.onClick.AddListener(OnExitClick);
            ReturnBtn.onClick.AddListener(OnReturnBtnClick);
            SaveBtn.onClick.AddListener(OnSaveClick);
            QuickSaveBtn.onClick.AddListener(QuickSaveClick);
            UploadBtn.onClick.AddListener(OnUploadClick);
            LeftUpUploadBtn.onClick.AddListener(OnUploadClick);
            SelectBtn.onClick.AddListener(ShowPosePanel);
            ResetBtn.onClick.AddListener(ResetJointNode);
#if UNITY_EDITOR
            SaveBudPoseBtn.gameObject.SetActive(true);
            SaveBudPoseBtn.onClick.AddListener(OnBUDSaveClick);
#endif
        }

        public void ResetJointNode()
        {
            OnResetClick?.Invoke();
        }

        private void ShowPosePanel()
        {
            var addPosePanel = UIManager.Inst.OpenPanel<AddAnimPosePanel>(PanelId.AddAnimPosePanel,curPoseType);
            addPosePanel.OnSelectPoseClick = OnSelectPoseClick;
            addPosePanel.SetRefreshSlot(OnRefreshSlot);
        }

        private void OnUploadClick()
        {
            SaveDataClick?.Invoke();
            var publishMachine = new UGCPublishStateMachine();
            var stateList = new List<UGCPublishStateBase>();
          
            var tmpPoseInfo =  AnimDataManager.Inst.animPose.Clone();
            stateList.Add(new UGCPoseDetailState());
            publishMachine.SetStates(stateList);
            var ugcPoseDraftInfo = PoseAssetManager.Inst.GetDraftInfo(tmpPoseInfo);
            if (ugcPoseDraftInfo == null)
            {
                ugcPoseDraftInfo = new PoseDraftInfo(tmpPoseInfo);
            }
            var shotData = GetShotData();
            ugcPoseDraftInfo.SetCover(shotData);
            publishMachine.SetEditData(new UGCPoseEditData()
            {
                draftInfo = ugcPoseDraftInfo,
                currencyType = CurrencyType.PinkCoin
            });

            publishMachine.SetCancelCallBack(() => { });
            publishMachine.SetFinishCallBack(() =>
            {
                UIManager.Inst.ClosePanel(PanelId.UGCPublishPanel);
            });

            publishMachine.Start();
        }

        private void OnBUDSaveClick()
        {
#if UNITY_EDITOR
            SaveDataClick?.Invoke();
            var shotData = GetShotData();
            File.WriteAllBytes(Application.streamingAssetsPath + "/pose.png", shotData);
            var tempData = AnimDataManager.Inst.animPose.Clone();
            File.WriteAllText(Application.streamingAssetsPath + "/pose.json",JsonConvert.SerializeObject(tempData));
#endif
        }

        public override void OnShow(EnterPanelMode panelMode,UgcPoseSubType poseType)
        {
            base.OnShow(panelMode,poseType);
            curPoseType = poseType;
            isAnimEnter = panelMode == EnterPanelMode.AnimEnter;
            LeftUpUploadBtn.gameObject.SetActive(panelMode == EnterPanelMode.AnimEnter);
            ViewNode.gameObject.SetActive(!isAnimEnter);
            ReturnBtn.gameObject.SetActive(isAnimEnter);
            GameTimeUtils.Inst.StartCollect(TAG);
            bool isCharacter = curPoseType == UgcPoseSubType.Single || curPoseType == UgcPoseSubType.Double;
            dataHandle = new HttpPageRequestHandle<PoseOcListPageUseData>(HttpUrlDefine.quickPoseList,
                paramStr: JsonConvert.SerializeObject(new JObject() {["skinType"] = isCharacter ? 0 : 1,["poseType"] = (int)curPoseType}),
                reqMethod: HttpMethod.GET);
            dataHandle.AddSuccessAction(OnPullData);
            dataHandle.Start();
        }


        private void OnExitClick()
        {
            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetIsCloseSelf(false);
            commonConfirmPanel.SetText("确认保存", "保存当前的创作进度吗？", "保存", "不保存");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                SaveDataClick?.Invoke();
                SaveTmpPoseInfo(false,(success) =>
                {
                    commonConfirmPanel.SetConfirmLoadingVisible(true);
                    if (success)
                    {
                        GameController.ExitGame(() =>
                        {
                            AnimDataManager.Inst.ClearData();
                            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.AnimWindow, true);
                            UIManager.Inst.BackToLastWindow();
                            MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                        });
                    }
                });
            }, () =>
            {
                if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                {
                    commonConfirmPanel.Close();
                }
                GameController.ExitGame(() =>
                {
                    AnimDataManager.Inst.ClearData();
                    UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.AnimWindow, true);
                    UIManager.Inst.BackToLastWindow();
                    MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                });
            });
        }

        private void OnReturnBtnClick()
        {
            SaveDataClick?.Invoke();
            GameController.ExitGame(() =>
            {
                UIAgentManager.Inst.OpenPanel(PanelId.BlackPanel);
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.AnimWindow, true);
                UIManager.Inst.BackToLastWindow();
                UIManager.Inst.ClosePanel(PanelId.AnimPoseEditPanel);
                GameController.StartGame(EnterGameModel.UgcAnimContinueEdit, AnimDataManager.Inst.animInfo, true,
                    "GameHall");
            },false);
        }

        public void OnSaveClick()
        {
            SaveBudPoseBtn.SetLoadingVisible(true);
            SaveDataClick?.Invoke();
            SaveTmpPoseInfo(false,(success) =>
            {
                SaveBudPoseBtn.SetLoadingVisible(false);
            });
        }

        private void QuickSaveClick()
        {
            if (!CanSave())
            {
                QuickSaveBtn.SetLoadingVisible(false);
                var quickPosePanel = UIManager.Inst.OpenPanel<BuyQuickPosePanel>(PanelId.BuyQuickPosePanel,curPoseType);
                quickPosePanel.SetOnBuySuccessAct(OnRefreshSlot);
                return;
            }
            QuickSaveBtn.SetLoadingVisible(true);
            SaveDataClick?.Invoke();
            SaveTmpPoseInfo(true,(success) =>
            {
                QuickSaveBtn.SetLoadingVisible(false);
                OnRefreshSlot();
            });
        }


        private void OnRefreshSlot()
        {
            dataHandle.Reset();
            dataHandle.Start();
        }
        
        private void OnRefreshSlot(int current,int count)
        {
            curSet = current;
            totalSet = count;
        }


        private bool CanSave()
        {
            if (totalSet == 0)
            {
                dataHandle.Reset();
                dataHandle.Start();
                return false;
            }
            return curSet < totalSet;
        }

        private void OnPullData(PoseOcListPageUseData data)
        {
            curSet = data.totalCount;
            totalSet = data.totalSlotCount;
        }

        private void SaveTmpPoseInfo(bool isQuickSave,Action<bool> callBack = null)
        {
            var poseInfo = AnimDataManager.Inst.animPose;
            var poseDraftInfo = PoseAssetManager.Inst.GetOrCreateDraftInfo(poseInfo);
            int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG); //重启编辑时长
            poseDraftInfo.editTime += curEditTime;
         
            // 截图需要隐藏地板
            var shotData = GetShotData();
            poseDraftInfo.SetCover(shotData);
            poseDraftInfo.isQuickSave = isQuickSave;
            poseDraftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            poseDraftInfo.UploadAndSave((info, isSuccess) =>
            {
                TipPanel.ShowToast(isSuccess ? "保存成功:D" : "保存失败");
                callBack?.Invoke(isSuccess);
            });
        }

        private byte[] GetShotData()
        {
            ChangeWhiteRole?.Invoke();
            var terrain = GameObject.Find("Grid");
            terrain.gameObject.SetActive(false);
            bool isRtEnabled = RTGApp.Get.enabled;
            RTGApp.Get.enabled = false;
            var shotCamera = GameCameraUtils.Inst.GetShotCamera();
            shotCamera.enabled = true;

            var whtieIkController = AnimPoseEditPanel.Creater.GetIkController(PoseImageType.WhiteBody);
            whtieIkController.ChangeShotMaterial(true);

            List<Vector3> orgPositions = new List<Vector3>();
            var poseModeData = DataTables.GetPoseModeConfig((int) AnimPoseEditPanel.Creater.curAnimType);
            var optNodes = AnimPoseEditPanel.Creater.OptNodes;
            for (var i = 0; i < optNodes.Count; i++)
            {
                orgPositions.Add(optNodes[i].transform.localPosition);
                optNodes[i].transform.localPosition = poseModeData.ShotEditPos[i];
            }

            shotCamera.transform.localPosition = poseModeData.ShotCamPos;
            shotCamera.orthographicSize = poseModeData.ShotCamSize;
            var shotData = ScreenShotUtils.TakeShot(shotCamera,
                GameConsts.UGCPoseShotSize, true);
            whtieIkController.ChangeShotMaterial(false);
            
            for (var i = 0; i < optNodes.Count; i++)
            {
                optNodes[i].transform.localPosition = orgPositions[i];
            }
            
            shotCamera.enabled = false;
            RTGApp.Get.enabled = isRtEnabled;
            terrain.gameObject.SetActive(true);
            RevertImageRole?.Invoke();
            return shotData;
        }
    }
}