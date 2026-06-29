using Game.BudBox;
using Game.MusicalInstrument;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台角色展示区，负责无/有角色态切换，管理导入和刷新按钮
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllBoxPanel : MonoBehaviour
    {
        [SerializeField] private Text BoxName;      // budbox名称文本
        [SerializeField] private Button ReNameBtn;  // 修改名称按钮（有角色态显示）
        [SerializeField] private Text OnLineStateText;    // 硬件在线/离线状态文本
        [SerializeField] private Image StateIcon;    // 硬件在线/离线状态图标
        [SerializeField] private Sprite[] StateSP;    // 硬件在线/离线状态图标

        [Header("亮屏/熄屏控制")]
        [SerializeField] private Button ScreenStateBtn;    // 亮屏/熄屏切换按钮（常驻显示）
        [SerializeField] private Sprite ScreenStateOnSP;   // 亮屏图标（当前亮屏时显示）
        [SerializeField] private Sprite ScreenStateOffSP;  // 熄屏图标（当前熄屏时显示）
        [SerializeField] private Text ScreenStateText;    // 硬件在线/离线状态文本

        [Header("角色预览")]
        [SerializeField] private BudBoxModel _budBoxModel;   // 负责角色与盒子模型的生成和销毁

        // 由父级 IncubationCabinControll 注入的回调
        public Action onImportClick;
        public Action onReNameClick;

        private UgcToneLoaderBehaviour _ugcToneLoader;
        private BudTimer _cmdPreviewTimer; // 口令预览定时器句柄，用于定时恢复播放按钮

        #region 初始化

        /// <summary>
        /// 绑定所有按钮点击事件，在 OnCreate 阶段调用一次
        /// </summary>
        public void InitUI()
        {
            ReNameBtn.onClick.AddListener(() => onReNameClick?.Invoke());
            ScreenStateBtn.onClick.AddListener(OnBacklightClicked);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxNameChange, RefreshBoxNameText);
            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.AddListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxScreenChange, OnBudBoxScreenChange);
        }

        #endregion

        #region 显示

        /// <summary>
        /// 根据是否有角色数据切换按钮显示状态，有角色时通过 BudBoxModel 创建模型并渲染到 RT
        /// </summary>
        public void OnShow(CabinCharacterUgcInfo data)
        {
            bool hasCharacter = data != null;

            var currentDeviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
            if (currentDeviceId != null)
                RefreshStateText(currentDeviceId);

            RefreshBoxNameText();
            RefreshBacklightState();

            if (hasCharacter)
                CreateCharacter(data);
            else
                RefreshWithoutCharacter();
        }

        public void OnHide()
        {
            DestroyCharacter();
        }

        /// <summary>
        /// 计算最终是否有待同步的变更（含皮肤包单独变化），结果暂未驱动按钮显隐（TODO: 切换 SyncBtn / NotSyncBtn）
        /// </summary>
        public void RefreshSyncState(bool hasChange)
        {
            // 如果外部未标记变更，还需额外检查皮肤包是否单独修改过
            if (!hasChange)
                hasChange = CabinBoxManager.Inst.IsSkinPackChanged();
        }

        /// <summary>
        /// 切换皮肤时只重建角色模型，无需刷新按钮状态
        /// </summary>
        public void RefreshSkin(CabinCharacterUgcInfo data)
        {
            CreateCharacter(data);
        }

        #endregion

        #region 角色模型

        /// <summary>
        /// 通过 BudBoxModel 异步创建角色模型并同步加载盒子场景，相机直接使用预设 RT
        /// </summary>
        private void CreateCharacter(CabinCharacterUgcInfo data)
        {
            _budBoxModel.Clear();

            // 提前获取盒子场景信息（闭包捕获），避免回调内再次查询
            var boxInfo = CabinBoxManager.Inst.GetCabinBudBoxData()?.boxInfo;

            CabinBoxManager.Inst.GetSkinPackInfo((defaultSkin) =>
            {
                // 回调内再次清理，防止多次快速调用时出现残留实例
                _budBoxModel.Clear();
                _budBoxModel.SetupCharacterFromSkinPack(defaultSkin);
                _budBoxModel.LoadBoxScene(boxInfo?.metaDataUrl);
            });
        }

        /// <summary>
        /// Box 场景同步结果回调：同步成功后立刻刷新 BudBoxModel 的盒子 3D 模型，
        /// 使预览视图与设备当前场景保持一致。
        /// </summary>
        /// <param name="isSuccess">true=同步成功</param>
        private void OnBoxSceneSyncResult(bool isSuccess)
        {
            if (!isSuccess)
                return;

            var boxInfo = CabinBoxManager.Inst.GetCabinBudBoxData()?.boxInfo;
            _budBoxModel.LoadBoxScene(boxInfo?.metaDataUrl);
        }

        /// <summary>
        /// 无角色态刷新：销毁已有角色模型，但根据 boxInfo 独立加载盒子场景。
        /// 场景有数据就显示，与有无角色无关。
        /// </summary>
        private void RefreshWithoutCharacter()
        {
            if (_cmdPreviewTimer != null)
            {
                TimerManager.Inst.Stop(_cmdPreviewTimer);
                _cmdPreviewTimer = null;
            }

            _budBoxModel.Clear();

            var boxInfo = CabinBoxManager.Inst.GetCabinBudBoxData()?.boxInfo;
            _budBoxModel.LoadBoxScene(boxInfo?.metaDataUrl);
        }

        /// <summary>销毁角色 GameObject，RT 本身由 Asset 管理，不在此释放</summary>
        private void DestroyCharacter()
        {
            // 角色销毁时同步停止口令预览定时器，防止面板关闭后触发已回收对象的回调
            if (_cmdPreviewTimer != null)
            {
                TimerManager.Inst.Stop(_cmdPreviewTimer);
                _cmdPreviewTimer = null;
            }

            _budBoxModel.Clear();
        }

        /// <summary>预览唤醒动作：在 BoxPanel 的角色上播放动画与语音</summary>
        public void PreviewActivation(characterInteraction interaction)
        {
            if (!_budBoxModel.HasCharacter) return;
            _budBoxModel.PlayController.InitData(interaction);
            // isMute == 1 时动画音效静音；语音 audioUrl 始终播放，不受 isMute 控制
            _budBoxModel.PlayController.PlayAnim(interaction.isMute != 1);
            if (!string.IsNullOrEmpty(interaction.audioUrl))
                GetToneLoader()?.LoadAudioClipAndPlay(PreviewAudioType.TwoD, interaction.audioUrl);
        }

        /// <summary>预览待机动作：在 BoxPanel 的角色上播放待机 Emote</summary>
        public void PreviewStand(pEmoteData data)
        {
            if (!_budBoxModel.HasCharacter) return;
            _budBoxModel.PlayController.InitData(data);
            // 待机动画不播放音效
            _budBoxModel.PlayController.PlayAnim(false);
        }

        /// <summary>
        /// 预览口令：在 BoxPanel 的角色上播放口令对应的动画与语音。
        /// 播放按钮的恢复由定时器控制（动画时长 + delaySecond），
        /// 避免播放控制器失败路径立即触发回调导致按钮瞬间恢复。
        /// </summary>
        /// <param name="voiceCommands">口令数据</param>
        /// <param name="onComplete">播放结束回调（定时器到期后触发），可为 null</param>
        public void PreviewVoiceCommands(voiceCommands voiceCommands, Action onComplete = null)
        {
            if (!_budBoxModel.HasCharacter) return;

            // 停止上一次口令预览定时器，防止残留
            if (_cmdPreviewTimer != null)
            {
                TimerManager.Inst.Stop(_cmdPreviewTimer);
                _cmdPreviewTimer = null;
            }

            // 不再将 onComplete 注入播放器——改由定时器控制恢复时机，避免失败路径立即触发回调
            _budBoxModel.PlayController.SetPlayEndCallback(null);
            _budBoxModel.PlayController.InitData(voiceCommands);
            // isMute == 1 时动画音效静音；语音 audioUrl 始终播放，不受 isMute 控制
            _budBoxModel.PlayController.PlayAnim(voiceCommands.isMute != 1);
            if (!string.IsNullOrEmpty(voiceCommands.audioUrl))
                GetToneLoader()?.LoadAudioClipAndPlay(PreviewAudioType.TwoD, voiceCommands.audioUrl);

            if (onComplete == null) return;

            // 记录动画开始时间，用于在时长查询完成后修正定时器剩余时间
            float playStartTime = Time.realtimeSinceStartup;

            // 异步查询动画时长，基于「动画时长 + delaySecond」启动定时器，时长到期后恢复播放按钮
            _budBoxModel.PlayController.FetchAnimMaxDelaySecond(voiceCommands, animDuration =>
            {
                int totalSeconds = (animDuration > 0 ? animDuration : 0) + voiceCommands.delaySecond;

                // 减去查询动画时长所消耗的时间，确保定时器是从动画开始起算
                float elapsed = Time.realtimeSinceStartup - playStartTime;
                float remainingSeconds = totalSeconds - elapsed;

                if (remainingSeconds <= 0f)
                {
                    onComplete();
                    return;
                }

                _cmdPreviewTimer = TimerManager.Inst.RunOnce("CabinBoxCmdPreview", remainingSeconds, () =>
                {
                    _cmdPreviewTimer = null;
                    onComplete();
                });
            });
        }

        /// <summary>
        /// 懒加载音效播放器：首次调用时从 GlobalMainCamera 获取并缓存，避免每次预览都执行 GameObject.Find
        /// </summary>
        private UgcToneLoaderBehaviour GetToneLoader()
        {
            if (_ugcToneLoader == null)
            {
                var soundObj = GameObject.Find("GlobalMainCamera");
                if (soundObj != null)
                    _ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            }
            return _ugcToneLoader;
        }


        private void OnBoxDeviceStateChanged(string deviceId)
        {
            if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId()) return;
            RefreshStateText(deviceId);
        }

        private void RefreshStateText(string deviceId)
        {
            if (OnLineStateText == null) return;
            var boxData = CabinBoxManager.Inst.GetCabinBudBoxData(deviceId);
            if (boxData == null) return;
            Color colorstart = Color.white;
            switch (boxData.deviceState.emBoxState)
            {
                case BoxState.Offline:
                    StateIcon.sprite = StateSP[0];
                    OnLineStateText.text = "离线";
                    if (ColorUtility.TryParseHtmlString($"#FFFFFF", out colorstart))
                        OnLineStateText.color = colorstart;
                    break;
                case BoxState.Connecting:
                    OnLineStateText.text = "连接中";
                    StateIcon.sprite = StateSP[2];
                    if (ColorUtility.TryParseHtmlString($"#FFBD53", out colorstart))
                        OnLineStateText.color = colorstart;
                    break;
                case BoxState.Online:
                    OnLineStateText.text = "在线";
                    StateIcon.sprite = StateSP[1];
                    if (ColorUtility.TryParseHtmlString($"#4DDD81", out colorstart))
                        OnLineStateText.color = colorstart;
                    break;
            }
        }

        private void RefreshBoxNameText(string deviceId)
        {
            var boxData = CabinBoxManager.Inst.GetCabinBudBoxData();

            if (boxData == null)
            {
                return;
            }

            if (!boxData.deviceId.Equals(deviceId))
            {
                return;
            }

            RefreshBoxNameText();
        }

        private void RefreshBoxNameText()
        {
            var boxData = CabinBoxManager.Inst.GetCabinBudBoxData();
            if (boxData == null)
                return;
            var name = boxData.deviceName ?? "";
            // 超过 5 个字符时截断为前 5 字 + "..."，避免设备名过长导致 UI 溢出
            BoxName.text = name.Length > 20 ? name.Substring(0, 20) + "..." : name;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用调试按钮：将角色重置为默认待机动作，便于调整截图效果。
        /// 仅在 Editor 下编译，不影响正式包体。
        /// </summary>
        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 160, 40), "恢复默认动作"))
            {
                _budBoxModel.PlayController.CancelAnim();
            }
        }
#endif

        private void OnDestroy()
        {
            DestroyCharacter();
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxNameChange, RefreshBoxNameText);
            MessageHelper.RemoveListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxScreenChange, OnBudBoxScreenChange);
        }

        #region 亮屏/熄屏控制

        /// <summary>
        /// 点击亮屏/熄屏按钮：切换当前屏幕状态并发送 MQTT 指令。
        /// 当前为亮屏（1）则切换为熄屏（0），反之亦然。
        /// 设备离线时不允许操作。
        /// </summary>
        private void OnBacklightClicked()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("设备未连接，无法操作。");
                return;
            }

            int currentScreen = CabinBoxManager.Inst.GetScreen();
            int targetScreen = currentScreen == 1 ? 0 : 1;

            // 先更新本地状态，立即广播刷新UI（乐观更新），再下发 MQTT 指令
            CabinBoxManager.Inst.SetScreen(targetScreen);
            MessageHelper.Broadcast<string>(MessageName.OnBudBoxScreenChange, CabinBoxManager.Inst.GetCurrentDeviceId());
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_screen);
        }

        /// <summary>
        /// 刷新亮屏/熄屏按钮图标，图标与当前状态一致（所见即所得）：
        /// screen=1（亮屏）显示亮屏图标；screen=0（熄屏）显示熄屏图标。
        /// 同时通知 BudBoxModel 更新3D预览状态。
        /// </summary>
        private void RefreshBacklightState()
        {
            if (ScreenStateBtn == null)
                return;

            int screen = CabinBoxManager.Inst.GetScreen();

            // screen=1 亮屏显示亮屏图标；screen=0 熄屏显示熄屏图标
            ScreenStateBtn.image.sprite = screen == 1 ? ScreenStateOnSP : ScreenStateOffSP;
            ScreenStateText.text = screen == 1 ? "亮屏" : "熄屏";
            // 通知 BudBoxModel 更新3D预览屏幕状态（视觉效果待后续实现）
            _budBoxModel.SetScreenOff(screen == 0);
        }

        /// <summary>
        /// 监听屏幕状态变更消息，仅处理当前选中设备的事件，刷新按钮图标。
        /// </summary>
        /// <param name="deviceId">触发变更的设备 ID</param>
        private void OnBudBoxScreenChange(string deviceId)
        {
            if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
                return;

            RefreshBacklightState();
        }

        #endregion

    }
}
