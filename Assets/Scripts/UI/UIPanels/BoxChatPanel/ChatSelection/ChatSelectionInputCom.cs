using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// "自己写"对应的输入组件，同时作为多选模式的发送入口
    /// </summary>
    public class ChatSelectionInputCom : MonoBehaviour
    {
        #region 创建聊天的组件
        public Button Selection_SendBtn;

        public GameObject Selection_NoSend;
        public InputField Selection_InputField;

        public Text Selection_InputTxtTem;

        public ScrollRect Selection_TxtScroll;

        public RectTransform Selection_TxtScrollContent;

        public ContentSizeFitter Selection_InputTxtTemSize;

        public RectTransform Selection_InputTxtTemRect;

        public RectTransform Selection_InputFieldRect;

        public RectTransform Selection_InputBgRect;

        public Button Selection_InputFieldBtn;

        public KeyBoardTest Selection_keyBoardTest;
        #endregion

        public RectTransform Content;
        public RectTransform ScrollView;
        public RectTransform panel;

        public ChatPanel chatPanel;

        float key_h;
        float txt_h;

        Action<string> _onSend;
        bool _hasExternalContent; // 外部（chips）已有选中内容

        void Awake()
        {
            // Selection_InputField.placeholder.GetComponent<Text>().text = "Talk to " + "";

            Selection_InputField.onEndEdit.AddListener(Selection_OnInput);
            Selection_InputFieldBtn.onClick.AddListener(Selection_OnInputBtn);
            Selection_InputFieldBtn.gameObject.SetActive(true);
            Selection_keyBoardTest.offsetAction = Selection_OnOffsetAction;
            Selection_keyBoardTest.inputAction = Selection_OnInputAction;
            Selection_InputField.onValueChanged.AddListener((str) => { Selection_OnInputAction(Selection_InputField.text); });
            Selection_SendBtn.onClick.AddListener(OnSendBtnClick);
        }

        public void SetData(Action<string> onSend)
        {
            _onSend = onSend;
            _hasExternalContent = false;
            Selection_InputField.SetTextWithoutNotify("");
            Selection_InputTxtTem.text = "";
            Selection_InputFieldBtn.gameObject.SetActive(true);
            UpdateSendState();
        }

        /// <summary>
        /// 由 ChatSelectionParentCom 在 chips 选中状态变化时调用，更新发送按钮可用性
        /// </summary>
        public void SetExternalContent(bool hasContent)
        {
            _hasExternalContent = hasContent;
            UpdateSendState();
        }

        void UpdateSendState()
        {
            bool canSend = _hasExternalContent || !string.IsNullOrEmpty(Selection_InputField.text);
            Selection_NoSend.gameObject.SetActive(!canSend);
            Selection_NoSend.SetActive(!canSend);
        }

        void OnSendBtnClick()
        {
            _onSend?.Invoke(Selection_InputField.text);
            Selection_InputField.SetTextWithoutNotify("");
            Selection_OnInputAction("");
            Selection_CloseKeyboard();
            Selection_OnOffsetAction(0);
        }

        #region selection

        void Selection_OnInput(string str)
        {
        }

        void Selection_OnInputBtn()
        {
#if UNITY_EDITOR
            Selection_OnOffsetAction(0);
#endif
            if (Selection_keyBoardTest.keyboard == null)
            {
                Selection_keyBoardTest.OpenKeyboard("");
                Selection_InputFieldBtn.gameObject.SetActive(false);
            }
        }

        void Selection_CloseKeyboard()
        {
            Selection_OnOffsetAction(0);
            Selection_keyBoardTest.CloseOpenKeyboard();
            Selection_InputFieldBtn.gameObject.SetActive(true);


            Selection_TxtScroll.gameObject.SetActive(false);
            Selection_InputBgRect.sizeDelta = new(721, 134);
            Selection_InputFieldRect.transform.SetParent(Selection_InputBgRect.transform);

            Selection_InputFieldRect.anchorMin = new Vector2(0.5f, 0);
            Selection_InputFieldRect.anchorMax = new Vector2(0.5f, 1);
            Selection_InputFieldRect.pivot = new Vector2(1, 0);

            Selection_InputFieldRect.sizeDelta = new Vector2(715f, Selection_InputFieldRect.sizeDelta.y);
            Selection_InputFieldRect.anchoredPosition = new Vector2(344f, Selection_InputFieldRect.anchoredPosition.y);
            Selection_InputFieldRect.offsetMin = new Vector2(Selection_InputFieldRect.offsetMin.x, 22.3f);
            Selection_InputFieldRect.offsetMax = new Vector2(Selection_InputFieldRect.offsetMax.x, -8.9f);

            //
            // Selection_InputFieldRect.anchoredPosition = new Vector2(714, 30);

            // Selection_InputFieldBtn.transform.SetAsLastSibling();
            // Selection_SendBtn.transform.SetAsLastSibling();

            Selection_InputBgRect.sizeDelta = new Vector2(Selection_InputBgRect.sizeDelta.x, 134 + Math.Max(0, 0 - 72));
            ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, 134 + key_h + 0);
        }

        float Selection_barY;
        void Selection_OnInputAction(string str)
        {
#if UNITY_EDITOR
            // Selection_OnOffsetAction(400);
#endif
            // Debug.LogError("1当前内容：" + str);
            if (Selection_InputTxtTem.text != str)
            {
                // 仅当 InputField 内容与新文本不一致时（外部键盘推送路径）才需要同步，
                // 同步后手动推进光标，否则 SetTextWithoutNotify 不会移动 m_CaretPosition。
                bool needsSync = Selection_InputField.text != str;
                int prevCaret = Selection_InputField.caretPosition;
                int prevLen   = Selection_InputField.text.Length;

                Selection_InputField.SetTextWithoutNotify(str);

                if (needsSync && str.Length > prevLen)
                {
                    int newCaret = Mathf.Min(prevCaret + (str.Length - prevLen), str.Length);
                    Selection_InputField.caretPosition = newCaret;
                    Selection_InputField.selectionAnchorPosition = newCaret;
                }

                Selection_InputTxtTem.text = str;
                Selection_InputTxtTemSize.SetLayoutVertical();
                txt_h = Selection_InputTxtTemRect.sizeDelta.y;
                if (txt_h >= 151)
                {
                    Selection_TxtScroll.gameObject.SetActive(true);
                    if (Selection_InputFieldRect.transform.parent != Selection_TxtScrollContent.transform)
                    {
                        Selection_InputFieldRect.transform.parent = Selection_TxtScrollContent.transform;
                        Selection_barY = 0;
                    }
                    else
                    {
                        Selection_barY = Selection_TxtScroll.verticalScrollbar.value;
                    }
                    Selection_InputBgRect.sizeDelta = new Vector2(Selection_InputBgRect.sizeDelta.x, 214);

                    Selection_InputFieldRect.anchorMin = new Vector2(0, 0);
                    Selection_InputFieldRect.anchorMax = new Vector2(0, 0);
                    Selection_InputFieldRect.pivot = new Vector2(0, 0);
                    Selection_InputFieldRect.anchoredPosition = new Vector2(0f, 0f);

                    LayoutRebuilder.ForceRebuildLayoutImmediate(Selection_TxtScrollContent);
                    Selection_TxtScrollContent.sizeDelta = new(Selection_TxtScrollContent.sizeDelta.x, 178.6f);
                    Selection_InputFieldRect.sizeDelta = new(Selection_InputFieldRect.sizeDelta.x, 178.6f);
                    Selection_TxtScrollContent.anchoredPosition = new Vector2(Selection_TxtScrollContent.anchoredPosition.x,
                        (Selection_TxtScrollContent.sizeDelta.y - 155) * (1 - Selection_barY));
                }
                else
                {
                    Selection_TxtScroll.gameObject.SetActive(false);
                    Selection_InputFieldRect.transform.SetParent(Selection_InputBgRect.transform);

                    Selection_InputFieldRect.anchorMin = new Vector2(0.5f, 0);
                    Selection_InputFieldRect.anchorMax = new Vector2(0.5f, 1);
                    Selection_InputFieldRect.pivot = new Vector2(1, 0);

                    Selection_InputFieldRect.sizeDelta = new Vector2(715f, Selection_InputFieldRect.sizeDelta.y);
                    Selection_InputFieldRect.anchoredPosition = new Vector2(344f, Selection_InputFieldRect.anchoredPosition.y);
                    Selection_InputFieldRect.offsetMin = new Vector2(Selection_InputFieldRect.offsetMin.x, 22.3f);
                    Selection_InputFieldRect.offsetMax = new Vector2(Selection_InputFieldRect.offsetMax.x, -8.9f);

                    //
                    // Selection_InputFieldRect.anchoredPosition = new Vector2(714, 30);

                    // Selection_InputFieldBtn.transform.SetAsLastSibling();
                    // Selection_SendBtn.transform.SetAsLastSibling();

                    Selection_InputBgRect.sizeDelta = new Vector2(Selection_InputBgRect.sizeDelta.x, 134 + Math.Max(0, txt_h - 72));
                    ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, 134 + key_h + txt_h);
                }

                UpdateSendState();
            }
        }

        void Selection_OnOffsetAction(float h)
        {
            key_h = h;
            if (key_h != panel.anchoredPosition.y)
            {
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, key_h);
                ScrollView.offsetMin = new Vector2(ScrollView.offsetMin.x, 200 + key_h + txt_h);
                if (Content.sizeDelta.y > ScrollView.rect.height)
                {
                    Content.DOLocalMoveY(Content.sizeDelta.y - ScrollView.rect.height, 0.2f);
                }
            }
            chatPanel.key_h = key_h;
            chatPanel.RefreshScrollviewLayout();
        }

        #endregion

        [Button("offset300")]
        void testOffset300()
        {
            Selection_OnOffsetAction(300);
        }
        [Button("offset0")]
        void testOffset0()
        {
            Selection_OnOffsetAction(0);
        }


        [Button("测试关闭键盘")]
        void testCloseKeyboard()
        {
            Selection_CloseKeyboard();
        }
    }
}
