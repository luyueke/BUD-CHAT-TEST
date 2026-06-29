using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class SearchUgcView : MonoBehaviour
    {
        private CButton inputBtn;
        private Text inputText;
        private Text searchEmptyText;
        private CButton closeSearchBtn;
        public string searchHandle = "请输入7位商品设计码";

        private Action<string> searchAction;
        private Action clearAction;
        private Action cancelAction;

        public void Awake()
        {
            inputBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SearchInput");
            inputText = GameObjectEx.FindComponentByName<Text>(transform, "InputText");
            searchEmptyText = GameObjectEx.FindComponentByName<Text>(transform, "SearchEmptyText");
            closeSearchBtn = GameObjectEx.FindComponentByName<CButton>(transform, "ClearInputButton");
            inputBtn.onClick.AddListener(OnInputBtnClick);
            closeSearchBtn.onClick.AddListener(OnClearInputClick);
        }

        public void SetSearchAction(Action<string> action1, Action action2, Action action3)
        {
            searchAction = action1;
            clearAction = action2;
            cancelAction = action3;
        }

        public void OnClearInputClick()
        {
            SetInputDef();
        }

        private void OnInputBtnClick()
        {
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText(searchHandle),
                inputMode = (int)KeyBoardInputMode.All,
                maxLength = 30,
                inputFlag = 0,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("您的输入超出了限制"),
                defaultText = "",
                returnKeyType = (int)ReturnType.Search,
                textSecurity = 1
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }

        private void OnKeyboard(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                SetInputDef();
                return;
            }
            inputText.color = Color.black;
            inputText.SetText(input);
            closeSearchBtn.gameObject.SetActive(true);
            searchEmptyText.gameObject.SetActive(false);
            searchAction?.Invoke(input);
        }

        private void SetInputDef()
        {
            inputText.color = DataUtil.DeSerializeColor("9E9E9E");
            inputText.SetLocalText(searchHandle);
            closeSearchBtn.gameObject.SetActive(false);
            searchEmptyText.gameObject.SetActive(true);
            clearAction?.Invoke();
        }
        
    }
}