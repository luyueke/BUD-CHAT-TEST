using Basic.Utils;
using Game.BLE;
using System;
using UnityEngine;
#if !UNITY_IOS
using UnityEngine.Android;
#endif

namespace Game
{
    public class ChatUtils
    {
        public static bool IsChatLimit()
        {
            return false;
            //             if (VipTrialTimeUtil.InTrial())
            //             {
            //                 var c = SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.RealTimeChatLimit);
            //                 Debug.Log("ChatLimit " + c);
            // #if UNITY_IOS
            //                 return c >= 10;
            // #else
            //                 return c >= 5;
            // #endif
            //             }
            //             return false;
        }

        public static void SetChatLimit()
        {
            // if (VipTrialTimeUtil.InTrial())
            {
                if (!IsChatLimit())
                {
                    // var c = SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.RealTimeChatLimit);
                    // c++;
                    // SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.RealTimeChatLimit, c);
                }
            }
        }

        public static void ReqMicrophonePermission(Action ac, Action open)
        {
            if (!GetMicrophonePermission())
            {
                // if (SaveGameUtil.Inst.GetBoolByPlayerPrefs(SaveGameUtil.FirstMicrophone, true))
                // {
                    // SaveGameUtil.Inst.SetBoolByPlayerPrefs(SaveGameUtil.FirstMicrophone, false);
                    RequestMicrophonePermission((bo) =>
                    {
                        if (bo == false)
                        {
                            OpenConfirm(ac);
                        }
                        else
                        {
                            open?.Invoke();
                        }
                    });
                // }
                // else
                // {
                //     OpenConfirm(ac);
                // }
            }
        }

        public static void OpenConfirm(Action ac)
        {
#if UNITY_IOS
                Application.OpenURL("app-settings:");
#else
            OpenAndroidAppSettings();
#endif

            ac?.Invoke();
            //             var tem = new CommonConfirmData();
            //             tem.title = "Microphone Required";
            //             tem.des = "Voice chat needs microphone access.\nAllow access to continue.";
            //             tem.leftTxt = "Go Back";
            //             tem.rightTxt = "Allow";
            //             tem.leftAc = () => { ac?.Invoke(); };
            //             tem.rightAc = () =>
            //             {
            // #if UNITY_IOS
            //                 Application.OpenURL("app-settings:");
            // #else
            //                 OpenAndroidAppSettings();
            // #endif
            //             };
            //             UIManager.Inst.OpenPanel(PanelId.CommonConfirmPanelNew, tem);
        }

        public static bool GetMicrophonePermission()
        {
#if UNITY_IOS
            return Application.HasUserAuthorization(UserAuthorization.Microphone);
#else
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#endif
        }
        public static void RequestMicrophonePermission(Action<bool> action)
        {
            BleSoftwareSideController.RequestMicrophonePermission(action);
        }


        static void OpenAndroidAppSettings()
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    string packageName = currentActivity.Call<string>("getPackageName");

                    using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
                    using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
                    {
                        // Android 8.0及以上版本
                        intent.Call<AndroidJavaObject>("setAction", "android.settings.APPLICATION_DETAILS_SETTINGS");
                        intent.Call<AndroidJavaObject>("setData", AndroidURIFromString("package:" + packageName));
                        intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
                        currentActivity.Call("startActivity", intent);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("跳转设置页面失败: " + e.Message);
            }
        }

        static AndroidJavaObject AndroidURIFromString(string uriString)
        {
            using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
            {
                return uriClass.CallStatic<AndroidJavaObject>("parse", uriString);
            }
        }
    }
}