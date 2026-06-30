using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    public class KeyBoardTest : MonoBehaviour
    {
        [SerializeField] public InputField InputField;

        [HideInInspector]public TouchScreenKeyboard keyboard;

        AndroidJavaClass unityClass;
        AndroidJavaObject currentActivity;
        AndroidJavaObject unityPlayer;
        bool unityPlayerFetchAttempted;

        [HideInInspector] public Action<string> inputAction;

        [HideInInspector] public Action<float> offsetAction;

        // 键盘被系统收起（按缩回/Done/Back）时触发
        [HideInInspector] public Action onKeyboardHidden;
#if PACKAGE_TYPE_US
    static readonly string androidClassPath = "com.pointone.buddyglobal.feature.unity.view.UnityPlayerActivity";
#else
    static readonly string androidClassPath = "cn.budapp.biyoudideshijie.feature.unity.view.UnityPlayerActivity";
#endif
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
                }
            }

            TouchScreenKeyboard.hideInput = true;
        }

        private AndroidJavaObject GetUnityPlayer()
        {
#if UNITY_STANDARD_BUILD
            return null;
#endif
            if (!unityPlayerFetchAttempted)
            {
                unityPlayerFetchAttempted = true;
                try
                {
                    unityPlayer = currentActivity?.Get<AndroidJavaObject>("mUnityPlayer");
                }
                catch (Exception)
                {
                    // Custom host Activity doesn't expose mUnityPlayer as a field.
                    // getGameView() returns the UnityPlayer instance (UnityPlayer extends FrameLayout),
                    // so JNI can still access UnityPlayer fields/methods on the returned object.
                    try
                    {
                        unityPlayer = currentActivity?.Call<AndroidJavaObject>("getGameView");
                    }
                    catch (Exception e2)
                    {
                        Debug.LogWarning("[KeyBoardTest] Could not get UnityPlayer via getGameView: " + e2.Message);
                    }
                }
            }
            return unityPlayer;
        }

        void Update()
        {
            // 仅在键盘真正可见时才从 InputField 同步引用，避免已关闭的实例被重新赋入
            if (InputField != null && InputField.touchScreenKeyboard != null && keyboard == null
                && InputField.touchScreenKeyboard.status == TouchScreenKeyboard.Status.Visible)
            {
                keyboard = InputField.touchScreenKeyboard;
            }

            if (keyboard != null)
            {
                // 用户按系统收起/Done/Back 等导致键盘关闭时，主动清空引用并通知上层
                if (keyboard.status != TouchScreenKeyboard.Status.Visible)
                {
                    keyboard = null;
                    onKeyboardHidden?.Invoke();
                    // h 会在下方 else 分支里重置为 0
                }
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
                    // mUnityPlayer may not exist in custom Activity; GetKeyboardHeight has a fallback
                }

                var _h = GetKeyboardHeight(true, 2436);
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
                if (currentActivity == null) return 0;

                var player = GetUnityPlayer();
                AndroidJavaObject view = null;
                var decorHeight = 0;

                if (player != null)
                {
                    view = player.Call<AndroidJavaObject>("getView");
                    if (includeInput)
                    {
                        try
                        {
                            var dialog = player.Get<AndroidJavaObject>("mSoftInputDialog");
                            if (dialog != null)
                            {
                                var dv = dialog.Call<AndroidJavaObject>("getWindow")?.Call<AndroidJavaObject>("getDecorView");
                                if (dv != null) decorHeight = dv.Call<int>("getHeight");
                            }
                        }
                        catch { }
                    }
                }

                // Fallback when mUnityPlayer is not exposed by the host Activity
                if (view == null)
                {
                    try
                    {
                        view = currentActivity.Call<AndroidJavaObject>("getWindow")
                            ?.Call<AndroidJavaObject>("getDecorView");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[KeyBoardTest] GetKeyboardHeight decorView fallback failed: " + e.Message);
                        return 0;
                    }
                }

                if (view == null) return 0;

                int viewH = GetViewHeight();
                if (viewH == 0) return 0;

                using (var rect = new AndroidJavaObject("android.graphics.Rect"))
                {
                    view.Call("getWindowVisibleDisplayFrame", rect);
                    int androidRectH = rect.Call<int>("height");
                    int h = Math.Abs(viewH - androidRectH);
                    int keyboardHeight = h + decorHeight;
                    return keyboardHeight * screenHeight / viewH;
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
            var player = GetUnityPlayer();
            if (player != null)
            {
                var view = player.Call<AndroidJavaObject>("getView");
                if (view != null) return view.Call<int>("getHeight");
            }
            // Fallback when mUnityPlayer is not exposed
            try
            {
                var decorView = currentActivity?.Call<AndroidJavaObject>("getWindow")
                    ?.Call<AndroidJavaObject>("getDecorView");
                if (decorView != null) return decorView.Call<int>("getHeight");
            }
            catch { }
            return Screen.height;
#endif
        }
    }


}