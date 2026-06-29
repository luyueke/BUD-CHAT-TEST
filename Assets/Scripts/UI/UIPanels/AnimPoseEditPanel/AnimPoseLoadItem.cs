using System;
using System.Drawing;
using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class AnimPoseLoadItem : MonoBehaviour
    {
        [Header("远程Icon")] 
        [SerializeField] private RemoteImageBehaviour remoteIcon;
        [SerializeField] private GameObject selectIcon;
        [SerializeField] private Button selectBtn;
        [SerializeField] private GameObject Go_VipTag;
        private Action<QuickPoseData> onItemSelected;
        private QuickPoseData mData;

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
            
            // ShowLoading();
            remoteIcon.Load(url, true, null);
        }

        public void UpdateViews(QuickPoseData poseData, Action<QuickPoseData> action)
        {
            onItemSelected = action;
            mData = poseData;
            selectIcon.SetActive(poseData.Selected);
            SetIcon(poseData.poseInfo.textureUrl);
            Go_VipTag.SetActive(poseData.poseInfo.isVip == 1);
        }

      
    }
}