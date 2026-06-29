using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class AnimPropToolPanel : BasePanel<AnimPropToolPanel>
    {
        [SerializeField] private PoseMyCreatePropOSAView createView;
        [SerializeField] private PoseBuyPropOSAView buyView;
        [SerializeField] private Button doneButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private MISource miSourceUI;
        private Action<PropInfo> OnSelectProp;
        private PropInfo selectPropInfo;
        private MISource.Source curSource = MISource.Source.Bud;

        public override void OnCreate()
        {
            base.OnCreate();
            miSourceUI.SetCallback(OnValueChange);
            doneButton.onClick.AddListener(OnDoneClick);
            closeButton.onClick.AddListener(CloseSelf);
            createView.OnSelectProp = SelectPropClick;
            buyView.OnSelectProp = SelectPropClick;
            doneButton.interactable = false;
        }

        public void SelectPropClick(PropInfo propInfo)
        {
            selectPropInfo = propInfo;
            doneButton.interactable = true;
        }

        public void SetSelectPropClick(Action<PropInfo> select)
        {
            OnSelectProp = select;
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            createView.OnCreate();
            buyView.OnCreate();
            buyView.gameObject.SetActive(false);
            miSourceUI.DefualtOn(curSource);
        }
        
        private void OnValueChange(MISource.Source source)
        {
            createView.gameObject.SetActive(source == MISource.Source.Bud);
            buyView.gameObject.SetActive(source != MISource.Source.Bud);
        }


        private void OnDoneClick()
        {
            OnSelectProp?.Invoke(selectPropInfo);
            CloseSelf();
        }
    }

}