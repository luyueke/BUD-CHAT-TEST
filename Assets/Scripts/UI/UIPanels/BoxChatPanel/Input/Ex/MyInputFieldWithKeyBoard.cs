using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game
{
    [RequireComponent(typeof(MyInputField))]
    public class MyInputFieldWithKeyBoard : MonoBehaviour
    {
        [Header("UI组件")]
        public RectTransform panelRectTransform;
        public Action<string> extraOnInputAction;
        public Action<float> extraOnOffsetAction;
        public float duration = 0.3f;
        MyInputField inputField;
        Button InputFieldBtn;
        ImagePointTrigger ImagePointTrigger;
        float key_h = 0;
        Tweener positionTweener; // 用于保存当前的位置动画

        public int currentKeyBoardHeight = 0;

        public void Awake()
        {
            duration = 0.3f;
            inputField = this.GetComponent<MyInputField>();
            inputField.caretBlinkRate = 1;
            inputField.caretWidth = 5;
            inputField.shouldHideMobileInput = true;
            // createInputBtn();
            // createImagePointTrigger();
#if UNITY_EDITOR
            // ImagePointTrigger.gameObject.SetActive(false);
            // InputFieldBtn.gameObject.SetActive(false);
            // inputField.onValueChanged.AddListener(OnInputFieldChanged);
#else
            // ImagePointTrigger.gameObject.SetActive(false);
            // InputFieldBtn.gameObject.SetActive(true);
            // InputFieldBtn.onClick.AddListener(OnInputBtn);
#endif
            // keyBoardTest = gameObject.AddComponent<MyKeyBoardTest>();
            // keyBoardTest.InputField = inputField;
            // keyBoardTest.offsetAction = OnOffsetAction;
            // keyBoardTest.inputAction = OnInputAction;
        }

        void Update()
        {
            int keyboardHeight = inputField.GetKeyboardHeight();
#if UNITY_EDITOR
            keyboardHeight = currentKeyBoardHeight;
#else
            currentKeyBoardHeight = keyboardHeight;
#endif
            if (keyboardHeight != key_h)
            {
                key_h = keyboardHeight;
                OnOffsetAction(key_h);
            }
        }

        void OnInputFieldChanged(string text)
        {
            extraOnInputAction?.Invoke(text);
        }

        void createInputBtn()
        {
            // var trans = this.inputField.transform.Find("InputFieldBtn");
            // if (trans == null)
            // {
            //     var rectTrans = new GameObject("InputFieldBtn").AddComponent<RectTransform>();
            //     trans = rectTrans.transform;
            //     trans.SetParent(this.inputField.transform);
            //     trans.localPosition = Vector3.zero;
            //     trans.localRotation = Quaternion.identity;
            //     trans.localScale = Vector3.one;
            //     var srcRect = this.inputField.GetComponent<RectTransform>();
            //     rectTrans.sizeDelta = srcRect.sizeDelta;
            //     rectTrans.anchorMin = new(0, 0);
            //     rectTrans.anchorMax = new(1, 1);
            //     rectTrans.pivot = new Vector2(0.5f, 0.5f);
            //     rectTrans.anchoredPosition = new Vector2(0, 0);
            //     rectTrans.offsetMin = new Vector2(0, 0);
            //     rectTrans.offsetMax = new Vector2(0, 0);

            //     var emptyImage = trans.gameObject.AddComponent<EmptyImage>();
            //     InputFieldBtn = trans.gameObject.AddComponent<Button>();
            //     InputFieldBtn.targetGraphic = emptyImage;
            // }
            // else
            // {
            //     InputFieldBtn = trans.gameObject.GetComponent<Button>();
            // }
        }

        void createImagePointTrigger()
        {
            // Transform trans = this.transform.Find("ImagePointTrigger");
            // RectTransform rectTrans = null;
            // if (trans == null)
            // {
            //     rectTrans = new GameObject("ImagePointTrigger").AddComponent<RectTransform>();
            //     trans = rectTrans.transform;
            //     var LayoutElement = trans.gameObject.AddComponent<LayoutElement>();
            //     LayoutElement.ignoreLayout = true;
            //     int siblingIndex = transform.GetSiblingIndex();
            //     trans.SetParent(this.transform.parent);
            //     trans.SetSiblingIndex(siblingIndex);
            //     trans.localPosition = Vector3.zero;
            //     trans.localRotation = Quaternion.identity;
            //     trans.localScale = Vector3.one;
            //     trans.gameObject.AddComponent<EmptyImage>();
            //     ImagePointTrigger = trans.gameObject.AddComponent<ImagePointTrigger>();
            // }
            // else
            // {
            //     ImagePointTrigger = transform.gameObject.GetComponent<ImagePointTrigger>();
            //     rectTrans = transform.GetComponent<RectTransform>();
            // }
            // ImagePointTrigger.OnPointDown = OnPointDown;
            // rectTrans.sizeDelta = new Vector2(3000, 4000);
        }

        void OnPointDown()
        {
            CloseKeyboard();
        }

        void OnInputBtn()
        {
           
        }

        public void CloseKeyboard()
        {
            // #if !UNITY_EDITOR
            //             keyBoardTest.CloseOpenKeyboard();
            //             InputFieldBtn.gameObject.SetActive(true);
            //             ImagePointTrigger.gameObject.SetActive(false);
            // #endif

        }

        void OnOffsetAction(float h)
        {
            key_h = h;

            // 停止之前的动画（如果有）
            if (positionTweener != null && positionTweener.IsActive())
            {
                positionTweener.Kill();
            }

            // 使用 DOTween 插值到目标位置
            // if (panelRectTransform.anchoredPosition.y < key_h)
            {
                positionTweener = panelRectTransform.DOAnchorPosY(key_h, duration)
    .SetEase(Ease.Linear); // 使用缓出曲线，也可以根据需要改为其他缓动函数
            }


            extraOnOffsetAction?.Invoke(h);
        }

        void OnInputAction(string text)
        {
           
        }

        void OnDestroy()
        {
            // 清理动画，避免内存泄漏
            if (positionTweener != null && positionTweener.IsActive())
            {
                positionTweener.Kill();
            }
        }

      


    }

}