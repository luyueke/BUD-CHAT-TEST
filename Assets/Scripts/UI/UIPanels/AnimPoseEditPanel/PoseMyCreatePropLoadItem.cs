using System;
using System.Drawing;
using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class PoseMyCreatePropLoadItem : MonoBehaviour
    {
        [Header("远程Icon")] 
        [SerializeField] private RemoteImageBehaviour remoteIcon;
        [SerializeField] private Transform loadingImg;
        [SerializeField] private GameObject selectIcon;
        [SerializeField] private Button selectBtn;
        private Action<PoseCreateItemData> onItemSelected;
        private PoseCreateItemData mData;

        private void Awake()
        {
            selectBtn.onClick.AddListener(OnItemClick);
        }

        private void OnItemClick()
        {
            onItemSelected?.Invoke(mData);
        }
        
        public void SetIcon(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                LoggerUtils.LogError($"GameIconLoadItem.LoadIcon error.(url is empty)");
                return;
            }
            
            ShowLoading();
            remoteIcon.Load(url, true, OnLoadComplete);
        }

        public void UpdateViews(PoseCreateItemData poseData, Action<PoseCreateItemData> action)
        {
            onItemSelected = action;
            mData = poseData;
            selectIcon.SetActive(poseData.Selected);
            SetIcon(poseData.propInfo.cover);
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
    }
}