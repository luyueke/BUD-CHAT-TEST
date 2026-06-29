using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameStagePanel : BasePanel<AIParkGameStagePanel>
    {
        public Button CloseBtn;
        public AIParkGameStageMusic Music;
        public AIParkGameChooseNpc Npc;
        public AIParkGameStageView View;

        [HideInInspector]public AIPark_StageBehaviour behaviour;
        public override void OnCreate()
        {
            base.OnCreate();

            CloseBtn.onClick.AddListener(OnCloseBtn);

            Music.Init(this);
            View.Init(this);
            Music.gameObject.SetActive(true);
            Npc.gameObject.SetActive(false);
            View.gameObject.SetActive(false);
        }

        protected override void OnDisable()
        {
            var panel = UIManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.AIParkGuestPanel) as AIParkGuestPanel;
            panel.ShowPanel();
            base.OnDisable();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            behaviour = (AIPark_StageBehaviour)args[0];

            var bo = (bool)args[1];
            if (bo)
            {
                Music.gameObject.SetActive(false);
                Npc.gameObject.SetActive(false);
                View.gameObject.SetActive(false);
                var panel = UIManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.AIParkGuestPanel) as AIParkGuestPanel;
                panel.HidePanel();
            }
        }

        private void OnCloseBtn() {
            var panel = UIManager.Inst.OpenPanel<AIParkConfirmPanel>(PanelId.AIParkConfirmPanel);
            panel.SetData("提示", "是否退出演奏", "取消", "确定",
                () =>
                {

                },
                () =>
                {
                    CloseSelf();
                    behaviour.Stop();
                });
        }
    }
}