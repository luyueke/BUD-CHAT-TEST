using System;
using Es;
using Game.Base;
using Game.MapSetting;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using GameData.Manager;
using UI.UIWidgets;
using UnityEngine;

namespace UI.UIPanels.GameEdit.SettingView {
    public class GameSettingView : BaseSettingView {
        private NumberPicker _maxPlayerNumPicker;
        private ButtonToggleGroup _winConditionToggleGroup;
        private Transform _collectAllStarTrans;
        private NumberPicker _collectAllStartNumPicker;

        private ButtonToggleGroup _gameDurationToggleGroup;
        private Transform _gameDurationSettingGo;
        private NumberPicker _gameDurationNumberPicker;
        private string spawnPointID = "20100024";

        #region HP UI

        private ButtonToggleGroup gameHpToggleGroup;
        private Transform gameHpSettingGo;
        private NumberPicker gameHpNumberPicker;

        #endregion

        private void Awake() {
            InitPlayerNum();
            InitWinConditionUI();


            InitGameDurationUI();
            InitGameHpUI();
        }


        public override void OnTabSelected() {
            RefreshPlayerNumData();
            RefreshWinConditionData();
            RefreshGameDurationData();
            RefreshGameHpData();
        }

        #region 玩家数量设置

        private void InitPlayerNum() {
            _maxPlayerNumPicker = GameObjectEx.FindChildByName(this.gameObject, "MaxPlayers/EditorNumberPicker")
                .GetComponent<NumberPicker>();
            _maxPlayerNumPicker.SetNumberRange(1, 16, "最少设置1名玩家", "最多设置16名玩家");
            _maxPlayerNumPicker.SetInputKeyBoardInfo(new KeyBoardInfo {
                type = 0,
                placeHolder = "",
                inputMode = 1,
                maxLength = 3,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            });

            _maxPlayerNumPicker.SetNumberChangedListener(OnNumberChanged);
        }

        private void CreateSpawnPoint(int createCount) {
            for (int i = 0; i < createCount; i++) {
                NodeBaseBehaviour behaviour;
                var sManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
                var lastIndex = sManager.GetLastIndex();
                GamePropNodeManager.Inst.TryCreateInEdit(spawnPointID, out behaviour);
                var component = behaviour.entity.GetOrAddComp<SpawnPointComponent>();
                component.SpawnIndex = lastIndex + 1;
                var sBehaviour = behaviour as SpawnPointBehaviour;
                sBehaviour.SetIndex(component.SpawnIndex);
                sBehaviour.SetDefault(component.SpawnDefault);
            }
        }

        private void DeletaSpawnPoint(int delCount) {
            for (int i = 0; i < delCount; i++) {
                var sManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
                var removes = sManager.GetSpawnPointByLast(1);
                removes.ForEach(x => GamePropNodeManager.Inst.TryDeleteInEdit(x));
            }
        }

        private void RefreshPlayerNumData() {
            var sManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
            var count = sManager.GetLastIndex();
            _maxPlayerNumPicker.SetText(count);
        }

        private void OnNumberChanged(int oldValue, int newValue) {
            var sManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
            var count = sManager.GetLastIndex();
            var interval = newValue - count;
            if (interval >= 0) {
                CreateSpawnPoint(interval);
            } else {
                DeletaSpawnPoint(-interval);
            }
        }

        #endregion

        #region 通关条件设置

        private void InitWinConditionUI() {
            _winConditionToggleGroup = GameObjectEx
                .FindChildByName(this.gameObject, "WinningCondition/EditorButtonToggleGroup")
                .GetComponent<ButtonToggleGroup>();
            _collectAllStarTrans = GameObjectEx.FindChildByName(this.gameObject, "CollectAllStar");
            _collectAllStartNumPicker = GameObjectEx
                .FindChildByName(_collectAllStarTrans.gameObject, "EditorNumberPicker").GetComponent<NumberPicker>();

            var winConfigs = DataTables.GetWinConditionList();
            _winConditionToggleGroup.AddToggleItem("无", OnWinConditionChanged);
            foreach (var w in winConfigs) {
                _winConditionToggleGroup.AddToggleItem(w.SettingButtonText, OnWinConditionChanged);
            }

            //星星
            var minCount = CollectStarMgr.MinNum;
            var maxCount = CollectStarMgr.MaxNum;
            _collectAllStartNumPicker.SetNumberRange(minCount, maxCount, CollectStarMgr.MinHint,
                CollectStarMgr.MaxHint);
            _collectAllStartNumPicker.SetInputKeyBoardInfo(new KeyBoardInfo {
                type = 0,
                placeHolder = "",
                inputMode = 1,
                maxLength = 3,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            });

            _collectAllStartNumPicker.SetNumberChangedListener(OnCollectStarNumberPickerChanged);
        }

        private void RefreshWinConditionData() {
            var curWinConId = WinConditionManager.Inst.GetCurrentConditionId();
            _winConditionToggleGroup.SetToggleOnWithoutNotify(curWinConId);

            RefreshCollectStarUI();
        }

        private void OnWinConditionChanged(int index) {
            WinConditionManager.Inst.SetWinCondition(index, (success, msg) => {
                //可能设置失败，刷一下UI表现
                RefreshWinConditionData();
            });
        }

        #region 收集星星

        private CollectStarManager CollectStarMgr => GlobalNodeManager.Inst.Get<CollectStarManager>();

        private void RefreshCollectStarUI() {
            var curWinConId = WinConditionManager.Inst.GetCurrentConditionId();
            var isCollectStarCondition = (WinConditionType)curWinConId == WinConditionType.CollectAllStars;
            _collectAllStarTrans.gameObject.SetActive(isCollectStarCondition);
            if (!isCollectStarCondition) return;
            _collectAllStartNumPicker.SetTextWithoutNotify(CollectStarMgr.GetCurStarCount());
        }

        private void OnCollectStarNumberPickerChanged(int oldNum, int num) {
            var startCondition = WinConditionManager.Inst.GetCurrentWinCondition<CollectStarCondition>();
            startCondition.OnEditSetMultiStar(num);
            _collectAllStartNumPicker.SetTextWithoutNotify(num);
        }

        #endregion

        #endregion

        #region 通关时长设置

        private void InitGameDurationUI() {
            _gameDurationToggleGroup = GameObjectEx.FindChildByName(gameObject, "GameDuration/EditorButtonToggleGroup")
                .GetComponent<ButtonToggleGroup>();
            _gameDurationSettingGo = GameObjectEx.FindChildByName(gameObject, "GameDurationSetting");
            _gameDurationNumberPicker = GameObjectEx.FindChildByName(_gameDurationSettingGo, "EditorNumberPicker")
                .GetComponent<NumberPicker>();

            _gameDurationToggleGroup.AddToggleItem("无时长限制", OnGameDurationToggleChanged);
            _gameDurationToggleGroup.AddToggleItem("设置通关时长", OnGameDurationToggleChanged);

            _gameDurationNumberPicker.SetNumberRange(1, 20, "最小值tips", "最大值Tips");
            _gameDurationNumberPicker.SetNumberSuffix("分钟");
            _gameDurationNumberPicker.SetInputKeyBoardInfo(new KeyBoardInfo {
                type = 0,
                placeHolder = "",
                inputMode = 1,
                maxLength = 3,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            });

            _gameDurationNumberPicker.SetNumberChangedListener(OnGameDurationNumberPickerChanged);

            // 暂时关闭 游戏时长 设置
            _gameDurationSettingGo.gameObject.SetActive(false);
            _gameDurationToggleGroup.gameObject.SetActive(false);


        }

        private void RefreshGameDurationData() {
            if (GameDurationManager.Inst.DurationLimit > 0) {
                _gameDurationToggleGroup.SetToggleOnWithoutNotify(1);
                _gameDurationSettingGo.gameObject.SetActive(true);
                _gameDurationNumberPicker.SetTextWithoutNotify(GameDurationManager.Inst.DurationLimit / 60);
            } else {
                gameHpSettingGo.gameObject.SetActive(false);
                _gameDurationToggleGroup.SetToggleOnWithoutNotify(0);
            }
        }

        private void OnGameDurationToggleChanged(int index) {
            Debug.Log("fsc-----OnGameDurationToggleChanged---->" + index);
            _gameDurationSettingGo.gameObject.SetActive(index != 0);
            if (index == 1) {
                _gameDurationNumberPicker.SetTextWithoutNotify(20);
                GameDurationManager.Inst.DurationLimit = 20 * 60;
            } else {
                GameDurationManager.Inst.DurationLimit = -1;
            }


        }

        private void OnGameDurationNumberPickerChanged(int oldV, int newV) {
            GameDurationManager.Inst.DurationLimit = (newV * 60);
        }

        #endregion


        /// <summary>
        /// 游戏 HP 设置
        /// </summary>
        private void InitGameHpUI() {
            gameHpToggleGroup = GameObjectEx.FindChildByName(gameObject, "GameHp/EditorButtonToggleGroup")
                .GetComponent<ButtonToggleGroup>();
            gameHpSettingGo = GameObjectEx.FindChildByName(gameObject, "GameHpSetting");
            gameHpNumberPicker = GameObjectEx.FindChildByName(gameHpSettingGo, "EditorNumberPicker")
                .GetComponent<NumberPicker>();

            gameHpToggleGroup.AddToggleItem("无生命值限制", OnGameHpToggleChanged);
            gameHpToggleGroup.AddToggleItem("设置生命值上限", OnGameHpToggleChanged);

            gameHpNumberPicker.SetNumberRange(1, 6, "最小生命值为 1", " 最大生命值为 6");

            gameHpNumberPicker.SetNumberChangedListener(OnGameHpNumberPickerChanged);
            // 暂时关闭 游戏 HP 设置
            gameHpToggleGroup.gameObject.SetActive(false);
            gameHpSettingGo.gameObject.SetActive(false);
        }

        private void RefreshGameHpData() {
            if (HpManager.Inst.HpLimit > 0) {
                gameHpToggleGroup.SetToggleOnWithoutNotify(1);
                gameHpSettingGo.gameObject.SetActive(true);
                gameHpNumberPicker.SetTextWithoutNotify(HpManager.Inst.HpLimit);
            } else {
                gameHpSettingGo.gameObject.SetActive(false);
                gameHpToggleGroup.SetToggleOnWithoutNotify(0);
            }
        }

        private void OnGameHpToggleChanged(int value) {
            gameHpSettingGo.gameObject.SetActive(value != 0);
            if (value == 1) {
                gameHpNumberPicker.SetTextWithoutNotify(6);
                HpManager.Inst.HpLimit = (6);
            } else {
                HpManager.Inst.HpLimit = (-1);
            }
        }

        private void OnGameHpNumberPickerChanged(int oldV, int newV) {
            HpManager.Inst.HpLimit = (newV);
        }
    }
}
