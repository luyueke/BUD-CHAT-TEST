using System;
using System.Collections;
using System.Collections.Generic;
using EventTracking;
using Game.CommunityGame;
using Sirenix.OdinInspector;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class SearchView : MonoBehaviour
    {
        private CButton cancelButton;
        private CButton clearInputButton;
        private CButton inputBtn;
        private CButton searchBtn;

        public SearchDiscoverAndHistoryView searchDiscoverAndHistoryView;
        private Text inputText;
        public string searchHandle = "搜索皮肤设计码/作者ID/皮肤名";

        private Action<string> searchAction;
        private Action clearAction;
        private Action cancelAction;
        private Action initSearchAction;

        public bool isSearching = false;
        [HideInInspector]
        public bool isFittingRoom = false;

        public void Awake()
        {
            cancelButton = GameObjectEx.FindChildByName(this.transform, "BackToPanelButton").GetComponent<CButton>();
            inputBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SearchInput");
            searchBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SearchButton");
            clearInputButton = GameObjectEx.FindComponentByName<CButton>(transform, "ClearInputButton");
            inputText = GameObjectEx.FindComponentByName<Text>(transform, "InputText");
            searchDiscoverAndHistoryView = GameObjectEx.FindComponentByName<SearchDiscoverAndHistoryView>(transform, "searchDiscoverAndHistoryView");
            cancelButton.onClick.AddListener(OnCancelClick);
            inputBtn.onClick.AddListener(OnInputBtnClick);
            if (searchBtn != null)
            {
                searchBtn.onClick.AddListener(OnSearchBtnClick);
            }
            clearInputButton.onClick.AddListener(OnClearInputClick);
        }

        public void HideSearchDiscoverAndHistoryView()
        {
            searchDiscoverAndHistoryView?.gameObject?.SetActive(false);
        }

        public bool IsSearchMode()
        {
            return gameObject.activeInHierarchy;
        }

        public void OnEnable()
        {
            if (!isFittingRoom) return;
            try
            {
                isSearching = true;
                transform.parent.GetComponent<RectMask2D>().enabled = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        void OnDisable()
        {
            if (!isFittingRoom) return;
            try
            {
                isSearching = false;
                transform.parent.GetComponent<RectMask2D>().enabled = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public void SetSearchAction(Action<string> action1, Action action2, Action action3)
        {
            searchAction = action1;
            clearAction = action2;
            cancelAction = action3;
        }

        public void SetInitSearchAction(Action action)
        {
            initSearchAction = action;
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
                defaultText = isFittingRoom ? (inputText.text == searchHandle ? "" : inputText.text) : "",
                returnKeyType = (int)ReturnType.Search,
                textSecurity = 1
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }

        private void OnSearchBtnClick()
        {
            if (string.IsNullOrEmpty(inputText.text))
            {
                SetInputDef();
                return;
            }
            if (inputText.text == searchHandle)
            {
                return;
            }
            SearchWord(inputText.text);
        }

        void SearchWord(string word)
        {
            isSearching = true;
            OnKeyboard(word);
            searchDiscoverAndHistoryView.gameObject.SetActive(false);
            searchAction?.Invoke(word);
        }

        private void OnKeyboard(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                SetInputDef();
                return;
            }
            clearInputButton.gameObject.SetActive(true);
            inputText.color = Color.black;
            inputText.SetText(input);
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                //UGC上报
                LoadEvent.ReportPopupStatus("SearchDoneClick", "ClickSearchDone");
            }
            if (!isFittingRoom)
            {
                //试衣间不进行搜索
                searchAction?.Invoke(input);
            }
        }

        private void SetInputDef()
        {
            inputText.color = DataUtil.DeSerializeColor("9E9E9E");
            inputText.SetLocalText(searchHandle);
            clearInputButton.gameObject.SetActive(false);

            clearAction?.Invoke();
        }

        public void OnClearInputClick()
        {
            SetInputDef();
            searchDiscoverAndHistoryView?.RefreshData();

            if (isFittingRoom)
            {
                initSearchAction?.Invoke();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetInputDef();

            if (!isFittingRoom)
            {
                OnInputBtnClick();
            }
            else
            {
                if (searchDiscoverAndHistoryView != null)
                {
                    searchDiscoverAndHistoryView.RefreshData();
                    searchDiscoverAndHistoryView.onItemClick = (word, type, id) =>
                    {
                        if (type == 1)
                        {
                            SearchLogicMgr.Inst.UploadSearchWords(id, null); //上报热词搜索
                        }
                        OnKeyboard(word);
                        // SearchWord(word); //不进行搜索
                    };
                }
            }
        }


        public void OnCancelClick()
        {
            gameObject.SetActive(false);
            cancelAction?.Invoke();
        }
    }
}