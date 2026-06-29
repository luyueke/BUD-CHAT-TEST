/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-14 13:43:52
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-27 15:35:50
 * @ Description: 通用的文字输入的组件UI
 */

using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class InputTextSubView : BasePropertyEditSubView
    {
        [Header("业务")]
        [SerializeField]private Text titleText;
        [SerializeField]private Text inputText;
        [SerializeField]private Text limitText;
        [SerializeField]private Button inputButton;
        [SerializeField]private Image contentBgImg;
        [SerializeField]private Sprite angleSprite;
        [SerializeField]private Sprite noAngleSprite;

        private const string DefaultInputStr = "请输入文字";
        private string DefaultHint = DefaultInputStr;
        private int MaxLength = 30;

        private readonly Color _hintColor = new Color(1, 1, 1, 0.5f);
        private readonly Color _normalColor = new Color(1, 1, 1, 1);

        public Action<string> OnTextChanged;

        protected override void OnInit()
        {
            DefaultHint = LocalizationManager.Inst.GetLocalizedText(DefaultInputStr);
            inputButton.onClick.AddListener(CallKeyboard);
            SetSingleUIStyle(false);
        }

        private void CallKeyboard()
        {
            string numLimitStr = LocalizationManager.Inst.GetLocalizedText("字数超出限制");
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = string.Empty,
                inputMode = 0,
                maxLength = MaxLength,
                inputFlag = 0,
                textSecurity = 0,
                lengthTips = numLimitStr,
                defaultText = inputText.text == DefaultHint ? string.Empty : inputText.text,
                returnKeyType = (int)ReturnType.Return
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
        }

        private void KeyboardReturn(string inputContent)
        {
            SetText(inputContent);
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        }

        /// <summary>
        /// 设置为独立UI存在。背景的图片不一样 
        /// </summary>
        public void SetSingleUIStyle(bool isSingle)
        {
            contentBgImg.sprite = isSingle ? angleSprite : noAngleSprite;
        }

        public void SetTitleText(string str)
        {
            titleText.text = str;
        }

        public void SetMaxLength(int max)
        {
            MaxLength = max;
        }

        public void SetText(string text)
        {
            text = FormatUtils.FilterNonStandardText(text);
            inputText.text = text;
            RefreshUI();
        }

        void RefreshUI()
        {
            var inputStr = inputText.text;
            if (inputStr.Length > MaxLength)
            {
                inputStr = inputStr.Substring(0, MaxLength);
                inputText.text = inputStr;
            }

            if (string.IsNullOrEmpty(inputText.text) || inputText.text == DefaultInputStr)
            {
                inputText.text = DefaultHint;
                inputText.color = _hintColor;
                limitText.color = _hintColor;
                limitText.text = $"0/{MaxLength}";
            } else
            {
                inputText.color = _normalColor;
                limitText.color = _normalColor;
                limitText.text = $"{inputStr.Length}/{MaxLength}";
                OnTextChanged?.Invoke(inputStr);
            }

        }
    }
}