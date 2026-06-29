using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.FriendList
{
    public class AddFriendPanel : BasePanel<AddFriendPanel>
    {
        public CButton BackBtn;
        public CButton SaveBtn;

        public CText idInputTxt;
        public CButton copyBtn;
        public CButton idInputBtn;
        public CText myIdText;

        private string idInput = "";
        private string myId = "";


        public override void OnCreate()
        {
            SaveBtn.onClick.AddListener(OnConfirmClick);
            BackBtn.onClick.AddListener(OnBackBtnClick);
            copyBtn.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(myId))
                {
                    return;
                }
                GUIUtility.systemCopyBuffer = myId;
                TipPanel.ShowToast("复制成功");
            });

            idInputBtn.onClick.AddListener(() =>
            {
                OnInputIdClick();
            });

            if (AccountDataManager.Inst.UserInfo != null)
            {
                myId = AccountDataManager.Inst.UserInfo.username;
                myIdText.SetLocalText("你的ID:{0}",myId);
            }
        }

        private void OnConfirmClick()
        {
            if (string.IsNullOrEmpty(idInput))
            {
                return;
            }

            if (idInput.Length > 6)
            {
                TipPanel.ShowToast("字数超出限制");
                return;
            }

            SearchByIdParams searchByIdParams = new SearchByIdParams();
            searchByIdParams.targetId = idInput;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.searchByID,
                HttpMethod.GET,
                JsonConvert.SerializeObject(searchByIdParams),
                onReceive: msg =>
                {
                    if (string.IsNullOrEmpty(msg))
                    {
                        return;
                    }
                    SearchByIdResponse searchByIdResponse =
                        JsonConvert.DeserializeObject<SearchByIdResponse>(msg);
                    if (searchByIdResponse != null)
                    {
                        FriendInfoPanel friendInfoPanel = UIManager.Inst.OpenPanel<FriendInfoPanel>(PanelId.FriendInfoPanel);
                        friendInfoPanel.SetData(idInput,searchByIdResponse);
                        CloseSelf();
                    }
                }, onFail: arg0 =>
                {
                    if (string.IsNullOrEmpty(arg0))
                    {
                        return;
                    }
                    HttpResponseRawData httpResponseRawData = JsonConvert.DeserializeObject<HttpResponseRawData>(arg0);
                    if (httpResponseRawData == null)
                    {
                        return;
                    }

                    string rmsg = httpResponseRawData.rmsg;
                    TipPanel.ShowToast(rmsg);
                });
        }

        private void OnBackBtnClick()
        {
            CloseSelf();
        }

        private void CloseSelf()
        {
            UIManager.Inst.ClosePanel(this);
        }

        private void OnInputIdClick()
        {
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = $"{LocalizationManager.Inst.GetLocalizedText("请输入6位ID")}...",
                inputMode = 2,
                maxLength = 1000,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                defaultText = "",
                returnKeyType = (int)ReturnType.Send
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
        }

        private void KeyboardReturn(string str)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            idInput = str;
            idInputTxt.text = str;
        }
    }

}
