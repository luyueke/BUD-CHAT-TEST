using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    public class MyKeyBoardTest : MonoBehaviour
    {
        [SerializeField] public MyInputField InputField;

        [HideInInspector]public TouchScreenKeyboard keyboard;

        AndroidJavaClass unityClass;
        AndroidJavaObject currentActivity;
        AndroidJavaObject unityPlayer;

        [HideInInspector] public Action<string> inputAction;

        [HideInInspector] public Action<float> offsetAction;

        float h;

        string txt;
        public TouchScreenKeyboard OpenKeyboard(string text) {
            if (keyboard == null) {
                keyboard = TouchScreenKeyboard.Open("");
                keyboard.text = text;
                txt = text;
            }
            return keyboard;
        }

        public TouchScreenKeyboard Open(string text, TouchScreenKeyboardType keyboardType, bool autocorrection, bool multiline, bool secure, bool alert,string textPlaceholder, int characterLimit)
        {
            if (keyboard == null)
            {
                keyboard = TouchScreenKeyboard.Open(text, keyboardType, autocorrection, multiline, secure, alert, textPlaceholder, characterLimit);
                txt = text;
            }
            return keyboard;
        }

        public void CloseOpenKeyboard() {
            if (keyboard != null)
            {
                keyboard.active = false;
                keyboard = null;
            }
        }

        private void Awake()
        {
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
            if (InputField != null && InputField.touchScreenKeyboard != null && keyboard == null) 
            {
                keyboard = InputField.touchScreenKeyboard;
            }
            if (keyboard != null)
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    if (unityClass == null)
                    {
                        Debug.LogError("unity unityClass is null");
                        return;
                    }
                    if (currentActivity == null)
                    {
                        Debug.LogError("unity currentActivity is null");
                        return;
                    }
                    if (unityPlayer == null)
                    {
                        Debug.LogError("unity unityPlayer is null");
                        return;
                    }
                }

                var _h = GetKeyboardHeight(true, 2436); //* 2436 / Screen.height;
                if (h != _h)
                {
                    h = _h;
                    offsetAction?.Invoke(h);
                }

                if (txt != keyboard.text)
                {
                    txt = keyboard.text;
                    inputAction?.Invoke(txt);
                }
            }
            else
            {
                var _h = 0;
                if (h != _h)
                {
                    h = _h;
                    offsetAction?.Invoke(h);
                }

                if (txt != "")
                {
                    txt = "";
                    inputAction?.Invoke(txt);
                }
            }
        }


        /// <summary>
        /// ��ȡ��׿ƽ̨�ϼ��̵ĸ߶�
        /// </summary>
        /// <returns></returns>
        public int GetKeyboardHeight(bool includeInput, int screenHeight)
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                //Debug.LogError("keyboard1 " + TouchScreenKeyboard.area.height);
                var val = (int)TouchScreenKeyboard.area.height * screenHeight / GetViewHeight();
                //Debug.LogError("keyboard2 " + val);
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
                    int androidRectH = rect.Call<int>("height"); // ���ǷǼ��̲��ֵĸ߶ȡ�       
                    int h = Math.Abs(GetViewHeight() - androidRectH); // �����̸߶�         
                    int keyboardHeight = h + decorHeight;
                    int finalres = keyboardHeight * screenHeight / GetViewHeight();
                    //Debug.LogFormat(
                    //    "���찲׿ƽ̨��⵽���̸߶ȣ�{0},Screen.height: {1},�Ǽ��̲��ֵĸ߶�: {2},�����ĸ߶�: {3},UIʵ�ʸ߶�: {4},���ռ��̸߶�: {5},��׿����ʾ�߶�: {6}",
                    //    keyboardHeight,
                    //    Screen.height, androidRectH, decorHeight, screenHeight, finalres, GetViewHeight());
                    return finalres;
                }
            }
            return 0;
        }

        /// <summary>
        /// ��ȡ��Ļ�߶�
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
            //Debug.LogFormat("Screen.height: {0},��Ļ�ֱ���: {1}", Screen.height, result);
            return result;
#endif
        }
    }


}