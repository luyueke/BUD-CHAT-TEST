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
    /// 通讯录界面(空界面) 点击后 去ai伙伴商城购买伙伴
    /// </summary>
    public class ChatTXEmptyNode : MonoBehaviour
    {
        public Button buyBtn;

        public ChatMainPanel chatMainPanel;

        void Awake()
        {
            buyBtn.onClick.AddListener(buyClick);
        }



        void buyClick()
        {
            ScreenOrientationHelper.Inst.Switch(ScreenOrientation.LandscapeLeft, () =>
            {
                UIManager.Inst.ClosePanel(PanelId.AICompanionChatPanel);
                UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel);
            });
        }

       
    }
}