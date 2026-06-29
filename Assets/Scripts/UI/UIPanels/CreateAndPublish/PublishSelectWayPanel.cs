using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BUD.GameStudio
{
    public class PublishSelectData
    {
        public DraftListItem DraftListItem;
        public Action PublishPassLevelMapAct;
        public Action PublishNormalMapAct;
    }
    
    public class PublishSelectWayPanel : BasePanel<PublishSelectWayPanel>
    {
        public Toggle NewGame;
        public Toggle ExistingGame;
        public CButton CancelButton;
        public CButton ContinueButton;
        public Image _titleBg;

        public CommonSpriteSwitch _toggleMark1;
        public CommonSpriteSwitch _toggleMark2;
        //是否发布为新地图 ？ 还是覆盖更新
        private bool newGame = true;
        private PublishSelectData _publishSelectData;
        private GameType _gameType = GameType.Normal;
        #region UIConfig

        public override void OnCreate()
        {
            base.OnCreate();
            NewGame.onValueChanged.AddListener(OnNewGameValueChanged);
            ExistingGame.onValueChanged.AddListener(OnExistingGameValueChanged);
            CancelButton.onClick.AddListener(OnCanel);
            ContinueButton.onClick.AddListener(NextStep);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow();
            _publishSelectData = (PublishSelectData)args[0];
            if (args != null && args.Length > 1 && args[1] is GameType)
            {
                _gameType = (GameType)args[1];
                if (_gameType == GameType.AIGame)
                {
                    if (ColorUtility.TryParseHtmlString(AIGameHospitalConfig.hospitalThemeColor, out Color color))
                    {
                        _titleBg.color = color;
                    }
                    _toggleMark1.Switch(1);
                    _toggleMark2.Switch(1);
                }
            }
        }

        #endregion

        private void OnNewGameValueChanged(bool isOn)
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);

            if (isOn) newGame = true;
        }

        private void OnExistingGameValueChanged(bool isOn)
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);

            if (isOn) newGame = false;
        }

        private void OnCanel()
        {
            CloseSelf();
        }

        private void NextStep()
        {
            if (newGame)
            {
                //如果是通关图
                if (_publishSelectData.DraftListItem.mapInfo.IsPassLevelMap()&&_gameType!=GameType.AIGame)
                {
                    _publishSelectData.PublishPassLevelMapAct?.Invoke();
                }
                else
                {
                    _publishSelectData.PublishNormalMapAct?.Invoke();
                }
            }
            else
            {
                //覆盖更新
                if (_publishSelectData.DraftListItem.mapInfo.gameSetting.aiGameId == (int)PGCGameType.AIPark)
                {
                    var p = UIManager.Inst.OpenPanel<UpdateOnlineGamePanel>(PanelId.UpdateOnlineGamePanel, _publishSelectData.DraftListItem, _gameType, (int)PGCGameType.AIPark);
                }
                else
                {
                    var p = UIManager.Inst.OpenPanel<UpdateOnlineGamePanel>(PanelId.UpdateOnlineGamePanel, _publishSelectData.DraftListItem, _gameType);
                }

            }
            
            //关闭自己
            CloseSelf();
        }
    }
}
