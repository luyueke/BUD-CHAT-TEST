using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    /// <summary>
    /// 文本输入组件 自动监听点击 调起端上接口 切换输入文本和填充文本
    /// </summary>
    public class TextInputView : MonoBehaviour
    {
        [SerializeField] private Button InputBtn;
        [SerializeField] private Text PlaceHolder;
        [Tooltip("形如 5/100 剩余字数 可选")]
        [SerializeField] private Text LimitText;
        [SerializeField] private SuperTextMesh inputSTM;
        [SerializeField] private KeyBoardInputMode inputMode = KeyBoardInputMode.SingleLine;
        public int charLimit;
        public bool userCharCalcul = false;
        [Tooltip("是否进行安全检查")]
        public bool checkSecurity = false;

        public bool filterEmoji = false;

        public string Input { get; private set; }

        private Action<string> onInput;

        private void Awake()
        {
            InputBtn.onClick.AddListener(OnInputClick);
            SetInputWithoutNotify("");
        }

        /// <summary>
        /// 监听输入事件
        /// </summary>
        /// <param name="act"></param>
        public void SetOnInput(Action<string> act)
        {
            onInput = act;
        }

        /// <summary>
        /// 设置输入并调用监听事件
        /// </summary>
        /// <param name="input"></param>
        public void SetInput(string input)
        {
            SetInputWithoutNotify(input);
            onInput?.Invoke(input);
        }

        /// <summary>
        /// 直接设置输入 不调用监听事件
        /// </summary>
        /// <param name="input"></param>
        public void SetInputWithoutNotify(string input)
        {
            Input = input;
            if (Input == null) Input = "";

            string targetStr = DataUtil.ReplaceEmojiForSTM(Input);
            targetStr = DataUtil.FilterNonStandardText(targetStr);
            inputSTM.text = targetStr;

            RefreshPlaceHolder();
            RefreshLimitText();
        }

        public void SetPlaceholder(string content)
        {
            PlaceHolder.text = content;
        }

        private void OnInputClick()
        {
            ShowKeyboard();
        }

        private void RefreshPlaceHolder()
        {
            if (PlaceHolder)
            {
                bool isEmpty = string.IsNullOrEmpty(Input);
                PlaceHolder.gameObject.SetActive(isEmpty);
                inputSTM.gameObject.SetActive(!isEmpty);
            }
        }

        public void RefreshLimitText()
        {
            if (LimitText)
            {
                LimitText.text = $"{GetDisplayLength(Input)}/{charLimit}";
            }
        }

        private void ShowKeyboard()
        {
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = GetPlaceHolderText(),
                inputMode = (int)inputMode,
                maxLength = charLimit,
                inputFlag = 0,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
                defaultText = Input,
                returnKeyType = (int)ReturnType.Send,
                textSecurity = checkSecurity ? 0 : 1,
                isFilterEmoji = filterEmoji ? 1 : 0
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }

        private void OnKeyboard(string str)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            // 限制 7 个字符单位：汉字/全角=1，数字字母=0.5
            if (userCharCalcul)
            {
                str = TruncateByDisplayLength(str ?? "", charLimit / 2);
            }
            SetInput(str);
        }

        /// <summary> 按显示长度截断：1 汉字/全角=1，1 数字/字母=0.5，总长不超过 maxLength（如 7） </summary>
        private string TruncateByDisplayLength(string s, float maxLength)
        {
            if (string.IsNullOrEmpty(s) || maxLength <= 0) return s ?? "";
            float sum = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float w = GetCharDisplayWidth(s[i]);
                if (sum + w > maxLength) return s.Substring(0, i);
                sum += w;
            }
            return s;
        }

        /// <summary> 数字字母=0.5，汉字/全角=1 </summary>
        private float GetCharDisplayWidth(char c)
        {
            if (c <= 0xFF) return 0.5f; // ASCII 数字字母等
            if (c >= 0x4E00 && c <= 0x9FFF) return 1f;
            if (c >= 0x3000 && c <= 0x303F) return 1f;
            if (c >= 0xFF00 && c <= 0xFFEF) return 1f;
            return 1f;
        }

        /// <summary> 计算字符串显示长度：汉字/全角=1，数字字母=0.5 </summary>
        public float GetDisplayLength(string s)
        {
            if (!userCharCalcul)
            {
                return s.Length;
            }
            if (string.IsNullOrEmpty(s)) return 0f;
            float sum = 0f;
            for (int i = 0; i < s.Length; i++)
                sum += GetCharDisplayWidth(s[i]);
            return sum * 2;
        }

        //输入框中显示Holder
        private string GetPlaceHolderText()
        {
            if (PlaceHolder)
            {
                return LocalizationManager.Inst.GetLocalizedText(PlaceHolder.text);
            }
            return Input;
        }
    }
}
