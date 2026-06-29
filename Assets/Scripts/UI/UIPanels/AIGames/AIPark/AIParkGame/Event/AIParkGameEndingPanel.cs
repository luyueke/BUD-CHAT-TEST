using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Game.Base;
using GameData.BaseInfo;
using GameData.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static AIGame.Base.AIParkUtils;
using static Pb.Game.AIGameAmusementParkSyncReply.Types;

namespace AIGame.Base
{
    public class AIParkGameEndingPanel : BasePanel<AIParkGameEndingPanel>
    {
        public Text DescTxt;

        public CButton QuitBtn;

        public CButton ContinueBtn;

        public Transform Caidan;

        public Transform Putong;

        public List<Image> UgcImage = new();

        public List<Image> UgcImage2 = new();

        public List<Text> Text = new();

        public override void OnCreate()
        {
            base.OnCreate();

            foreach (var item in UgcImage)
            {
                item.ParkPopImageColor1();
            }

            foreach (var item in UgcImage2)
            {
                item.ParkPopImageColor2();
            }

            foreach (var item in Text)
            {
                item.ParkPopTextColor();
            }

            QuitBtn.onClick.AddListener(OnQuitBtn);

            ContinueBtn.onClick.AddListener(OnContinueBtn);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            DescTxt.text = args[0].ToString();

            Caidan.gameObject.SetActive(false);
            Putong.gameObject.SetActive(false);

            ParkEndType parkEndType = AIParkUtils.Inst.GetCompleteEndType();
            if(parkEndType == ParkEndType.Uncomplete){
                return;
            }else{
                Caidan.gameObject.SetActive(parkEndType == ParkEndType.SpecailSummary);
                Putong.gameObject.SetActive(parkEndType == ParkEndType.NormalSummary);
            }
        }

        private void OnQuitBtn()
        {
            AIGameController.Inst.GetCurAIGame<AIParkGame>().ExitGame();
        }

        private void OnContinueBtn()
        {
            CloseSelf();
        }
    }
}