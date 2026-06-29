using BUD.MailBox;
using Com.TheFallenGames.OSA.Util.IO;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem.Data;

namespace GameUI
{
    public class ActivitySkipView : ActivityBaseView
    {
        public CButton Btn;
        public Text BtnTxt;
        public Image Img;
        public RemoteImageBehaviour RemoteImage;

        public RawImage RemoteImage2;

        private ActivityInfo info;

        public List<Texture> textures;
        public override void Init(ActivityInfo info)
        {
            base.Init(info);
            this.info = info;
            Btn.onClick.AddListener(OnBtn);

            RemoteImage.Load(info.activitySkipMsg.coverUrl, true, (t,b) => {
                //RemoteImage.RawImage.SetNativeSize();
            });

            switch (info.activitySkipMsg.bgColor)
            {
                case "pink": RemoteImage2.texture = textures[0];  break;
                case "yellow": RemoteImage2.texture = textures[1]; break;
                case "blue": RemoteImage2.texture = textures[2]; break;
            }
     
            Img.color = DataUtil.DeSerializeColorCheckHash(info.activitySkipMsg.buttonColor);

            BtnTxt.text = info.activitySkipMsg.buttonName;

            ActivitySkipSystem.Inst.ShowPopupIds.Add(info.activitySkipMsg.id);
        }

        private void OnEnable()
        {
            if (info != null)
            {
                ActivitySkipSystem.Inst.ShowPopupIds.Add(info.activitySkipMsg.id);
            }
        }

        private void OnDestroy()
        {
            ActivitySkipSystem.Inst.PushreportData();
        }

        void OnBtn() 
        {
            if (info.activitySkipMsg == null) 
            {
                return;
            }
            var webtoolNewsData = new WebtoolNewsData()
            {
                skipType = info.activitySkipMsg.skipType,
                skipData = info.activitySkipMsg.skipData,
            };
            WebtoolNewsSkipManager.Inst.HandleSkip(webtoolNewsData);
            ActivitySkipSystem.Inst.PushreportData(null, info.activitySkipMsg.id,1);
        }
    }
}