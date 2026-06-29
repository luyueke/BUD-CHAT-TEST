using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Audio;
using Game.AINPCStudio;
using Game.AIResData;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using Game.Event;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIYandereStartPanel : BasePanel<AIYandereStartPanel>
    {
        public CButton Btn_Leaderboard;
        public CButton Btn_ExitGame;
        public CButton Btn_Start;
        public CButton Btn_ChooseCharacter;

        private Action _startAct;
        private Action _closeAct;

        private AINpcInfo _npcInfo;
        public GameObject CardNode;
        public Text HasCountText;
        
        private AIContentData aiResData;

        public override void OnCreate()
        {
            base.OnCreate();
            var wrapper = Loader.Load<TextAsset>("Assets/Loadable/UI/UIPanel/AIYandereGame/AIGameMetaData/YUMI.json");
            if (wrapper == null)
            {
                LoggerUtils.LogError("localOfficialConfig  read Error");
                return;
            }
            var content = wrapper.RetainAsset(this.gameObject).text;
            _npcInfo = JsonConvert.DeserializeObject<AINpcInfo>(content);
            AkSoundManager.Inst.StopBGSound();
            AIGameSoundUtils.Inst.PlayBgm(YandereConfig.BGM_START);
            Btn_Leaderboard.onClick.AddListener(OnBtnLeaderboardClick);
            Btn_ExitGame.onClick.AddListener(OnExitGameClick);
            Btn_Start.onClick.AddListener(OnBtnStartClick);
            Btn_ChooseCharacter.onClick.AddListener(OnChooseCharacterClick);

            EventCenterDataManager.Inst.ReportTask(PostEventId.ViewEscapeSimulator);
        }
        

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            GetAIGame();
        }

        private void GetAIGame()
        {
            aiResData = AIResDataManager.Inst.GetAIGameData(AIResType.AIYandere);
            if (aiResData == null)
            {
                LoggerUtils.LogError("无法获取AI游戏对局信息");
                return;
            }
            SetGameCard(aiResData.remaining);
            CardNode.SetActive(aiResData.free != (int) AIResFreeState.Free);
        }

        public void SetGameCard(int has)
        {
            HasCountText.SetLocalText("今日剩余局数：{0}", has);
        }
        
        private void UpdateAIResData(int buyCount)
        {
            if (aiResData != null)
            {
                aiResData.free = (int) AIResFreeState.Buy;
                aiResData.remaining = buyCount;
                SetGameCard(buyCount);
            }
        }

        private void UpdateFreeByNewDay()
        {
            AIResDataManager.Inst.GetNetworkAIResData(GetAIGame);
        }

        private void ValidateGameCard(Action onNext)
        {
            if (aiResData == null)
            {
                onNext?.Invoke();
            }
            else
            {
                if (aiResData.remaining <= 0)
                {
                    var aiPanel = UIManager.Inst.OpenPanel<AIBuyResourcePanel>(PanelId.AIBuyResourcePanel,(int)AIResType.AIYandere);
                    aiPanel.SetOnBuySuccessAct(UpdateAIResData,UpdateFreeByNewDay);
                }
                else
                {
                    onNext?.Invoke();
                }
            }
        }


        public void SetAction(Action startAct = null, Action closeAct = null)
        {
            this._startAct = startAct;
            this._closeAct = closeAct;
        }

        private void OnBtnStartClick()
        {
            ValidateGameCard(EnterYandereGame);
        }

        private void EnterYandereGame()
        {
            this._startAct?.Invoke();
            GameController.StartAIGame(_npcInfo, EnterGameModel.AIYandere, true, "AIYandere");
            EventCenterDataManager.Inst.ReportTask(PostEventId.PlayApartmentEscape);
        }

        private void OnChooseCharacterClick()
        {
            ValidateGameCard(() =>
            {
                NpcStoreEnterData enterData = new NpcStoreEnterData();
                enterData.EnterType = NpcStoreEnterType.AIYandereGameStart;
                enterData.OnSelectNpcAct = OnSelectNpc;

                UIManager.Inst.OpenPanel<AINpcStorePanel>(PanelId.AINpcStorePanel, enterData);
            });
        }

        private void OnSelectNpc(AINpcInfo npcInfo)
        {
            LoggerUtils.LogError("选择了 NPC : ", npcInfo.id);
            _npcInfo = npcInfo;
            PreStartReq req = new()
            {
                npcId = npcInfo.id,
                gameId = (int) PGCGameType.AIYandere,
            };
            UIManager.Inst.OpenPanel<AIGameLoadingPanel>(PanelId.AIGameLoadingPanel, req);
            EnterYandereGame();
            UIManager.Inst.ClosePanel(PanelId.AINpcStorePanel);
            UIManager.Inst.ClosePanel(PanelId.AIYandereStartPanel);
        }

        private void OnExitGameClick()
        {
            AIGameSoundUtils.Inst.StopBgm(YandereConfig.BGM_START);
            GameController.ExitGame();
            CloseSelf();
        }

        private void OnBtnLeaderboardClick()
        {
            UIManager.Inst.OpenPanel<AIYandereLeaderboardPanel>(PanelId.AIYandereLeaderboardPanel);
        }
    }
}
