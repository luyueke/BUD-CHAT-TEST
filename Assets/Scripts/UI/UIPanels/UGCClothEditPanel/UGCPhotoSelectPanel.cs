using Newtonsoft.Json;
using System;
using GameData;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public class UGCPhotoSelectPanel : MonoBehaviour
    {
        public Button album;
        public Button phoneLibrary;
        public Button closeBtn;
        private UGCPhotoBehaviour curBehav;

        void Start()
        {
            album.onClick.AddListener(OnAlbumClick);
            phoneLibrary.onClick.AddListener(OnPhoneLibraryClick);
            closeBtn.onClick.AddListener(ClosePanel);
        }

        public void OnAlbumClick()
        {
#if UNITY_EDITOR
            TestButton(SavePhotoType.CheckInPhoto);
#endif
            ClosePanel();
        }

        public void OnPhoneLibraryClick()
        {
#if UNITY_EDITOR
            TestButton(SavePhotoType.SystemPhoto);
#else
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.getMediaFromNative, OnGetMediaFromNative);
            MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.getMediaFromNative, OnGetMediaFromNativeFail);
            var jb = new JObject
            {
                ["mediaType"] = 0,
                ["needReview"] = 0,
            };
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.getMediaFromNative, JsonConvert.SerializeObject(jb));
#endif
            ClosePanel();
        }
        
        void OnGetMediaFromNative(string msg)
        {
            LoggerUtils.Log($"ShotPhotoSubView.OnGetMediaFromNative {msg}");
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.getMediaFromNative);
            var mobileData = JsonConvert.DeserializeObject<SavingData.MobileMediaFromNativeData>(msg);
            var remoteUrl = mobileData.remoteUrl;
            if (string.IsNullOrEmpty(mobileData.remoteUrl))
            {
                TipPanel.ShowToast("导入图片失败， 请再试一遍!");
                return;
            }
            curBehav.data.photoUrl = remoteUrl;
            curBehav.OpenLoader();
            curBehav.LoadPhoto();
        }
        
        void OnGetMediaFromNativeFail(string msg)
        {
            LoggerUtils.Log($"ShotPhotoSubView.OnGetMediaFromNativeFail {msg}");
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.getMediaFromNative);
        }

        private void TestButton(SavePhotoType type)
        {
            string content = String.Empty;
            string urlR = "https://i.ibb.co/RcQ3pJP/v2-3942bc6160c1cbc84216731fe935f9f4-1440w.jpg";
            string urlL = "https://cdn.joinbudapp.com/TestFolder/UgcImage/_1644358089.jpg";
            var photoBehaviour = UGCImportPhotoManager.Inst.CurrentSelectBehaviour as UGCPhotoBehaviour;
            photoBehaviour.photoType = type;
            string remoteUrl = string.Empty;
            if (type == SavePhotoType.CheckInPhoto)
            {
                remoteUrl = urlL;
            }
            else
            {
                remoteUrl = urlR;
            }

            if (string.IsNullOrEmpty(remoteUrl))
            {
                TipPanel.ShowToast("Oops! Something went wrong. Please try again!");
                return;
            }
            curBehav.data.photoUrl = remoteUrl;
            curBehav.OpenLoader();
            curBehav.LoadPhoto();
        }

        public void ShowPanel(UGCPhotoBehaviour behav)
        {
            curBehav = behav;
            gameObject.SetActive(true);
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}