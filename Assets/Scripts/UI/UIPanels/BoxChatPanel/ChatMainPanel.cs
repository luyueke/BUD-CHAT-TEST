using Sirenix.OdinInspector;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ChatMainPanel : MonoBehaviour
    {
        public CButton BtnMsg;
        public CButton BtnVoice;
        public CButton BtnContacts;

        public GameObject ChatPanelObj; //文字聊天界面
        public GameObject ChatVoicePanelObj; //通话界面
        public GameObject ChatTXPanelObj;

        // 各 tab 选中态图标（可选，在 Inspector 中绑定）
        public Image ImgMsgSel;
        public Image ImgVoiceSel;
        public Image ImgContactsSel;

        public GameObject bottomGo;

        public ChatPanel chatPanel;

        private int _curTab = -1;

        bool canShowDefault = true;

        private void Awake()
        {
            if (BtnMsg != null)
                BtnMsg.onClick.AddListener(() => SwitchTab(0));
            if (BtnVoice != null)
                BtnVoice.onClick.AddListener(() => SwitchTab(1));
            if (BtnContacts != null)
                BtnContacts.onClick.AddListener(() => SwitchTab(2));

            chatPanel.onCloseCb = () =>
            {
                SwitchTab(0);
            };

            chatPanel?.SetChatMainPanel(this);
            chatPanel.gameObject.SetActive(false);
        }

        public void ShowDefault()
        {
            if (canShowDefault)
            {
                SwitchTab(0);
            }
        }

        public void SwitchTab(int index)
        {
            if (_curTab == index) return;
            _curTab = index;

            if (ChatPanelObj != null) ChatPanelObj.SetActive(index == 0);
            if (ChatVoicePanelObj != null) ChatVoicePanelObj.SetActive(index == 1);
            if (ChatTXPanelObj != null) ChatTXPanelObj.SetActive(index == 2);

            RefreshTabHighlight(index);
        }

        private void RefreshTabHighlight(int index)
        {
            if (ImgMsgSel != null) ImgMsgSel.gameObject.SetActive(index == 0);
            if (ImgVoiceSel != null) ImgVoiceSel.gameObject.SetActive(index == 1);
            if (ImgContactsSel != null) ImgContactsSel.gameObject.SetActive(index == 2);
        }

        public void HideAll()
        {
            bottomGo?.SetActive(false);
            SwitchTab(-1);
        }


        /// <summary>
        /// 创建角色聊天
        /// </summary>
        public void BeginCreateRoleChat()
        {
            canShowDefault = false;
            bottomGo?.SetActive(false);
            ChatPanelObj?.SetActive(false);
            chatPanel.gameObject.SetActive(true);
            chatPanel?.BeginCreateRoleChat();
        }

        /// <summary>
        /// 角色聊天
        /// </summary>
        public void BeginCharacterChat(string sessionId,string name,string portraitUrl)
        {
            canShowDefault = false;
            bottomGo?.SetActive(false);
            ChatPanelObj?.SetActive(false);
            CabinChatManager.Inst.GetBoxTextHistory(sessionId, (cabinChatTextHistoryData) =>
            {
                OnHistoryReceived(portraitUrl,cabinChatTextHistoryData);
            });
            chatPanel?.BeginCharacterChat(sessionId,portraitUrl,name);
        }

        private void OnHistoryReceived(string robotPortraitUrl,CabinChatTextHistoryData cabinChatTextHistoryData)
        {
            chatPanel.InitWithHistory(robotPortraitUrl,cabinChatTextHistoryData);
        }


        public void EndRoleChat()
        {
            canShowDefault = true;
            bottomGo?.SetActive(true);
            ChatPanelObj?.SetActive(true);
        }

        [Button("测试跟角色聊天")]
        public void TestChatWithCharacter()
        {
            // CabinChatManager.Inst.BoxchatStream();
        }
    }
}
