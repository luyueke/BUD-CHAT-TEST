using Game.Base;
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
using System.Threading.Tasks.Sources;
using UI.Base;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkConfirmPanel : BasePanel<AIParkConfirmPanel>
    {
        public CButton LBtn;
        public CButton RBtn;

        public Text LText;
        public Text RText;

        public Text Title;
        public Text Desc;

        private Action LAction;

        private Action RAction;
        public override void OnCreate()
        {
            base.OnCreate();

            LBtn.onClick.AddListener(OnLBtn);
            RBtn.onClick.AddListener(OnRBtn);

            MessageHelper.AddListener<AIParkConfirmParam>(MessageName.OnConfirmPanel, SetData);
        }

        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener<AIParkConfirmParam>(MessageName.OnConfirmPanel, SetData);
            base.OnDestroy();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
        }

        public void SetData(AIParkConfirmParam param) {
            SetData(param.Title,param.Desc,param.LText,param.RText,param.LAction,param.RAction);
        }

        public void SetData(string title ,string desc,string lTxt,string rTxt,Action lAction,Action rAction) 
        {
            Title.text = title;
            Desc.text = desc;
            LText.text = lTxt;
            RText.text = rTxt;
            LAction = lAction;
            RAction = rAction;
        }

        private void OnLBtn() {
            CloseSelf();
            LAction?.Invoke();
        }
        private void OnRBtn()
        {
            CloseSelf();
            RAction?.Invoke();
        }
    }
}