using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class AddAnimPosePanel : BasePanel<AddAnimPosePanel>
    {
        [SerializeField] private PoseMISource miSourceUI;
        public Button CloseBtn;
        public CButton DoneBtn;
        public FreePoseOSAView FreePoseView;
        public QuickPoseOSAView QuickPoseView;
        public CreatePoseOSAView CreatePoseView;
        public BuyPoseOSAView BuyPoseView;
        private Dictionary<PoseMISource.Source, BasePoseOSAView> allViews;
        public Action<string> OnSelectPoseClick;
        private string curPoseData = string.Empty;
        private PoseMISource.Source curSource = PoseMISource.Source.Free;
        private UgcPoseSubType poseSubType;
        public override void OnCreate()
        {
            base.OnCreate();
            CloseBtn.onClick.AddListener(OnClose);
            allViews = new Dictionary<PoseMISource.Source, BasePoseOSAView>();
            allViews.Add(PoseMISource.Source.Free,FreePoseView);
            allViews.Add(PoseMISource.Source.Quick,QuickPoseView);
            allViews.Add(PoseMISource.Source.Create,CreatePoseView);
            allViews.Add(PoseMISource.Source.Buy,BuyPoseView);
            miSourceUI.SetCallback(OnValueChange);
            DoneBtn.interactable = false;
            DoneBtn.onClick.AddListener(OnDoneClick);
        }

        public void SetRefreshSlot(Action<int,int> refresh)
        {
            QuickPoseView.OnRefreshSlot = refresh;
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            poseSubType = (UgcPoseSubType)args[0];
            OnStartView();
            miSourceUI.DefualtOn(curSource);
        }

        private void OnSelectPose(string poseData)
        {
            curPoseData = poseData;
            DoneBtn.interactable = !string.IsNullOrEmpty(curPoseData);
        }

        private void OnDoneClick()
        {
            OnSelectPoseClick?.Invoke(curPoseData);
            CloseSelf();
        }

        private void OnStartView()
        {
            foreach (var keyValue in allViews)
            {
                keyValue.Value.OnStart(poseSubType);
                keyValue.Value.OnSelectPoseClick = OnSelectPose;
            }
        }

        private void OnValueChange(PoseMISource.Source source)
        {
            foreach (var keyValue in allViews)
            {
                keyValue.Value.gameObject.SetActive(false);
            }

            if (curSource != source)
            {
                OnSelectPose(string.Empty);
            }

            allViews[source].gameObject.SetActive(true);
            allViews[source].OnUpdate();
        }

        private void OnClose()
        {
            CloseSelf();
        }
    }
}
