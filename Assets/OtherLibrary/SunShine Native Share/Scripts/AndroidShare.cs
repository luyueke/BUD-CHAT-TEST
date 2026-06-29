using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class AndroidShare : MonoBehaviour, INativeShare
    {
        private static AndroidJavaObject _share;

        private void Start()
        {
            SetUpShare();
        }

        private static void SetUpShare()
        {
            if (_share == null)
            {
                _share = new AndroidJavaObject("com.SmileSoft.unityplugin.Share.ShareFragment");

                string fileprovider = GetAndroidPackageName();

                Debug.Log($"AndroidShare fileprovider={fileprovider}");

                _share.Call("SetUp", fileprovider, "NativeShare", "ShareCallback");
            }

        }

        private static string GetAndroidPackageName()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");        
        return currentActivity.Call<string>("getPackageName")+".fileprovider";
#else
            return SunShineNativeShare.fileProviderName;
#endif
        }

        public void ShareMultipleFileOfSameType(string[] path, string fileType, string message, string shareDialogTitle)
        {
            SetUpShare();
            _share.Call("ShareMultipleFileOfSameFileType", path, fileType, message, shareDialogTitle);
        }

        public void ShareSingleFile(string path, string fileType, string message, string shareDialogTitle)
        {
            SetUpShare();
            _share.Call("ShareSingleFile", path, fileType, message, shareDialogTitle);
        }

        public void ShareText(string message, string shareDialogTitle)
        {
            SetUpShare();
            _share.Call("ShareText", message, shareDialogTitle);
        }

        public void ShareMultipleFileOfMultipleType(string[] path, string message, string shareDialogTitle)
        {
            ShareMultipleFileOfSameType(path, SunShineNativeShare.TYPE_FILE, message, shareDialogTitle);
        }
    }
}