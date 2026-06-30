using Basic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Game.BudBox;
using Message;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通话界面节点，管理"连接中"和"通话中"两个状态的 UI 与逻辑。
    /// 可作为 AICompanionChatPanel 内的子节点使用（旧场景），
    /// 也可由 PorVideoCallNodePanel 包装后通过 UIManager 打开（控制台场景）。
    /// </summary>
    public class ChatVideoCallNode : MonoBehaviour
    {
        // ─── 外部绑定 UI ─────────────────────────────────────────────────────

        /// <summary>小窗/最小化按钮（不结束通话，仅关闭界面）</summary>
        public Button btnClose;

        /// <summary>远端角色头像</summary>
        public RemoteImageBehaviour remoteImg;

        /// <summary>角色名称</summary>
        public Text txtName;

        /// <summary>BUD BOX 音量滑块（0-100）</summary>
        public Slider slider_voice;

        /// <summary>通话时间（连接中显示"等待..."动效，通话中显示 mm:ss）</summary>
        public Text txt_callTime;

        /// <summary>结束/取消通话按钮</summary>
        public Button btnShutDown;

        /// <summary>结束/取消按钮文字</summary>
        public Text txt_shutdown;

        /// <summary>通讯录详情节点引用（AICompanionChatPanel 场景专用，可为 null）</summary>
        public ChatCallTXDetailNode chatCallTXDetailNode;

        // ─── 外部注入回调（PorVideoCallNodePanel 场景使用）────────────────────

        /// <summary>点击小窗按钮时触发（不挂断通话，仅关闭界面）。为 null 时退回旧行为。</summary>
        public Action onMinimize;

        /// <summary>连接失败（超时）后触发，通话已自动挂断。为 null 时退回旧行为。</summary>
        public Action onCallFailed;

        /// <summary>通话结束（主动挂断或被动挂断）后触发。为 null 时退回旧行为。</summary>
        public Action onCallEnded;

        // ─── 私有状态 ─────────────────────────────────────────────────────────

        private CabinBudBoxData _box;

        // 计时器句柄
        private BudTimer _callTimer;           // 通话中每秒计时
        private BudTimer _inviteTimer;         // 延迟 1s 后发送邀请
        private BudTimer _connectTimeoutTimer; // 15s 连接超时
        private BudTimer _minConnectTimer;     // 最少 3s 连接展示
        private BudTimer _dotAnimTimer;        // "..." 动效

        private int _callSeconds;                  // 通话累计秒数
        private bool _waitingForCallEnd;           // 是否正在等待挂断确认
        private bool _isWaitingForAccept;          // 是否处于"等待对方接受"阶段
        private bool _isConnectingSoundPlaying;    // 是否正在播放连接中等待音效

        private float _connectStartTime;       // 进入连接状态的 Time.time，用于保证最少 3s 展示
        private int _dotCount;                 // "..." 动效当前点数（0-3）

        // ─── 入口 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 初始化通话界面。
        /// callBeginTime == 0 → 发起新通话（连接中状态）；
        /// callBeginTime  > 0 → 恢复已建立的通话（通话中状态）。
        /// </summary>
        /// <param name="character">角色信息（头像、名称）</param>
        /// <param name="box">设备信息（MQTT 通信、音量等）</param>
        public void Init(CabinPublishData character, CabinBudBoxData box)
        {
            _box = CabinBoxManager.Inst.GetCabinBudBoxData(box.deviceId) ?? box;
            CabinBoxManager.Inst.SetActiveBox(_box);

            // 头像
            if (remoteImg != null && !string.IsNullOrEmpty(character.characterInfo?.characterPortraitUrl))
                remoteImg.Load(character.characterInfo.characterPortraitUrl);

            // 名称
            if (txtName != null)
                txtName.text = character.characterInfo?.GetName() ?? string.Empty;

            // 音量滑块
            if (slider_voice != null)
            {
                slider_voice.minValue = 0;
                slider_voice.maxValue = 100;
                slider_voice.value = CabinBoxManager.Inst.GetBoxVolume();
                slider_voice.onValueChanged.RemoveAllListeners();
                slider_voice.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (txt_shutdown != null)
                txt_shutdown.text = "取消";

            // 按钮事件
            btnShutDown?.onClick.RemoveAllListeners();
            btnShutDown?.onClick.AddListener(CancelOrEndCall);

            btnClose?.onClick.RemoveAllListeners();
            btnClose?.onClick.AddListener(OnMinimizeClick);

            // 清理上一次残留
            StopAllTimers();
            RemoveAllCallListeners();
            _isWaitingForAccept         = false;
            _waitingForCallEnd          = false;
            _dotCount                   = 0;
            _isConnectingSoundPlaying   = false;

            // 根据通话状态决定进入哪个阶段
            _callSeconds = _box.deviceState.callBeginTime > 0
                ? Mathf.Max(0, (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _box.deviceState.callBeginTime))
                : 0;

            if (_callSeconds == 0)
            {
                // 全新通话：进入连接中状态，1s 后发送邀请
                EnterConnectingState();
            }
            else
            {
                // 恢复通话（小窗后重新打开）：直接进入通话中状态
                EnterInCallState();
            }
        }

        public void OnEnable() { }

        private void OnDisable()
        {
            StopAllTimers();
            RemoveAllCallListeners();
            StopConnectingSound();

            _isWaitingForAccept = false;
            _waitingForCallEnd  = false;

            if (slider_voice != null)
                slider_voice.onValueChanged.RemoveAllListeners();
        }

        // ─── 连接中状态 ──────────────────────────────────────────────────────

        /// <summary>
        /// 进入"连接中"阶段：启动 "..." 动效、记录开始时间、延迟 1s 后发送邀请。
        /// </summary>
        private void EnterConnectingState()
        {
            _isWaitingForAccept = true;
            _connectStartTime   = Time.time;

            if (txt_callTime != null)
            {
                txt_callTime.alignment = TextAnchor.MiddleLeft;
                txt_callTime.text = "等待对方接受邀请";
            }

            if (txt_shutdown != null)
                txt_shutdown.text = "取消";

            // 启动 "..." 动效（每 0.5s 循环）
            _dotAnimTimer = TimerManager.Inst.Run("VideoCallDotAnim", 0.5f, 0.5f, OnDotAnimTick);

            // 延迟 1s 发送邀请（让 UI 先展示）
            _inviteTimer = TimerManager.Inst.RunOnce("VideoCallInviteDelay", 1f, SendCallInvite);

            // 播放等待对方接受邀请的循环音效
            _isConnectingSoundPlaying = true;
            AkSoundManager.Inst.PostEventAsync("Play_Phonecall_Loading", gameObject);
        }

        /// <summary>"..." 动效每帧回调，循环 0→1→2→3 个点</summary>
        private void OnDotAnimTick()
        {
            _dotCount = (_dotCount + 1) % 4;
            string dots = new string('.', _dotCount);

            if (txt_callTime != null)
                txt_callTime.text = $"等待对方接受邀请{dots}";
        }

        // ─── 邀请发送 & 接受回调 ─────────────────────────────────────────────

        /// <summary>延迟到期后发送 MQTT 通话邀请，并启动 15s 连接超时计时器</summary>
        private void SendCallInvite()
        {
            _inviteTimer = null;

            if (!_isWaitingForAccept)
                return;

            CabinBoxManager.Inst.SetCall(1, -1, _box?.deviceId);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_call, deviceId: _box?.deviceId);

            // 监听设备响应
            MessageHelper.AddListener<string>(MessageName.OnBudBoxCallChange, OnCallAccepted);

            // 15s 内无响应则判定为连接失败
            _connectTimeoutTimer = TimerManager.Inst.RunOnce(
                "VideoCallConnectTimeout", 15f, OnConnectTimeout);
        }

        /// <summary>
        /// 收到 MQTT 设备状态回包。
        /// call == 1 → 硬件确认通话建立，进入通话中（保证至少展示 3s 连接状态）。
        /// call != 1 → 通话被拒绝或失败，不进入计时。
        /// 注：硬件不提供 callBeginTime，直接以 call 状态作为建立判据，计时从 0 开始。
        /// </summary>
        private void OnCallAccepted(string deviceId)
        {
            if (_box != null && deviceId != _box.deviceId)
                return;

            if (!_isWaitingForAccept)
                return;

            if (CabinBoxManager.Inst.GetCall(_box?.deviceId) != 1)
                return; // 通话未建立（call ≠ 1），继续等待

            // 取消超时计时器
            CancelConnectTimeout();

            _isWaitingForAccept = false;
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallAccepted);

            // 保证连接中状态至少展示 3s
            float elapsed     = Time.time - _connectStartTime;
            float minDelay    = Mathf.Max(0f, 3f - elapsed);

            if (minDelay > 0f)
            {
                _minConnectTimer = TimerManager.Inst.RunOnce(
                    "VideoCallMinConnectDelay", minDelay, EnterInCallState);
            }
            else
            {
                EnterInCallState();
            }
        }

        /// <summary>连接超时（15s 内 callBeginTime 未更新）→ 自动挂断并通知失败</summary>
        private void OnConnectTimeout()
        {
            _connectTimeoutTimer = null;

            if (!_isWaitingForAccept)
                return;

            _isWaitingForAccept = false;
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallAccepted);

            // 停止动效和等待音效
            StopDotAnimTimer();
            StopConnectingSound();

            // 自动挂断
            CabinBoxManager.Inst.SetCall(0, -1, _box?.deviceId);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_call, deviceId: _box?.deviceId);

            TipPanel.ShowToast("通话连接失败，请重新连接");

            LoggerUtils.Log($"[ChatVideoCallNode] 连接超时，已自动挂断。deviceId={_box?.deviceId}");

            if (onCallFailed != null)
                onCallFailed.Invoke();
            else
                gameObject.SetActive(false);
        }

        // ─── 通话中状态 ──────────────────────────────────────────────────────

        /// <summary>
        /// 进入"通话中"阶段：停止连接相关计时器，更新按钮文字，启动通话计时。
        /// </summary>
        private void EnterInCallState()
        {
            _minConnectTimer = null;

            // 守卫：等待最短展示时长期间，若通话已被外部终止则直接退出
            if (CabinBoxManager.Inst.GetCall(_box?.deviceId) == 0)
            {
                LoggerUtils.Log($"[ChatVideoCallNode] 进入通话中前检测到通话已结束，直接关闭。deviceId={_box?.deviceId}");

                if (onCallEnded != null)
                    onCallEnded.Invoke();
                else
                    gameObject.SetActive(false);

                return;
            }

            // 停止连接阶段的辅助计时器，并停止等待音效
            StopDotAnimTimer();
            StopConnectingSound();

            if (txt_shutdown != null)
                txt_shutdown.text = "结束通话";

            // 若 callBeginTime 尚未由硬件回包设置，客户端兜底生成时间戳，
            // 确保 CabinControllShowPanel 等监听方能以 callBeginTime > 0 判断通话进行中
            if ((_box?.deviceState.callBeginTime ?? 0) <= 0)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                CabinBoxManager.Inst.SetCall(1, now, _box?.deviceId);
                LoggerUtils.Log($"[ChatVideoCallNode] callBeginTime 为 0，客户端兜底生成：{now}，deviceId={_box?.deviceId}");
            }

            // 从 callBeginTime 计算已通话秒数（重新进入时同步时间）
            _callSeconds = _box?.deviceState.callBeginTime > 0
                ? Mathf.Max(0, (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _box.deviceState.callBeginTime))
                : 0;

            RefreshCallTime();

            // 启动每秒计时
            _callTimer = TimerManager.Inst.Run("VideoCallTimer", 1f, 1f, OnCallTimerTick);

            // 监听外部挂断（后端超时销毁房间、设备主动挂断）
            MessageHelper.AddListener<string>(MessageName.OnBudBoxCallChange, OnCallEndedExternally);

            // 监听设备断线
            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxOfflineDuringCall);

            LoggerUtils.Log($"[ChatVideoCallNode] 进入通话中状态，已通话 {_callSeconds}s。deviceId={_box?.deviceId}");

            // 广播通话进入"通话中"状态，使 CabinControllShowPanel 的 CallTex 同步显示通话时长
            MessageHelper.Broadcast<string>(MessageName.OnBudBoxCallChange, _box?.deviceId);
        }

        /// <summary>每秒计时回调</summary>
        private void OnCallTimerTick()
        {
            _callSeconds++;
            RefreshCallTime();
        }

        /// <summary>将 _callSeconds 格式化为 mm:ss 显示</summary>
        private void RefreshCallTime()
        {
            if (txt_callTime == null)
                return;

            int m = _callSeconds / 60;
            int s = _callSeconds % 60;
            txt_callTime.alignment = TextAnchor.MiddleCenter;
            txt_callTime.text = $"{m:D2}:{s:D2}";
        }

        /// <summary>
        /// 监听到 OnBudBoxCallChange 时，若通话状态变为 0（外部挂断），结束界面。
        /// 覆盖场景：后端 30s 无声销毁房间、设备主动挂断。
        /// </summary>
        private void OnCallEndedExternally(string deviceId)
        {
            if (_box != null && deviceId != _box.deviceId)
                return;

            if (CabinBoxManager.Inst.GetCall(_box?.deviceId) != 0)
                return;

            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallEndedExternally);
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxOfflineDuringCall);

            StopCallTimer();

            LoggerUtils.Log($"[ChatVideoCallNode] 外部挂断（后端/设备）。deviceId={_box?.deviceId}");

            if (onCallEnded != null)
                onCallEnded.Invoke();
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// 通话中检测到设备断线 → 弹通用提示弹窗，结束通话界面。
        /// </summary>
        private void OnBoxOfflineDuringCall(string deviceId)
        {
            if (_box != null && deviceId != _box.deviceId)
                return;

            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Offline)
                return;

            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxOfflineDuringCall);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallEndedExternally);

            StopCallTimer();

            LoggerUtils.Log($"[ChatVideoCallNode] 通话中设备断线。deviceId={_box?.deviceId}");

            // 弹 Box 通用带标题弹窗，用户确认后关闭通话界面
            var disconnectPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(
                PanelId.CommonBoxConfirmWithTitlePanel);
            disconnectPanel.SetTextAndAction(
                titleText:    "连接断开",
                contentText:  "设备已断开连接，通话已结束。",
                confirmText:  "确定",
                cancelText:   null,
                confirmClick: () =>
                {
                    if (onCallEnded != null)
                        onCallEnded.Invoke();
                    else
                        gameObject.SetActive(false);
                },
                cancelClick:  null
            );
        }

        // ─── 音量 ────────────────────────────────────────────────────────────

        /// <summary>音量滑块变化回调，同步到 BUD BOX 硬件</summary>
        private void OnVolumeChanged(float value)
        {
            CabinBoxManager.Inst.SetBoxVolume((int)value);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_volume);
        }

        // ─── 按钮事件 ────────────────────────────────────────────────────────

        /// <summary>
        /// 小窗按钮点击：不结束通话，仅关闭界面。
        /// 有 onMinimize 回调时优先使用（PorVideoCallNodePanel 场景），否则退回旧行为。
        /// </summary>
        private void OnMinimizeClick()
        {
            if (onMinimize != null)
            {
                onMinimize.Invoke();
            }
            else
            {
                gameObject.SetActive(false);

                if (chatCallTXDetailNode != null)
                    chatCallTXDetailNode.RefreshCallState();
            }
        }

        /// <summary>
        /// 结束/取消按钮点击：
        /// 连接中 → 取消邀请并挂断；
        /// 通话中 → 结束通话。
        /// </summary>
        private void CancelOrEndCall()
        {
            if (_waitingForCallEnd)
                return;

            // 连接中阶段：取消等待，不需要发两次挂断
            if (_isWaitingForAccept)
            {
                _isWaitingForAccept = false;
                CancelInviteTimer();
                CancelConnectTimeout();
                StopDotAnimTimer();
                StopConnectingSound();
                MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallAccepted);
            }

            _waitingForCallEnd = true;
            StopCallTimer();

            CabinBoxManager.Inst.SetCall(0, -1, _box?.deviceId);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_call, deviceId: _box?.deviceId);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxCallChange, OnCallChangeConfirmed);

            LoggerUtils.Log($"[ChatVideoCallNode] 用户主动结束通话。deviceId={_box?.deviceId}");
        }

        /// <summary>硬件确认挂断后，关闭通话界面</summary>
        private void OnCallChangeConfirmed(string deviceId)
        {
            if (_box != null && deviceId != _box.deviceId)
                return;

            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallChangeConfirmed);
            _waitingForCallEnd = false;

            if (onCallEnded != null)
                onCallEnded.Invoke();
            else
                gameObject.SetActive(false);
        }

        // ─── 计时器管理 ──────────────────────────────────────────────────────

        private void StopCallTimer()
        {
            if (_callTimer != null)
            {
                TimerManager.Inst.Stop(_callTimer);
                _callTimer = null;
            }
        }

        private void CancelInviteTimer()
        {
            if (_inviteTimer != null)
            {
                TimerManager.Inst.Stop(_inviteTimer);
                _inviteTimer = null;
            }
        }

        private void CancelConnectTimeout()
        {
            if (_connectTimeoutTimer != null)
            {
                TimerManager.Inst.Stop(_connectTimeoutTimer);
                _connectTimeoutTimer = null;
            }
        }

        private void CancelMinConnectTimer()
        {
            if (_minConnectTimer != null)
            {
                TimerManager.Inst.Stop(_minConnectTimer);
                _minConnectTimer = null;
            }
        }

        private void StopDotAnimTimer()
        {
            if (_dotAnimTimer != null)
            {
                TimerManager.Inst.Stop(_dotAnimTimer);
                _dotAnimTimer = null;
            }
        }

        /// <summary>一次性停止所有计时器</summary>
        private void StopAllTimers()
        {
            StopCallTimer();
            CancelInviteTimer();
            CancelConnectTimeout();
            CancelMinConnectTimer();
            StopDotAnimTimer();
        }

        /// <summary>
        /// 停止"连接中"循环等待音效（Play_Phonecall_Loading）。
        /// 通过 _isConnectingSoundPlaying 标志防止重复调用 Stop 事件。
        /// </summary>
        private void StopConnectingSound()
        {
            if (!_isConnectingSoundPlaying)
                return;

            _isConnectingSoundPlaying = false;
            AkSoundManager.Inst.PostEventAsync("Stop_Phonecall_Loading", gameObject);
        }

        // ─── 消息监听清理 ────────────────────────────────────────────────────

        /// <summary>移除本节点注册的所有消息监听，防止多次订阅和内存泄漏</summary>
        private void RemoveAllCallListeners()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallAccepted);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallChangeConfirmed);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallEndedExternally);
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxOfflineDuringCall);
        }

        // ─── 测试 ────────────────────────────────────────────────────────────

        [Button("测试数据")]
        private void TestData()
        {
            var mockInfo = new CabinCharacterUgcInfo { name = "小雅", characterPortraitUrl = "", id = "mock" };
            var mockBox  = new CabinBudBoxData("TEST-001") { deviceName = "我的BUD BOX" };
            mockBox.deviceState.volume = 70;
            Init(new CabinPublishData(mockInfo), mockBox);
        }
    }
}
