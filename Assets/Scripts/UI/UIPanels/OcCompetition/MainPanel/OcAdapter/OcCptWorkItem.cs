using Com.TheFallenGames.OSA.Util.IO;
using GameData.UGCData;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCptWorkItem : MonoBehaviour
    {
        //图标
        public RemoteImageBehaviour RemoteImage;
        //票数
        public Transform VoteBg;
        public Text VoteTxt;
        //投票
        public Button VoteBtn;
        //举报
        public Button ReportBtn;
        //编辑
        public Button EditBtn;
        //购买
        public Button BuyBtn;
        public Text PriceTxt;
        //穿上
        public Button PutBtn;
        //穿上
        public Button DetailBtn;
        //穿上
        public Button HeadBtn;
        //作者
        public HeadViewWidget HeadView;
        //编辑2
        public Button EditBtn2;

        [HideInInspector] public OcCptListMsgItem data;
        private void Awake()
        {
            EditBtn.onClick.AddListener(OnEditBtn);
            if(EditBtn2 != null) EditBtn2.onClick.AddListener(OnEditBtn2);
            PutBtn.onClick.AddListener(OnPutBtn);
            BuyBtn.onClick.AddListener(OnBuyBtn);
            ReportBtn.onClick.AddListener(OnReportBtn);
            VoteBtn.onClick.AddListener(OnVoteBtn);
            DetailBtn.onClick.AddListener(OnDetailBtn);
            if (HeadBtn != null) HeadBtn.onClick.AddListener(OnHead);
        }

        public void SetData(OcCptListMsgItem _data) 
        {
            data = _data;

            RemoteImage.Load(data.creationInfo.cover);
            VoteTxt.text = data.scoreInfo.score + "票";

            if(data.creationInfo.paymentInfo != null) PriceTxt.text = data.creationInfo.paymentInfo.price.ToString();

            if (HeadView != null) HeadView.InitHeadCycle(data.creator.uid, data.creator.portraitUrl,data.creator.avatarFrame);
        }

        private void OnEditBtn()
        {
            if (data == null)
            {
                return;
            }
            var panel = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
            panel.MyGroup.AvatarGroup.Clear();
            panel.MyGroup.AvatarGroup.setType = 1;
            panel.MyGroup.AvatarGroup.oldCreation = data.creationInfo.ocInfo.baseInfo.ocId;
            panel.MyGroup.OcGroup.OcInfo = data.creationInfo.ocInfo.baseInfo;
            panel.MyGroup.PoseGroup.PoseInfo = data.creationInfo.poseInfo;
            panel.MyGroup.SetStep(OcCompetitionStep.Oc);
        }
        private void OnEditBtn2()
        {
            if (data == null)
            {
                return;
            }
            var panel = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
            panel.MyGroup.SingleGroup.ItemData = data;
            panel.MyGroup.SetStep(OcCompetitionStep.Single);
        }
        private void OnPutBtn()
        {
            var panel = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
            panel.MyGroup.SingleGroup.PutOn();
        }
        private void OnBuyBtn()
        {
            var panel = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
            panel.MyGroup.SingleGroup.BuyAll();
        }
        private void OnReportBtn()
        {
            if (data == null)
            {
                return;
            }
            var _reportReq = new ErrReportReq()
            {
                Uid = data.creator.uid,
                bizId = data.creationInfo.creationId,
                scenesType = (int)ErrReportSceneType.OC,
                contestId = OcCompetitionSystem.Inst.data.ContestInfo.contestId
            };
            UIManager.Inst.OpenPanel(PanelId.ReportAssetPanel, _reportReq);
        }

        private void OnVoteBtn()
        {
            if (data == null)
            {
                return;
            }
            OcCompetitionSystem.Inst.OcVote(OcCompetitionSystem.Inst.data.ContestInfo.contestId,data.creationInfo.creationId);
        }

        private void OnDetailBtn()
        {
            if (data == null)
            {
                return;
            }
            var panel = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
            panel.TogMy.isOn = true;
            panel.MyGroup.SingleGroup.ItemData = data;
            panel.MyGroup.SetStep(OcCompetitionStep.Single);
        }

        void OnHead()
        {
            if (data == null)
            {
                return;
            }
            UIManager.Inst.SwapPanel(PanelId.ProfilePanel, data.creator.uid);
        }
    }
}