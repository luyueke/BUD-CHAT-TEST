using Basic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.BudBox;
using Message;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通讯录详情界面
    /// </summary>
    public class ChatCallTXDetailNode : MonoBehaviour
    {
        public RemoteImageBehaviour remoteImageBehaviour;
        public Text txtName;
        public Text txtTime; //认识时间 xx年xx月xx日

        public Button BtnMsg;
        public Button BtnCall;

        public GameObject call_Checking;  // 检测中
        public GameObject call_State1;    // 未通话（callBeginTime == 0）
        public GameObject call_State2;    // 通话中（callBeginTime > 0）
        public Text txt_callTotalTime;    // 当前通话时长 xx:xx，自动累加

        public Button closeBtn;

        public ChatCallPopNode chatCallPopNode;
        public ChatVideoCallNode chatVideoCallNode;

        private CabinPublishData _data;
        private readonly HashSet<string> _allSkinPackIds = new HashSet<string>();
        private bool _deviceListReady;
        private bool _skinPacksReady;
        private CabinBudBoxData _matchedBox;
        private bool _callStateKnown;
        private BudTimer _checkTimer;
        private BudTimer _callDisplayTimer;
        private int _callDisplaySeconds;

        // ─── 入口 ────────────────────────────────────────────────────────────

        public void Init(CabinPublishData data, Action<string> onMsg, Action<string> onCall)
        {
            _data = data;
            var info = data.characterInfo;

            if (remoteImageBehaviour != null && !string.IsNullOrEmpty(info?.characterPortraitUrl))
                remoteImageBehaviour.Load(info.characterPortraitUrl);

            if (txtName != null)
                txtName.text = info?.GetName() ?? string.Empty;

            if (txtTime != null && info != null && info.createTime > 0)
            {
                var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(info.createTime).ToLocalTime();
                txtTime.text = $"{dt:yyyy年MM月dd日}";
                UICommonUtils.RefreshLayout(txtTime.transform.parent);
            }

            BtnMsg?.onClick.RemoveAllListeners();
            BtnMsg?.onClick.AddListener(() => onMsg?.Invoke(info?.id ?? string.Empty));

            BtnCall?.onClick.RemoveAllListeners();
            BtnCall?.onClick.AddListener(OnBtnCallClick);

            closeBtn?.onClick.RemoveAllListeners();
            closeBtn?.onClick.AddListener(() => gameObject.SetActive(false));

            // 能量为 0 时无法发起通话，跳过设备检测，直接显示未通话态
            if (IsEnergyEmpty())
            {
                _callStateKnown = true;
                SetCallStateUI(false, true, false);
                return;
            }
            StartDetection();
        }

        public void OnEnable() { }

        void OnDisable()
        {
            CleanupListeners();
            CancelCheckTimer();
            StopCallDisplayTimer();
            CabinBoxManager.Inst.DisconnectMqtt();
        }

        // ─── 检测流程 ─────────────────────────────────────────────────────────

        private void StartDetection()
        {
            _deviceListReady = false;
            _skinPacksReady = false;
            _matchedBox = null;
            _callStateKnown = false;
            _allSkinPackIds.Clear();
            StopCallDisplayTimer();
            CancelCheckTimer();

            if (_data.characterInfo?.skinPack != null)
                foreach (var s in _data.characterInfo.skinPack)
                    if (!string.IsNullOrEmpty(s.packId)) _allSkinPackIds.Add(s.packId);

            SetCallStateUI(false, false, false); // 全隐藏，先检测
            call_Checking?.SetActive(true);

            MessageHelper.AddListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
            MessageHelper.AddListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnected);

            var extIds = _data.characterInfo?.extensionPackList;
            if (extIds != null && extIds.Count > 0)
                CabinNetManager.Inst.GetExtensionPackBatchInfo(extIds, OnExtensionPacksReady);
            else
                _skinPacksReady = true;

            CabinBoxManager.Inst.InitBudBoxDic(OnDeviceListReady);
        }

        private void OnExtensionPacksReady(bool success, List<CabinCharacterPackInfo> packs)
        {
            if (this == null) return;
            if (success && packs != null)
                foreach (var pack in packs)
                    if (pack?.skinPack != null)
                        foreach (var skin in pack.skinPack)
                            if (!string.IsNullOrEmpty(skin.packId)) _allSkinPackIds.Add(skin.packId);
            _skinPacksReady = true;
            TrySubscribeAndHeartbeat();
        }

        private void OnDeviceListReady(bool success)
        {
            if (this == null) return;
            if (!success || CabinBoxManager.Inst.GetBudBoxCount() == 0)
            {
                ShowResult(); // 无设备，直接认为未通话
                return;
            }
            _deviceListReady = true;
            TrySubscribeAndHeartbeat();
        }

        private void TrySubscribeAndHeartbeat()
        {
            if (!_deviceListReady || !_skinPacksReady) return;
            if (_callStateKnown) return;

            if (CabinBoxManager.Inst.IsConnected)
                DoSubscribeAndHeartbeat();
            else
                CabinBoxManager.Inst.ConnectMqttBroker();
        }

        private void OnMqttConnected()
        {
            if (!_deviceListReady || !_skinPacksReady || _callStateKnown) return;
            DoSubscribeAndHeartbeat();
        }

        private void DoSubscribeAndHeartbeat()
        {
            CabinBoxManager.Inst.SubscribeAllBoxes();
            CabinBoxManager.Inst.StartAllHeartbeatTimers();
            if (_checkTimer == null)
                _checkTimer = TimerManager.Inst.RunOnce("CallTXDetailCheck", 7f, ShowResult);
        }

        // ─── 结果处理 ────────────────────────────────────────────────────────

        private void OnBudBoxBaseDataRest(string deviceId)
        {
            if (_callStateKnown) return;
            var box = FindMatchedBox();
            if (box == null) return; // 等待其他设备的回包
            _matchedBox = box;
            CancelCheckTimer();
            ShowResult();
        }

        private void ShowResult()
        {
            if (this == null) return;
            _callStateKnown = true;
            CleanupListeners();
            CancelCheckTimer();
            call_Checking?.SetActive(false);

            if (_matchedBox == null)
                _matchedBox = FindMatchedBox(); // 超时后再尝试一次

            bool inCall = _matchedBox != null && _matchedBox.deviceState.callBeginTime > 0;
            SetCallStateUI(false, !inCall, inCall);

            if (inCall)
                StartCallDisplayTimer(_matchedBox.deviceState.callBeginTime);

            MessageHelper.AddListener<string>(MessageName.OnBudBoxCallChange, OnBudBoxCallChange);
        }

        private void OnBudBoxCallChange(string deviceId)
        {
            if (_matchedBox == null || deviceId != _matchedBox.deviceId) return;
            RefreshCallState();
        }

        public void RefreshCallState()
        {
            if (!_callStateKnown || _matchedBox == null) return;
            _matchedBox = CabinBoxManager.Inst.GetCabinBudBoxData(_matchedBox.deviceId) ?? _matchedBox;
            bool inCall = _matchedBox.deviceState.callBeginTime > 0;
            SetCallStateUI(false, !inCall, inCall);
            if (inCall)
                StartCallDisplayTimer(_matchedBox.deviceState.callBeginTime);
            else
                StopCallDisplayTimer();
        }

        private void SetCallStateUI(bool checking, bool state1, bool state2)
        {
            call_Checking?.SetActive(checking);
            call_State1?.SetActive(state1);
            call_State2?.SetActive(state2);
        }

        // ─── 通话计时（call_State2）───────────────────────────────────────────

        private void StartCallDisplayTimer(long callBeginTime)
        {
            StopCallDisplayTimer();
            _callDisplaySeconds = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - callBeginTime);
            if (_callDisplaySeconds < 0) _callDisplaySeconds = 0;
            RefreshCallDisplayTime();
            _callDisplayTimer = TimerManager.Inst.Run("CallTXDetailTimer", 1f, 1f, () =>
            {
                _callDisplaySeconds++;
                RefreshCallDisplayTime();
            });
        }

        private void RefreshCallDisplayTime()
        {
            if (txt_callTotalTime == null) return;
            int m = _callDisplaySeconds / 60;
            int s = _callDisplaySeconds % 60;
            txt_callTotalTime.text = $"{m:D2}:{s:D2}";
        }

        private void StopCallDisplayTimer()
        {
            if (_callDisplayTimer != null)
            {
                TimerManager.Inst.Stop(_callDisplayTimer);
                _callDisplayTimer = null;
            }
        }

        // ─── BtnCall 逻辑 ────────────────────────────────────────────────────

        private void OnBtnCallClick()
        {
            // 能量为 0 → 跳转能量不足界面，不进入通话流程
            if (IsEnergyEmpty())
            {
                UIManager.Inst.OpenPanel(PanelId.AICreditOverPanel);
                return;
            }

            if (!_callStateKnown)
            {
                TipPanel.ShowToast("正在检测角色状态");
                return;
            }
            if (_matchedBox != null && _matchedBox.deviceState.callBeginTime > 0)
            {
                // 通话中 → 恢复通话界面
                if (chatVideoCallNode != null)
                {
                    chatVideoCallNode.gameObject.SetActive(true);
                    chatVideoCallNode.Init(_data, _matchedBox);
                }
            }
            else
            {
                // 未通话 → 打开 ChatCallPopNode 发起通话
                if (chatCallPopNode != null)
                {
                    chatCallPopNode.gameObject.SetActive(true);
                    chatCallPopNode.Init(_data, () => chatCallPopNode.gameObject.SetActive(false));
                }
            }
        }

        private static bool IsEnergyEmpty()
        {
            var credit = AccountDataManager.Inst.BalanceInfo.AiCredit;
            return credit.dailyAmount <= 0 && credit.expiringAmount <= 0 && credit.permanentAmount <= 0;
        }

        // ─── 工具方法 ────────────────────────────────────────────────────────

        private CabinBudBoxData FindMatchedBox()
        {
            if (_allSkinPackIds.Count == 0) return null;
            foreach (var box in CabinBoxManager.Inst.GetBudBoxList())
                if (!string.IsNullOrEmpty(box.deviceState.skinPackId) &&
                    _allSkinPackIds.Contains(box.deviceState.skinPackId))
                    return box;
            return null;
        }

        private void CancelCheckTimer()
        {
            if (_checkTimer != null)
            {
                TimerManager.Inst.Stop(_checkTimer);
                _checkTimer = null;
            }
        }

        private void CleanupListeners()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
            MessageHelper.RemoveListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnected);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnBudBoxCallChange);
        }

        // ─── 测试 ────────────────────────────────────────────────────────────

        [Button("测试-未通话")]
        void TestState1()
        {
            SetupMock("小雅");
            call_Checking?.SetActive(false);
            call_State1?.SetActive(true);
            call_State2?.SetActive(false);
        }

        [Button("测试-通话中")]
        void TestState2()
        {
            SetupMock("小雅");
            long fakeBegin = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 75; // 模拟已通话75秒
            call_Checking?.SetActive(false);
            call_State1?.SetActive(false);
            call_State2?.SetActive(true);
            StartCallDisplayTimer(fakeBegin);
        }

        [Button("测试数据")]
        void testData()
        {
            var mockInfo = new CabinCharacterUgcInfo
            {
                name = "小雅",
                characterPortraitUrl = "",
                createTime = new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(),
                id = "test_id_001"
            };
            Init(new CabinPublishData(mockInfo),
                (id) => Debug.Log("[ChatCallTXDetailNode] BtnMsg id=" + id),
                (id) => Debug.Log("[ChatCallTXDetailNode] BtnCall id=" + id));
        }

        private void SetupMock(string charName)
        {
            var mockInfo = new CabinCharacterUgcInfo { name = charName, characterPortraitUrl = "", id = "mock" };
            _data = new CabinPublishData(mockInfo);
            if (txtName != null) txtName.text = charName;
            _callStateKnown = true;
        }
    }
}
