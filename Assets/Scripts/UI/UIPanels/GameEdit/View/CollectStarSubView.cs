using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class CollectStarSubView : BasePropertyEditSubView
    {
        public Text inputText;
        public Text limitText;
        public Button inputButton;

        private const string DefaultInputStr = "给星星起个名字吧，例如：草丛中的隐藏星星";
        private string DefaultHint = DefaultInputStr;
        private const int MaxLength = 30;

        private readonly Color _hintColor = new Color(1, 1, 1, 0.5f);
        private readonly Color _normalColor = new Color(1, 1, 1, 1);

        public Action<string> OnStarNameChanged;

        protected override void OnInit()
        {
            DefaultHint = LocalizationManager.Inst.GetLocalizedText(DefaultInputStr);
            inputButton.onClick.AddListener(CallKeyboard);
        }

        private void CallKeyboard()
        {
            string limitStr = LocalizationManager.Inst.GetLocalizedText("哎呀！文本太长了！缩短后再试试吧！");
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = string.Empty,
                inputMode = 0,
                maxLength = MaxLength,
                inputFlag = 0,
                textSecurity = 0,
                lengthTips = limitStr,
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

        public void SetText(string text)
        {
            inputText.text = text;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (string.IsNullOrEmpty(inputText.text) || string.Equals(inputText.text, DefaultInputStr) )
            {
                inputText.text = DefaultHint;
            }

            if (string.Equals(inputText.text, DefaultHint))
            {
                inputText.color = _hintColor;
                limitText.color = _hintColor;
                limitText.text = $"0/{MaxLength}";
            }
            else
            {
                inputText.color = _normalColor;
                limitText.color = _normalColor;
                limitText.text = $"{inputText.text.Length}/{MaxLength}";
                OnStarNameChanged?.Invoke(inputText.text);
            }

        }
    }
}