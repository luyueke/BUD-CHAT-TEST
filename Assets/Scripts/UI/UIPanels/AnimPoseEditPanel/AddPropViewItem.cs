using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

namespace BUD.AnimPose
{
    public class AddPropViewItem : MonoBehaviour
    {
        [Header("远程Icon")] 
        [SerializeField]private RemoteImageBehaviour remoteIcon;
        [SerializeField]private Transform loadingImg;
        [SerializeField]private GameObject selectIcon;
        [SerializeField]private CButton addButton;
        [SerializeField]private CButton iconButton;
        [SerializeField]private GameObject vipIcon;
        public PropAnimIK propIk { set; get; }
        private Action<AddPropViewItem> onSelect;
        public AnimPropData curPropData { get; private set; }

        public void UpdateData(bool isVip,AnimPropData data,Action<AddPropViewItem> select)
        {
            onSelect = select;
            curPropData = data;
            vipIcon.SetActive(isVip);
            addButton.onClick.AddListener(OnAddPropClick);
            iconButton.onClick.AddListener(OnPropClick);
            if (data != null && !string.IsNullOrEmpty(data.id))
            {
                addButton.gameObject.SetActive(false);
                iconButton.gameObject.SetActive(true);
                SetIcon(data.cover);
            }
            else
            {
                addButton.gameObject.SetActive(true);
                iconButton.gameObject.SetActive(false);
            }
        }


        private void OnAddPropClick()
        {
            var propPanel = UIManager.Inst.OpenPanel<AnimPropToolPanel>(PanelId.AnimPropToolPanel);
            propPanel.SetSelectPropClick(AddPropClick);
        }


        private void OnPropClick()
        {
            if (propIk == null)
            {
                LoggerUtils.LogError("Exception propIK is null");
                return;
            }
            propIk.gameObject.SetActive(true);
            onSelect?.Invoke(this);
        }

        public void RemovePropNode()
        {
            SetSelectIconVisible(false);
        }

        public void AddPropClick(PropInfo info)
        {
            curPropData.id = info.id;
            curPropData.cover = info.cover;
            curPropData.metaDataUrl = info.metaDataUrl;
            SetIcon(info.cover);
            var curIkController = AnimPoseEditPanel.Creater.GetCurrentIkController();
            var curAnimType = AnimPoseEditPanel.Creater.curAnimType;
            propIk = curIkController.CreatePropIK(curAnimType,curPropData);
            onSelect?.Invoke(this);
        }
        
        
        public void ChangePropClick(PropInfo info,PropAnimIK replaceIK)
        {
            curPropData.id = info.id;
            curPropData.cover = info.cover;
            curPropData.metaDataUrl = info.metaDataUrl;
            SetIcon(info.cover);
            var curIkController = AnimPoseEditPanel.Creater.GetCurrentIkController();
            var curAnimType = AnimPoseEditPanel.Creater.curAnimType;
            curIkController.RemovePropIK(replaceIK);
            propIk = curIkController.CreatePropIK(curAnimType,curPropData);
            var propTransform = propIk.transform;
            var replaceTransform = replaceIK.transform;
            propTransform.SetParent(replaceTransform.parent);
            propTransform.localPosition = replaceTransform.localPosition;
            propTransform.localRotation = replaceTransform.localRotation;
            propTransform.localScale = replaceTransform.localScale;
           
            GameObject.Destroy(replaceIK.gameObject);
            onSelect?.Invoke(this);
        }

        public void SetIcon(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                LoggerUtils.LogError($"GameIconLoadItem.LoadIcon error.(url is empty)");
                return;
            }
            addButton.gameObject.SetActive(false);
            iconButton.gameObject.SetActive(true);
            ShowLoading(); 
            remoteIcon.Load(url, true, OnLoadComplete);
        }


        public void RemovePropIK()
        {
            curPropData.id = null;
            curPropData.cover = null;
            curPropData.metaDataUrl = null;
            curPropData.bindIndex = 0;
            propIk = null;
            addButton.gameObject.SetActive(true);
            iconButton.gameObject.SetActive(false);
            remoteIcon.RawImage.texture = remoteIcon._LoadingTexture;
        }
        
        void OnLoadComplete(bool fromCache, bool success)
        {
            HideLoading();
        }
        

        public void SetLoadingVisible(bool value)
        {
            loadingImg.gameObject.SetActive(value);
        }

        public void ShowLoading()
        {
            SetLoadingVisible(true);
        }

        public void HideLoading()
        {
            SetLoadingVisible(false);
        }

        public void SetSelectIconVisible(bool isVisible)
        {
            selectIcon.SetActive(isVisible);
        }
    }
}