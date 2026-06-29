using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Base;
using Game.Config;
using Game.MusicalInstrument;
using Game.Props.PropsManagers;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

namespace Game.AnimationStudio
{
    public class AnimationStudioDetailView : MonoBehaviour
    {
        public CButton Btn_Close;
        public CButton Btn_DraftEdit;
        public CButton Btn_EditName;
        public CButton Btn_Copy;
        public CButton Btn_Delete;
        public CButton Btn_Publish;
        public CButton Btn_GameImage;
        public BUD_Text Txt_MapName;
        public CText Txt_LastEditTime;
        public RemoteImageBehaviour Remote_MapCover;

        private Action<DraftListItem> reuploadAction;
        private DraftListItem _draftListItem;

        private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

        private AnimationStudioType _animationStudioType = AnimationStudioType.Animation;

        public void InitUI()
        {
            Btn_Close.onClick.AddListener(HidePanel);
            Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
            Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
            Btn_Copy.onClick.AddListener(OnBtnCopyClick);
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
            Btn_Publish.onClick.AddListener(GoToPublishView);
            Btn_GameImage.onClick.AddListener(GoToPublishView);
        }

        public void InitType(AnimationStudioType type)
        {
            this._animationStudioType = type;
        }

        private bool CheckDataIllegal(DraftListItem draftListItem)
        {
            if (draftListItem == null)
            {
                LoggerUtils.LogError("详情页 - draftListItem = null");
                return true;
            }
            
            if (_animationStudioType == AnimationStudioType.Animation)
            {
                if (draftListItem.animInfo == null)
                {
                    LoggerUtils.LogError("详情页 - animInfo = null");
                    return true;
                }
            }
            else
            {
                if (draftListItem.poseInfo == null)
                {
                    LoggerUtils.LogError("详情页 - poseInfo = null");
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 更新详情布局数据
        /// </summary>
        /// <param name="draftListItem"></param>
        /// <param name="studioType"></param>
        public void UpdateInfo(DraftListItem draftListItem)
        {
            if(CheckDataIllegal(draftListItem))
                return;

            this.gameObject.SetActive(true);
            _draftListItem = draftListItem;

            Txt_MapName.text = GameStudioUtils.GetBaseInfo(draftListItem).name;
            Txt_LastEditTime.SetLocalText("最后编辑 {0}", TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));

            string coverUrl = GameStudioUtils.GetBaseInfo(draftListItem).cover;
            if (!string.IsNullOrEmpty(coverUrl))
            {
                Remote_MapCover.Load(coverUrl);
            }
        }

        private void HidePanel()
        {
            this.gameObject.SetActive(false);
        }

        private void OnDraftsEditBtnClick()
        {
            if(CheckDataIllegal(_draftListItem))
                return;
            
            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
            
            switch (_animationStudioType)
            {
                case AnimationStudioType.Animation:
                    var animSprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCAnim_" + _draftListItem.animInfo.animType, gameObject);
                    p.Init(new AnimInfo()
                    {
                        name = _draftListItem.animInfo.name
                    }, null, LoadingType.UGCAnim, s: animSprite);
                    GameController.StartGame(EnterGameModel.UgcAnimContinueEdit, _draftListItem.animInfo, true, null);
                    break;
                
                case AnimationStudioType.Pose:
                    var tempPoseInfo = _draftListItem.poseInfo.Clone();
                    tempPoseInfo.poseData = _draftListItem.poseInfo.poseData;
                    AnimDataManager.Inst.enterMode = EnterPanelMode.Standard;
                    AnimDataManager.Inst.animPose =  tempPoseInfo;
                    
                    var poseSprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCAnim_" + _draftListItem.poseInfo.poseType, gameObject);
                    p.Init(new PoseInfo()
                    {
                        name = _draftListItem.poseInfo.name
                    }, null, LoadingType.UGCAnim, s: poseSprite);
                    GameController.StartGame(EnterGameModel.AnimPoseContinueEdit, _draftListItem.poseInfo, true, "AnimatedScene");
                    break;
            }
            HidePanel();
        }

        private void OnEditNameBtnClick()
        {
            var name = GameStudioUtils.GetBaseInfo(_draftListItem).name;
            var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
            panel.SetOnClickAction(ConfirmEditName);
        }

        private void ConfirmEditName(string name)
        {
            if(CheckDataIllegal(_draftListItem))
                return;

            SetAnimInfoReq req = new SetAnimInfoReq();
            switch (_animationStudioType)
            {
                case AnimationStudioType.Animation:
                    _draftListItem.animInfo.name = name;
                    req = new SetAnimInfoReq
                    {
                        animInfo = _draftListItem.animInfo,
                        setType = (int)SetType.Edit
                    };
                    NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setAnim, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
                    break;
                
                case AnimationStudioType.Pose:
                    _draftListItem.poseInfo.name = name;
                    req = new SetAnimInfoReq
                    {
                        poseInfo = _draftListItem.poseInfo,
                        setType = (int)SetType.Edit
                    };
                    NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetPose, HttpMethod.POST, JsonConvert.SerializeObject(req), OnEditSuccess, OnEditFail);
                    break;
            }
        }

        private void OnEditSuccess(string msg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            HidePanel();
            RefreshDraftsList();
        }

        private void OnEditFail(string failMsg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            LoggerUtils.LogError(failMsg);
        }

        private void OnBtnCopyClick()
        {
            if(CheckDataIllegal(_draftListItem))
                return;

            SetAnimInfoReq req = new SetAnimInfoReq();
            switch (_animationStudioType)
            {
                case AnimationStudioType.Animation:
                    req = new SetAnimInfoReq
                    {
                        animInfo = _draftListItem.animInfo,
                        setType = (int)SetType.Copy
                    };
                    NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setAnim, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
                    break;
                
                case AnimationStudioType.Pose:
                    req = new SetAnimInfoReq
                    {
                        poseInfo = _draftListItem.poseInfo,
                        setType = (int)SetType.Copy
                    };
                    NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetPose, HttpMethod.POST, JsonConvert.SerializeObject(req), CopySuccess, CopyFail);
                    break;
            }
        }

        private void CopySuccess(string msg)
        {
            HidePanel();
            RefreshDraftsList();
        }

        private void CopyFail(string failMsg)
        {
            LoggerUtils.LogError(failMsg);
        }

        private void OnBtnDeleteClick()
        {
            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetText("确认删除", "你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
        }

        private void ConfirmClick()
        {
            if(CheckDataIllegal(_draftListItem))
                return;

            SetAnimInfoReq req = new SetAnimInfoReq();
            switch (_animationStudioType)
            {
                case AnimationStudioType.Animation:
                    req = new SetAnimInfoReq
                    {
                        animInfo = _draftListItem.animInfo,
                        setType = (int)SetType.Delete
                    };
                    NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setAnim, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
                    break;
                
                case AnimationStudioType.Pose:
                    req = new SetAnimInfoReq
                    {
                        poseInfo = _draftListItem.poseInfo,
                        setType = (int)SetType.Delete
                    };
                    NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetPose, HttpMethod.POST, JsonConvert.SerializeObject(req), OnDeleteSuccess, OnDeleteFail);
                    break;
            }
        }

        private void CancelClick()
        {
        }

        private void OnDeleteSuccess(string msg)
        {
            HidePanel();
            RefreshDraftsList();
        }

        private void OnDeleteFail(string failMsg)
        {
            LoggerUtils.LogError(failMsg);
        }

        private void GoToPublishView()
        {
            OnBtnPublishClick(CurrencyType.PinkCoin);
        }

        private void OnBtnPublishClick(CurrencyType currencyType)
        {
            if(CheckDataIllegal(_draftListItem))
                return;
            
            var publishMachine = new UGCPublishStateMachine();
            var stateList = new List<UGCPublishStateBase>()
            {
            };
            
            switch (_animationStudioType)
            {
                case AnimationStudioType.Animation:
                    var tmpAnimInfo = _draftListItem.animInfo.Clone();
                    stateList.Add(new UGCAnimDetailState());
                    publishMachine.SetStates(stateList);

                    var ugcAnimDraftInfo = UGCAnimAssetManager.Inst.GetOrCreateDraftInfo(tmpAnimInfo);
                    if (ugcAnimDraftInfo == null)
                        return;

                    publishMachine.SetEditData(new UGCAnimEditData()
                    {
                        draftInfo = ugcAnimDraftInfo,
                        currencyType = currencyType
                    });
                    break;
                
                case AnimationStudioType.Pose:
                    var tmpPoseInfo = _draftListItem.poseInfo.Clone();
                    stateList.Add(new UGCPoseDetailState());
                    publishMachine.SetStates(stateList);
                    
                    var ugcPoseDraftInfo = PoseAssetManager.Inst.GetOrCreateDraftInfo(tmpPoseInfo);
                    if (ugcPoseDraftInfo == null)
                        return;

                    publishMachine.SetEditData(new UGCPoseEditData()
                    {
                        draftInfo = ugcPoseDraftInfo,
                        currencyType = currencyType
                    });
                    break;
            }

            publishMachine.SetCancelCallBack(() => { });
            publishMachine.SetFinishCallBack(() =>
            {
                HidePanel();
                RefreshPublishedList();
            });

            publishMachine.Start();
        }

        private void RefreshDraftsList()
        {
            MessageHelper.Broadcast(MessageName.OnUgcAnimStudioDraftListChange);
        }

        private void RefreshPublishedList()
        {
            MessageHelper.Broadcast(MessageName.OnUgcAnimStudioPublishedListChange);
        }
    }
}