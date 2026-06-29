using Com.TheFallenGames.OSA.Util.IO;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GEParkDetailPanel : BasePanel<GEParkDetailPanel>
    {
        public Button CloseBtn;
        public CButton copyIdBtn;
        public Button HeadBtn;
        public Text NameText;
        public Text PlotText;
        public List<GEParkNpcHead> NpcList;
        public RemoteImageBehaviour RemoteImage;

        public RemoteImageBehaviour CreatHead;
        public Text CreatName;
        public Text CreatID;
        public Image CreatFrame;
        public Transform Trans_BottomEffect;
        public Transform Trans_TopEffect;
        public FollowButton FollowBtn;
        public LikeButton LikeBtn;
        public FavoritesButton SelectedBtn;
        public ShareButton ShareBtn;
        public EnergyCoinButton EnergyCoinBtn;
        public Button DeleteBtn;
        public Button EditorBtn;

        public Button ChooseBtn;
        public ReportButton _reportButton;


        [HideInInspector] public UgcInfoRsp UgcInfoRsp;
        public override void OnCreate()
        {
            base.OnCreate();

            copyIdBtn.onClick.AddListener(OnNickCopyIdClick);
            HeadBtn.onClick.AddListener(OnHead);
            CloseBtn.onClick.AddListener(CloseSelf);
            ChooseBtn.onClick.AddListener(OnChooseBtn);
            DeleteBtn.onClick.AddListener(OnDeleteBtn);
            EditorBtn.onClick.AddListener(OnEditorBtn);
        }


        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            UgcInfoRsp = (UgcInfoRsp)args[0];

            if (args.Length >= 2)
            {
                DeleteBtn.gameObject.SetActive((bool)args[1]);
            }
            else
            {
                DeleteBtn.gameObject.SetActive(false);
            }

            if (args.Length >= 3)
            {
                EditorBtn.gameObject.SetActive((bool)args[2]);
               
            }
            else
            {
                EditorBtn.gameObject.SetActive(false); 
            }

            _reportButton.SetData(UgcInfoRsp.mapInfo.id, (int)ErrReportSceneType.UgcMap);
            RefreshView();
        }


        public void RefreshView()
        {

            EnergyCoinBtn.SetData(UgcInfoRsp.creator.uid, UgcInfoRsp.mapInfo.id, UgcInfoRsp.interactInfo.rewardAmount);

            ShareBtn.SetData(UgcInfoRsp.creator.uid, UgcInfoRsp.mapInfo.id, UgcInfoRsp.interactInfo.shareAmount);

            LikeBtn.SetData(UgcInfoRsp.mapInfo.id, UgcInfoRsp.interactInfo.liked, UgcInfoRsp.interactInfo.likeAmount);

            FollowBtn.SetRelation(UgcInfoRsp.creator.uid, UgcInfoRsp.relationShipInfo);

            SelectedBtn.SetData(UgcInfoRsp.mapInfo.id, UgcInfoRsp.interactInfo.collected, UgcInfoRsp.interactInfo.collectAmount);

            var map = UgcInfoRsp.mapInfo;
            NameText.text = map.name;
            //PlotText.text = map.gameSetting.AICommonGameConfig.plot;
            PlotText.text = map.desc;
            RemoteImage.Load(map.cover);
            var ls = map.gameSetting.AICommonGameConfig.npcData;
            for (int i = 0; i < NpcList.Count; i++)
            {
                if (i < ls.Count)
                {
                    NpcList[i].gameObject.SetActive(true);
                    NpcList[i].SetDate(ls[i].id, ls[i].cover);
                }
                else
                {
                    NpcList[i].gameObject.SetActive(false);
                }
            }

            var creator = UgcInfoRsp.creator;
            CreatHead.Load(creator.portraitUrl);
            CreatName.text = creator.nickname;
            CreatID.text = "ID：" + creator.username;

            CreatFrame.gameObject.SetActive(false);
            Trans_BottomEffect.ClearChildren();
            Trans_TopEffect.ClearChildren();
            var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(UgcInfoRsp.creator.avatarFrame, this.gameObject);
            if (headCycleData != null)
            {
                CreatFrame.gameObject.SetActive(true);
                CreatFrame.sprite = headCycleData.Sp_HeadCycle;

                if (headCycleData.Effect_Bottom != null)
                {
                    headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
                }

                if (headCycleData.Effect_Top != null)
                {
                    headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
                }
            }
        }

        #region 按钮
        private void OnNickCopyIdClick()
        {
            GUIUtility.systemCopyBuffer = UgcInfoRsp.creator.username;
            TipPanel.ShowToast("已复制ID");
        }
        void OnChooseBtn()
        {
            CloseSelf();

            GameEntrySystem.Inst.CloseParkSelectPanel();
            GameEntrySystem.Inst.CloseParkGroupPanel();
            GameEntrySystem.Inst.CloseParkWorkPanel();

            var p = UIManager.Inst.FindPanel<GameEntryParkPanel>(PanelId.GameEntryParkPanel);
            p.RefreshView(UgcInfoRsp.mapInfo);

            if (BootDataManager.Inst.GetParkStart() == 0)
            {
                UIManager.Inst.OpenPanel(PanelId.BootPanel, 125);
            }
        }

        void OnDeleteBtn() {
            CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetLocalText("确认删除", "你确定删除该作品吗？", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(ConfirmDeleteClick, null);
        }
        void OnEditorBtn()
        {
            var publishMachine = new UGCPublishStateMachine();
            publishMachine.SetStates(new List<UGCPublishStateBase>()
                    {
                        new UGCMapDetailState()
                    });

            var tempMapInfo = UgcInfoRsp.mapInfo.Clone();
            var draftInfo = MapAssetManager.Inst.GetDraftInfo(tempMapInfo);
            if (draftInfo == null)
            {
                draftInfo = new MapDraftInfo(tempMapInfo);
            }

            publishMachine.SetEditData(new MapEditData()
            {
                draftInfo = draftInfo,
                isPublish = false,
                isCondition = false,
            });

            publishMachine.SetFinishCallBack(() =>
            {
                PublishMapAction(true);
                var panel = UIManager.Inst.FindPanel<GEParkWorkPanel>(WindowId.RecommendWindow, PanelId.GEParkWorkPanel);
                if (panel != null)
                {
                    panel.RefreshList();
                }
                CloseSelf();
            });
            publishMachine.SetCancelCallBack(() => { PublishMapAction(false); });
            publishMachine.Start();
        }

        private void PublishMapAction(bool isSuccess)
        {
            //publishGameAction.Invoke(isSuccess);
        }

        private void ConfirmDeleteClick()
        {
            if (UgcInfoRsp != null)
            {
                var req = new SetMapInfoReq
                {
                    mapInfo = UgcInfoRsp.mapInfo,
                    setType = (int)SetType.Delete
                };

                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST,
                    JsonConvert.SerializeObject(req),
                    (content) => {
                        var panel = UIManager.Inst.FindPanel<GEParkWorkPanel>(WindowId.RecommendWindow, PanelId.GEParkWorkPanel);
                        if (panel != null)
                        {
                            panel.RefreshList();
                        }
                        CloseSelf(); 
                    }, null);
            }
        }

        void OnHead() 
        {
            if (UgcInfoRsp != null)
            {
                UIManager.Inst.SwapPanel(PanelId.ProfilePanel, UgcInfoRsp.creator.uid);
            }
        }
        #endregion
    }
}