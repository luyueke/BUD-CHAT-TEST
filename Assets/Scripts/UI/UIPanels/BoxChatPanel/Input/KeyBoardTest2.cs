using Mopsicus.Plugins;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    public class KeyBoardTest2 : MonoBehaviour
    {
        AndroidJavaClass unityClass;
        AndroidJavaObject currentActivity;
        AndroidJavaObject unityPlayer;

        [HideInInspector] public Action<string> inputAction;

        [HideInInspector] public Action<float> offsetAction;

        float h;

        float _h;

        string txt;

        void OnShowKeyboard(bool isShow, int height)
        {
            Debug.LogFormat("Keyboad action, show = {0}, height = {1}", isShow, height);
            _h = height;
        }

        private void Awake()
        {
            MobileInput.OnShowKeyboard += OnShowKeyboard;

            if (Application.platform == RuntimePlatform.Android)
            {
                unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                if (unityClass != null)
                {
                    currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                    if (currentActivity != null)
                    {
                        //unityPlayer = currentActivity.Get<AndroidJavaObject>("mUnityPlayer");
                        unityPlayer = currentActivity.Get<AndroidJavaObject>("gameView");
                        if (unityPlayer == null)
                        {
                            unityPlayer = currentActivity.Call<AndroidJavaObject>("getUnityPlayer");
                        }
                    }
                }
            }

            TouchScreenKeyboard.hideInput = true;
        }

        void Update()
        {
            //if (Application.platform == RuntimePlatform.Android)
            //{
            //    if (unityClass == null)
            //    {
            //        Debug.LogError("unity unityClass is null");
            //        return;
            //    }
            //    if (currentActivity == null)
            //    {
            //        Debug.LogError("unity currentActivity is null");
            //        return;
            //    }
            //    if (unityPlayer == null)
            //    {
            //        Debug.LogError("unity unityPlayer is null");
            //        return;
            //    }
            //}

            //var _h = GetKeyboardHeight(true, 2436); //* 2436 / Screen.height;
            if (h != _h)
            {
                h = _h;
                offsetAction?.Invoke(h);
            }
        }


        /// <summary>
        /// 获取安卓平台上键盘的高度
        /// </summary>
        /// <returns></returns>
        public int GetKeyboardHeight(bool includeInput, int screenHeight)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                //Debug.Log("keyboard1 " + TouchScreenKeyboard.area.height);
                var val = (int)TouchScreenKeyboard.area.height * screenHeight / GetViewHeight();
                //Debug.Log("keyboard2 " + val);
                return val;
            }
            if (Application.platform == RuntimePlatform.Android)
            {
                var view = unityPlayer.Call<AndroidJavaObject>("getView");
                var dialog = unityPlayer.Get<AndroidJavaObject>("mSoftInputDialog");

                if (view == null || dialog == null)
                    return 0;

                var decorHeight = 0;

                if (includeInput)
                {
                    var decorView = dialog.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");

                    if (decorView != null)
                        decorHeight = decorView.Call<int>("getHeight");
                }

                using (var rect = new AndroidJavaObject("android.graphics.Rect"))
                {
                    view.Call("getWindowVisibleDisplayFrame", rect);
                    int androidRectH = rect.Call<int>("height"); // 这是非键盘部分的高度。       
                    int h = Math.Abs(GetViewHeight() - androidRectH); // 软键盘高度         
                    int keyboardHeight = h + decorHeight;
                    int finalres = keyboardHeight * screenHeight / GetViewHeight();
                    //Debug.LogFormat(
                    //    "聊天安卓平台检测到键盘高度：{0},Screen.height: {1},非键盘部分的高度: {2},输入框的高度: {3},UI实际高度: {4},最终键盘高度: {5},安卓的显示高度: {6}",
                    //    keyboardHeight,
                    //    Screen.height, androidRectH, decorHeight, screenHeight, finalres, GetViewHeight());
                    return finalres;
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取屏幕高度
        /// </summary>
        /// <returns></returns>
        public int GetViewHeight()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Screen.height;
#elif UNITY_STANDALONE_OSX || UNITY_IPHONE
        return  Screen.height;
#elif UNITY_ANDROID              
            var view = unityPlayer.Call<AndroidJavaObject>("getView");
            if (view == null)
                return 0;
            int result = view.Call<int>("getHeight");
            //Debug.LogFormat("Screen.height: {0},屏幕分辨率: {1}", Screen.height, result);
            return result;
#endif
        }
    }


}