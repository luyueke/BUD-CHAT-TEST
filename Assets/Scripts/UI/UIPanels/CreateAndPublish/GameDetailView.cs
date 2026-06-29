using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地图详情卡片
/// </summary>

namespace BUD.GameStudio
{
    public class GameDetailView : MonoBehaviour
    {
        public CButton Btn_Close;
        public CButton Btn_DraftEdit;
        public CButton Btn_EditName;
        public CButton Btn_Copy;
        public CButton Btn_Delete;
        public CButton Btn_Publish;
        public CButton Btn_Update;
        public CButton Btn_EditDetail;
        public CButton Btn_ViewGame;
        public CButton Btn_GameImage;
        public BUD_Text Txt_MapName;
        public CText Txt_LastEditTime;
        public RemoteImageBehaviour Remote_MapCover;

        private Action<DraftListItem> editGameAction;
        private Action<DraftListItem> editInfoAction;
        private Action<DraftListItem> copyGameAction;
        private Action<DraftListItem> deleteGameAction;
        private Action<bool> updateGameAction;
        private Action<DraftListItem> reuploadAction;
        private Action<bool> publishGameAction;

        private Action<StudioSubType> _changeViewAction;

        private DraftListItem _draftListItem;
        private StudioSubType _studioType;
        private Texture currentMapTexture;
        private IPool gamePool;

        public void InitUI()
        {
            Btn_Close.onClick.AddListener(HidePanel);
            Btn_DraftEdit.onClick.AddListener(OnDraftsEditBtnClick);
            Btn_EditName.onClick.AddListener(OnEditNameBtnClick);
            Btn_Copy.onClick.AddListener(OnBtnCopyClick);
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
            Btn_Publish.onClick.AddListener(OnPublishCheck);
            Btn_Update.onClick.AddListener(OnBtnUpdateClick);
            Btn_EditDetail.onClick.AddListener(OnBtnEditDetailClick);
            Btn_ViewGame.onClick.AddListener(OnBtnViewGameClick);
            Btn_GameImage.onClick.AddListener(OnBtnViewGameClick);
            gamePool = new FIFOCachingPool(1, TextureDestoryer);
            Remote_MapCover.InitializeWithPool(gamePool);
        }

        private void HidePanel()
        {
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新详情布局数据
        /// </summary>
        /// <param name="draftListItem"></param>
        /// <param name="studioType"></param>
        public void UpdateInfo(DraftListItem draftListItem, StudioSubType studioType)
        {
            if (draftListItem == null || draftListItem.mapInfo == null)
                return;

            this.gameObject.SetActive(true);
            this._studioType = studioType;
            _draftListItem = draftListItem;

            Txt_MapName.text = GameStudioUtils.GetBaseInfo(draftListItem).name;
            Txt_LastEditTime.SetLocalText("最后编辑 {0}",TimestampConverter.ConvertToDateTimeString(GameStudioUtils.GetBaseInfo(draftListItem).updateTime));

            ShowIsEdit(studioType == StudioSubType.Drafts);

            string coverUrl = GameStudioUtils.GetBaseInfo(draftListItem).cover;
            if (!string.IsNullOrEmpty(coverUrl))
            {
                Remote_MapCover.Load(coverUrl);
            }
        }

        private void ShowIsEdit(bool isDraft)
        {
            if (Btn_Publish) Btn_Publish.gameObject.SetActive(isDraft);
            if (Btn_EditName) Btn_EditName.gameObject.SetActive(isDraft);
            if (Btn_Copy) Btn_Copy.gameObject.SetActive(isDraft);
            if (Btn_DraftEdit) Btn_DraftEdit.gameObject.SetActive(isDraft);

            if (Btn_EditDetail) Btn_EditDetail.gameObject.SetActive(!isDraft);
            if (Btn_Update) Btn_Update.gameObject.SetActive(!isDraft);
            if (Btn_ViewGame) Btn_ViewGame.gameObject.SetActive(!isDraft);

            if (Btn_Delete) Btn_Delete.gameObject.SetActive(true);

            if (isDraft)
            {
                //草稿箱才显示上传状态
                UploadStatus uploadStatus = GameStudioUtils.GetUploadStatus(_draftListItem);
                SetUploadStatus(uploadStatus);
            }
        }

        private void TextureDestoryer(object urlKey, object texture)
        {
            var asUnityObject = texture as UnityEngine.Object;
            if (asUnityObject != null)
                Destroy(asUnityObject);
        }

        private void OnDraftsEditBtnClick()
        {
            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
            p.Init(_draftListItem.mapInfo,  _draftListItem.creator, LoadingType.Map, currentMapTexture);
            GameController.StartGame(EnterGameModel.ContinueEditScene, _draftListItem.mapInfo);
        }

        /// <summary>
        /// 修改信息
        /// </summary>
        private void OnEditNameBtnClick()
        {
            if (_studioType == StudioSubType.Drafts)
            {
                var name = GameStudioUtils.GetBaseInfo(_draftListItem).name;
                var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "修改名字", name, "确定");
                panel.SetOnClickAction(ConfirmEditName);
            }
        }

        private void ConfirmEditName(string name)
        {
            if (_draftListItem != null)
            {
                _draftListItem.mapInfo.name = name;
                var req = new SetMapInfoReq
                {
                    mapInfo = _draftListItem.mapInfo,
                    setType = (int)SetType.Edit
                };

                _draftListItem.mapInfo.name = name;
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    OnEditSuccess, OnEditFail);
            }
        }

        /// <summary>
        /// 修改名字成功
        /// </summary>
        /// <param name="msg"></param>
        private void OnEditSuccess(string msg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            editInfoAction.Invoke(_draftListItem);
        }

        /// <summary>
        /// 修改名字失败
        /// </summary>
        /// <param name="failMsg"></param>
        private void OnEditFail(string failMsg)
        {
            UIManager.Inst.ClosePanel(PanelId.EditNamePanel);
            HttpResponseFailDataStruct repData = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(failMsg);
            string errormessage = repData.rmsg;
            if (!String.IsNullOrEmpty(errormessage))
            {
                TipPanel.ShowToast(errormessage);
            }
        }

        /// <summary>
        /// 复制草稿 地图、素材、材质
        /// </summary>
        private void OnBtnCopyClick()
        {
            if (_draftListItem != null)
            {
                var req = new SetMapInfoReq
                {
                    mapInfo = _draftListItem.mapInfo,
                    setType = (int)SetType.Copy
                };

                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    CopySuccess, CopyFail);
            }
        }

        private void CopySuccess(string msg)
        {
            copyGameAction?.Invoke(_draftListItem);
        }

        private void CopyFail(string failMsg)
        {
        }

        private void OnBtnDeleteClick()
        {
            if (_studioType == StudioSubType.Published)
            {
                CommonConfirmWithTitlePanel commonConfirmPanel =
                    UIManager.Inst.OpenPanel<CommonConfirmWithTitlePanel>(PanelId.CommonConfirmWithTitlePanel);
                commonConfirmPanel.SetLocalText("确定要删除吗？", "删除后无法复原，且和该作品相关的点赞和评论也将消失。", "删除", "取消");
                commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
            }
            else if (_studioType == StudioSubType.Drafts)
            {
                CommonConfirmPanel commonConfirmPanel =
                    UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
                commonConfirmPanel.SetLocalText("确认删除","你确定要删除该草稿吗？\n 一旦删除就无法找回", "删除", "取消");
                commonConfirmPanel.SetOnClickAction(ConfirmClick, CancelClick);
            }
        }


        private void ConfirmClick()
        {
            if (_draftListItem != null)
            {
                var req = new SetMapInfoReq
                {
                    mapInfo = _draftListItem.mapInfo,
                    setType = (int)SetType.Delete
                };

                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    OnDeleteSuccess, OnDeleteFail);
            }
        }

        private void CancelClick()
        {
        }

        private void OnDeleteSuccess(string msg)
        {
            deleteGameAction?.Invoke(_draftListItem);
            HidePanel();
        }

        private void OnDeleteFail(string failMsg)
        {
        }

        private void OnPublishCheck()
        {
            if (_draftListItem.mapInfo == null)
            {
                LoggerUtils.LogError("mapInfo 为空");
                return;
            }

            // string mapId = "2kGez1FypKQr3WJaTo2C5i1P0Oe";
            string mapId = _draftListItem.mapInfo.id;
            var jb = new JObject
            {
                ["id"] = mapId
            };
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getBannedUgcs, HttpMethod.GET, JsonConvert.SerializeObject(jb), content =>
            {
                var getBannedUgcsResponse = JsonConvert.DeserializeObject<GetBannedUgcsResponse>(content);
                if (getBannedUgcsResponse == null)
                {
                    OnBtnPublishClick();
                    return;
                }
                var list = getBannedUgcsResponse.list;
                if (list == null || list.Count == 0)
                {
                    OnBtnPublishClick();
                    return;
                }
                var panel = UIManager.Inst.OpenPanel<RemovedItemsPanel>(PanelId.RemovedItemsPanel, list);
            } , failMessage =>
            {
                TipPanel.ShowToast(failMessage);
            });
        }

        private void OnBtnPublishClick()
        {
            if (_draftListItem.mapInfo == null)
            {
                LoggerUtils.LogError("mapInfo 为空");
                return;
            }

            var detailInfo = _draftListItem.mapInfo.detailInfo;
            //判断是否通过顶点数校验
            if (detailInfo != null && !GameProfilerManager.Inst.CheckStatisticInfoIsPass(LimitType.Map, detailInfo))
            {
                UIManager.Inst.OpenPanel<PublishWarningPanel>(PanelId.PublishWarningPanel, LimitType.Map, detailInfo);
                return;
            }


            PublishSelectData data = new PublishSelectData()
            {
                DraftListItem = _draftListItem,
                PublishPassLevelMapAct = () =>
                {
                    //通关图发布
                    UIManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel, new CommonSingleConfirmPanelData()
                    {
                        ContextString = "通关地图即可发布！",
                        ConfirmString = "确定",
                        ConfirmClickAction = OnPulishTestClick,
                    });
                },
                PublishNormalMapAct = GoToPublishMapPanel,
            };

            UIManager.Inst.OpenPanel(PanelId.PublishSelectWayPanel, data);
        }

        private void OnBtnUpdateClick()
        {
            //已发布跳转更新页面
            //TODO 接入杰哥的
            UpdateMapPanel updateMapPanel = UIManager.Inst.OpenPanel<UpdateMapPanel>(PanelId.UpdateMapPanel, _draftListItem);
            updateMapPanel.SetUpdateMapAction(UpdateMapAction);
        }

        private void OnBtnEditDetailClick()
        {
            if (GameStudioUtils.GetBaseInfo(_draftListItem) == null)
                return;
            var publishMachine = new UGCPublishStateMachine();
            publishMachine.SetStates(new List<UGCPublishStateBase>()
            {
                new UGCMapDetailState()
            });

            var tempMapInfo = _draftListItem.mapInfo.Clone();
            var draftInfo = MapAssetManager.Inst.GetDraftInfo(tempMapInfo);
            if (draftInfo == null)
            {
                draftInfo = new MapDraftInfo(tempMapInfo);
            }

            publishMachine.SetEditData(new MapEditData()
            {
                draftInfo = draftInfo,
            });
            publishMachine.SetFinishCallBack((Action)(() =>
            {
                _draftListItem.mapInfo = publishMachine.GetEditData().GetInfo() as MapInfo;
                editInfoAction.Invoke(_draftListItem);
            }));
            publishMachine.Start();



        }

        private void OnBtnViewGameClick()
        {
            if (_studioType == StudioSubType.Published)
            {
                UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, _draftListItem.mapInfo.id);
            }
        }

        /// <summary>
        /// 更新地图回调
        /// </summary>
        private void UpdateMapAction(bool isSuccess)
        {
            updateGameAction?.Invoke(isSuccess);
        }

        public void GoToPublishMapPanel()
        {
            var publishMachine = new UGCPublishStateMachine();
            publishMachine.SetStates(new List<UGCPublishStateBase>()
            {
                new UGCMapDetailState()
            });

            var tempMapInfo = _draftListItem.mapInfo.Clone();
            var draftInfo = MapAssetManager.Inst.GetDraftInfo(tempMapInfo);
            if (draftInfo == null)
            {
                draftInfo = new MapDraftInfo(tempMapInfo);
            }

            publishMachine.SetEditData(new MapEditData()
            {
                draftInfo = draftInfo,
                isPublish = true
            });

            publishMachine.SetFinishCallBack(() =>
            {
                PublishMapAction(true);
                _changeViewAction?.Invoke(StudioSubType.Published);
                HidePanel();
            });
            publishMachine.SetCancelCallBack(() => { PublishMapAction(false); });
            publishMachine.Start();
        }


        private void PublishMapAction(bool isSuccess)
        {
            publishGameAction.Invoke(isSuccess);
        }

        void OnPulishTestClick()
        {
            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
            p.Init(_draftListItem.mapInfo, _draftListItem.creator, LoadingType.Map, currentMapTexture);
            GameController.StartGame(EnterGameModel.PublishTest, _draftListItem.mapInfo);
        }

        /// <summary>
        /// 设置上传状态
        /// </summary>
        /// <param name="uploadStatus"></param>
        private void SetUploadStatus(UploadStatus uploadStatus)
        {
            // switch (uploadStatus)
            // {
            //     case UploadStatus.Uploading:
            //         editGameBtn.gameObject.SetActive(false);
            //         copyBtn.gameObject.SetActive(false);
            //         deleteBtn.gameObject.SetActive(false);
            //         publishBtn.gameObject.SetActive(false);
            //         editInfoBtn.gameObject.SetActive(false);
            //         break;
            //     case UploadStatus.UploadFail:
            //         editGameBtn.gameObject.SetActive(false);
            //         copyBtn.gameObject.SetActive(false);
            //         deleteBtn.gameObject.SetActive(false);
            //         publishBtn.gameObject.SetActive(false);
            //         editInfoBtn.gameObject.SetActive(false);
            //         break;
            //     default:
            //         editGameBtn.gameObject.SetActive(true);
            //         copyBtn.gameObject.SetActive(true);
            //         deleteBtn.gameObject.SetActive(true);
            //         publishBtn.gameObject.SetActive(true);
            //         editInfoBtn.gameObject.SetActive(true);
            //         break;
            // }
        }

        /// <summary>
        /// 销毁UI
        /// </summary>
        public void DestroyUI()
        {
        }


        public void SetAction(Action<DraftListItem> editInfoAction,
            Action<DraftListItem> copyGameAction,
            Action<DraftListItem> deleteGameAction,
            Action<bool> publishGameAction,
            Action<bool> updateGameAction,
            Action<DraftListItem> reuploadAction)
        {
            this.editInfoAction = editInfoAction;
            this.copyGameAction = copyGameAction;
            this.deleteGameAction = deleteGameAction;
            this.publishGameAction = publishGameAction;
            this.updateGameAction = updateGameAction;
            this.reuploadAction = reuploadAction;
        }

        public void SetChangeViewAction(Action<StudioSubType> act)
        {
            _changeViewAction = act;
        }
    }
}
