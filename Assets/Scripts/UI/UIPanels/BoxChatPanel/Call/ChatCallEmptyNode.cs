using Basic;
using DG.Tweening;
using Fsbm.Runtime;
using Message;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通话界面(空界面)
    /// </summary>
    public class ChatCallEmptyNode : MonoBehaviour
    {
        public GameObject empty_buy; //去购买bud box界面
        public Button buyBtn;
        public GameObject empty_chat; //去聊天界面
        public Button chatBtn;

        public ChatMainPanel chatMainPanel;

        void Awake()
        {
            buyBtn.onClick.AddListener(buyClick);
            chatBtn.onClick.AddListener(chatClick);
        }

        public void ShowEmpty()
        {
            CabinBoxManager.Inst.InitBudBoxDic((b) =>
            {
                empty_buy.SetActive(CabinBoxManager.Inst.GetBudBoxCount() == 0);
                empty_chat.SetActive(CabinBoxManager.Inst.GetBudBoxCount() > 0);

#if UNITY_EDITOR
                // empty_buy.SetActive(false);
                // empty_chat.SetActive(false);
#endif
            });
        }

        void buyClick()
        {
            TipPanel.ShowToast("跳转购买");
        }

        void chatClick()
        {
            // TipPanel.ShowToast("去聊天");
            chatMainPanel.SwitchTab(2);
        }
    }
}