using System;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace BUD.AnimPose
{
    public class PoseBuyPropLoadItem : MonoBehaviour
    {
        [Header("远程Icon")] 
        [SerializeField] private RemoteImageBehaviour remoteIcon;
        [SerializeField] private Transform loadingImg;
        [SerializeField] private GameObject selectIcon;
        [SerializeField] private Button selectBtn;
        [SerializeField] private GameObject storeIcon;
        private Action<PoseBuyItemData> onItemSelected;
        private PoseBuyItemData mData;

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

        public void UpdateViews(PoseBuyItemData poseData, Action<PoseBuyItemData> action)
        {
            onItemSelected = action;
            mData = poseData;
            if (poseData.isStore)
            {
                remoteIcon.RawImage.color = new Color(0,0,0,0);
                storeIcon.gameObject.SetActive(true);
                return;
            }
            remoteIcon.RawImage.color = Color.white;
            storeIcon.gameObject.SetActive(false);
            selectIcon.SetActive(poseData.Selected);
            SetIcon(poseData.ugcInfo.cover);
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