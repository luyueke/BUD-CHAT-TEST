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
    /// 某个伙伴CabinPublishData弹出的通话控制台界面(HttpUrlDefine.CabinBudBoxList获取budbox设备列表)
    /// </summary>
    public class ChatCallPopNode : MonoBehaviour
    {
        public GameObject checkingGo;             // 转圈检测 ui
        public GameObject node1;                  // box已与此伙伴绑定
        public Text node1_txtBoxTip;              // xx的BUD BOX
        public Text node1_txt_title;              // 在BUD BOX上和xx视频聊天
        public GameObject node1_onlineState;
        public GameObject node1_offlineState;
        public Button node1_onlineState_callBtn;  // 进入VideoCallNode
        public Button node1_onlineState_cancelBtn;
        public Button node1_offlineState_cancelBtn;

        public GameObject node2;                  // 账号下没有 BUD BOX
        public Button node2_buyBtn;
        public Button node2_cancelBtn;

        public ChatVideoCallNode chatVideoCallNode; // 通话中界面

        public GameObject node3;                  // 此伙伴未导入任何 box
        public Text node3_txtBoxTip;              // xx的BUD BOX
        public Button node3_importBtn;
        public GameObject node3_import_state1;    // 导入
        public GameObject node3_import_state2;    // 导入中
        public GameObject node3_onlineState;
        public GameObject node3_offlineState;
        public Button node3_cancelBtn;

        private CabinPublishData _character;
        private Action _onClose;
        private BudTimer _checkTimer;
        private bool _resultShown;

        // 皮肤包匹配集合（基础包 + 扩展包，Init 后异步填充）
        private readonly HashSet<string> _allSkinPackIds = new HashSet<string>();

        // 并行初始化完成标志
        private bool _deviceListReady;
        private bool _skinPacksReady;

        // 当前正在展示的 box 及所在 node（1 或 3），用于实时刷新在线/离线态
        private CabinBudBoxData _displayedBox;
        private int _displayedNode;

        // ─── 入口 ────────────────────────────────────────────────────────────

        public void Init(CabinPublishData data, Action onClose)
        {
            _character = data;
            _onClose = onClose;
            _resultShown = false;
            _deviceListReady = false;
            _skinPacksReady = false;
            _allSkinPackIds.Clear();

            // 基础皮肤包 ID 直接写入
            if (data.characterInfo?.skinPack != null)
                foreach (var s in data.characterInfo.skinPack)
                    if (!string.IsNullOrEmpty(s.packId)) _allSkinPackIds.Add(s.packId);

            CancelCheckTimer();
            SetAllNodes(false);
            checkingGo?.SetActive(true);

            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
            MessageHelper.AddListener(MessageName.OnImportBoxCharacterFinish, OnImportFinish);
            MessageHelper.AddListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnected);

            // ── 并行：设备列表 + 扩展包详情 ──────────────────────────────────
            var extIds = data.characterInfo?.extensionPackList;
            if (extIds != null && extIds.Count > 0)
            {
                CabinNetManager.Inst.GetExtensionPackBatchInfo(extIds, OnExtensionPacksReady);
            }
            else
            {
                _skinPacksReady = true; // 无扩展包，直接标记完成
            }

            CabinBoxManager.Inst.InitBudBoxDic(OnDeviceListReady);
        }

        public void OnEnable() { }

        void OnDisable()
        {
            CancelCheckTimer();
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
            MessageHelper.RemoveListener(MessageName.OnImportBoxCharacterFinish, OnImportFinish);
            MessageHelper.RemoveListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnected);
        }

        // ─── 并行初始化回调 ───────────────────────────────────────────────────

        private void OnExtensionPacksReady(bool success, List<CabinCharacterPackInfo> packs)
        {
            if (this == null) return;
            if (success && packs != null)
                foreach (var pack in packs)
                    if (pack?.skinPack != null)
                        foreach (var skin in pack.skinPack)
                            if (!string.IsNullOrEmpty(skin.packId)) _allSkinPackIds.Add(skin.packId);

            _skinPacksReady = true;
            TryStartHeartbeats();
        }

        private void OnDeviceListReady(bool success)
        {
            if (this == null) return;

            if (!success || CabinBoxManager.Inst.GetBudBoxCount() == 0)
            {
                ShowNode(2);
                return;
            }

            _deviceListReady = true;
            TryStartHeartbeats();
        }

        /// <summary>设备列表和扩展包都就绪后启动心跳流程</summary>
        private void TryStartHeartbeats()
        {
            if (!_deviceListReady || !_skinPacksReady) return;
            if (_resultShown) return;

            if (CabinBoxManager.Inst.IsConnected)
            {
                // MQTT 已连接，直接订阅并启动心跳
                StartSubscribeAndHeartbeat();
            }
            else
            {
                // MQTT 尚未连接，发起连接；连接成功后由 OnMqttConnected 继续
                CabinBoxManager.Inst.ConnectMqttBroker();
            }
        }

        /// <summary>MQTT 连接成功回调：若检测流程仍在等待则补齐订阅和心跳</summary>
        private void OnMqttConnected()
        {
            if (!_deviceListReady || !_skinPacksReady || _resultShown) return;
            StartSubscribeAndHeartbeat();
        }

        private void StartSubscribeAndHeartbeat()
        {
            // 先订阅所有设备 Topic，MQTT 才能将心跳回包路由到本端
            CabinBoxManager.Inst.SubscribeAllBoxes();
            CabinBoxManager.Inst.StartAllHeartbeatTimers();
            // 离线超时 6s，多留 1s 余量后强制出结果
            if (_checkTimer == null)
                _checkTimer = TimerManager.Inst.RunOnce("ChatCallPopCheck", 7f, ShowResult);
        }

        // ─── 消息监听 ────────────────────────────────────────────────────────

        private void OnBoxDeviceStateChanged(string deviceId)
        {
            if (_resultShown)
            {
                // 展示阶段：只刷新当前显示 box 的在线/离线状态
                RefreshDisplayedBoxState(deviceId);
                return;
            }
            if (!_skinPacksReady) return;
            // 检测阶段：skinPackId 由 OnBudBoxBaseDataRest 就绪后再匹配，此处不处理
        }

        private void RefreshDisplayedBoxState(string deviceId)
        {
            if (_displayedBox == null || _displayedBox.deviceId != deviceId) return;
            var box = CabinBoxManager.Inst.GetCabinBudBoxData(deviceId);
            if (box == null) return;
            bool online = box.deviceState.emBoxState == BoxState.Online;
            if (_displayedNode == 1)
            {
                node1_onlineState?.SetActive(online);
                node1_offlineState?.SetActive(!online);
            }
            else if (_displayedNode == 3)
            {
                node3_onlineState?.SetActive(online);
                node3_offlineState?.SetActive(!online);
            }
        }

        /// <summary>sync_baseMsg 回包后 skinPackId 已写入 _budBoxDic，此时再做完整匹配</summary>
        private void OnBudBoxBaseDataRest(string deviceId)
        {
            if (_resultShown || !_skinPacksReady) return;
            var matched = FindMatchedBox();
            if (matched != null && matched.deviceState.emBoxState == BoxState.Online)
            {
                CancelCheckTimer();
                ShowResult();
            }
        }

        private void OnImportFinish()
        {
            TimerManager.Inst.RunOnce("OnImportFinish", 2, () =>
            {
                if (this == null) return;
                node3_import_state1?.SetActive(true);
                node3_import_state2?.SetActive(false);

                // MQTT 确认后 _budBoxDic 中的 skinPackId 会更新，尝试切换到 node1
                var matched = FindMatchedBox();
                if (matched != null)
                {
                    node3?.SetActive(false);
                    ShowNode1(matched);
                }
            });
        }

        // ─── 结果展示 ────────────────────────────────────────────────────────

        private void ShowResult()
        {
            if (this == null || _resultShown) return;
            _resultShown = true;

            // 保留 OnBoxDeviceStateChanged 监听，展示后继续实时刷新在线/离线状态
            checkingGo?.SetActive(false);

            var matched = FindMatchedBox();

            if (matched != null)
            {
                ShowNode1(matched);
            }
            else
            {
                var best = FindBestBox();
                if (best != null)
                    ShowNode3(best);
                else
                    ShowNode(2);
            }
        }

        private void ShowNode1(CabinBudBoxData box)
        {
            _displayedBox = box;
            _displayedNode = 1;
            node1?.SetActive(true);
            string charName = _character?.characterInfo?.GetName() ?? string.Empty;
            if (node1_txtBoxTip != null) node1_txtBoxTip.text = $"{charName}的BUD BOX";
            if (node1_txt_title != null) node1_txt_title.text = $"在BUD BOX上和{charName}视频聊天";

            bool online = box.deviceState.emBoxState == BoxState.Online;
            node1_onlineState?.SetActive(online);
            node1_offlineState?.SetActive(!online);

            node1_onlineState_callBtn?.onClick.RemoveAllListeners();
            node1_onlineState_callBtn?.onClick.AddListener(() =>
            {
                if (chatVideoCallNode != null)
                {
                    chatVideoCallNode.gameObject.SetActive(true);
                    chatVideoCallNode.Init(_character, box);
                }
                _onClose?.Invoke();
            });
            node1_onlineState_cancelBtn?.onClick.RemoveAllListeners();
            node1_onlineState_cancelBtn?.onClick.AddListener(() => _onClose?.Invoke());
            node1_offlineState_cancelBtn?.onClick.RemoveAllListeners();
            node1_offlineState_cancelBtn?.onClick.AddListener(() => _onClose?.Invoke());
        }

        private void ShowNode3(CabinBudBoxData box)
        {
            _displayedBox = box;
            _displayedNode = 3;
            node3?.SetActive(true);
            string boxName = !string.IsNullOrEmpty(box.deviceName) ? box.deviceName : box.deviceId;
            if (node3_txtBoxTip != null) node3_txtBoxTip.text = $"{boxName}的BUD BOX";

            bool online = box.deviceState.emBoxState == BoxState.Online;
            node3_onlineState?.SetActive(online);
            node3_offlineState?.SetActive(!online);
            node3_import_state1?.SetActive(true);
            node3_import_state2?.SetActive(false);

            node3_importBtn?.onClick.RemoveAllListeners();
            node3_importBtn?.onClick.AddListener(() => OnNode3Import(box));

            node3_cancelBtn?.onClick.RemoveAllListeners();
            node3_cancelBtn?.onClick.AddListener(() => _onClose?.Invoke());
        }

        private void ShowNode(int nodeIndex)
        {
            if (this == null) return;
            _resultShown = true;
            checkingGo?.SetActive(false);
            node1?.SetActive(nodeIndex == 1);
            node2?.SetActive(nodeIndex == 2);
            node3?.SetActive(nodeIndex == 3);

            if (nodeIndex == 2)
            {
                node2_buyBtn?.onClick.RemoveAllListeners();
                node2_buyBtn?.onClick.AddListener(OnBuyBtnClick);
                node2_cancelBtn?.onClick.RemoveAllListeners();
                node2_cancelBtn?.onClick.AddListener(() => _onClose?.Invoke());
            }
        }

        // ─── 导入逻辑 ────────────────────────────────────────────────────────

        private void OnNode3Import(CabinBudBoxData box)
        {
            if (box.deviceState.emBoxState != BoxState.Online)
            {
                TipPanel.ShowToast("请先连接BUD BOX");
                LoggerUtils.Log("[ChatCallPopNode] 请先连接BUD BOX");
                return;
            }

            node3_import_state1?.SetActive(false);
            node3_import_state2?.SetActive(true);


            CabinBoxManager.Inst.SetActiveBox(box);
            CabinBoxManager.Inst.ImportBoxCharacterData(_character.characterInfo);

        }

        // ─── 工具方法 ────────────────────────────────────────────────────────

        /// <summary>
        /// 在 _allSkinPackIds（基础包 + 扩展包）中找到 skinPackId 匹配的 box
        /// </summary>
        private CabinBudBoxData FindMatchedBox()
        {
            if (_allSkinPackIds.Count == 0) return null;
            foreach (var box in CabinBoxManager.Inst.GetBudBoxList())
            {
                if (!string.IsNullOrEmpty(box.deviceState.skinPackId) &&
                    _allSkinPackIds.Contains(box.deviceState.skinPackId))
                    return box;
            }
            return null;
        }

        /// <summary>找最优 box：skinPackId 为空 + 在线 + 绑定时间戳最新（PlayerPrefs "BUDBOXBindTime_deviceId"）</summary>
        private CabinBudBoxData FindBestBox()
        {
            var boxes = CabinBoxManager.Inst.GetBudBoxList();
            if (boxes.Count == 0) return null;

            CabinBudBoxData best = null;
            int bestTime = -1;

            foreach (var box in boxes)
            {
                if (box.deviceState.emBoxState != BoxState.Online) continue;
                if (!string.IsNullOrEmpty(box.deviceState.skinPackId)) continue;

                int bindTime = PlayerPrefs.GetInt("BUDBOXBindTime_" + box.deviceId, 0);
                if (best == null || bindTime > bestTime)
                {
                    best = box;
                    bestTime = bindTime;
                }
            }

            return best;
        }

        private void SetAllNodes(bool active)
        {
            checkingGo?.SetActive(active);
            node1?.SetActive(active);
            node2?.SetActive(active);
            node3?.SetActive(active);
        }

        private void CancelCheckTimer()
        {
            if (_checkTimer != null)
            {
                TimerManager.Inst.Stop(_checkTimer);
                _checkTimer = null;
            }
        }

        private void OnBuyBtnClick()
        {
            // TODO: 跳转外部购买链接
            LoggerUtils.Log("[ChatCallPopNode] 购买BUD BOX");
        }

        // ─── 测试 ────────────────────────────────────────────────────────────

        [Button("测试-node1在线")]
        void TestNode1Online()
        {
            SetupMockState("小雅", "mock_pack_001");
            var box = MakeMockBox("TEST-001", "mock_pack_001", BoxState.Online);
            ShowNode1(box);
        }

        [Button("测试-node1离线")]
        void TestNode1Offline()
        {
            SetupMockState("小雅", "mock_pack_001");
            var box = MakeMockBox("TEST-001", "mock_pack_001", BoxState.Offline);
            ShowNode1(box);
        }

        [Button("测试-node2无设备")]
        void TestNode2()
        {
            SetupMockState("小雅", "mock_pack_001");
            ShowNode(2);
        }

        [Button("测试-node3未导入(在线)")]
        void TestNode3Online()
        {
            SetupMockState("小雅", "mock_pack_001");
            ShowNode3(MakeMockBox("TEST-002", "", BoxState.Online));
        }

        [Button("测试-node3未导入(离线)")]
        void TestNode3Offline()
        {
            SetupMockState("小雅", "mock_pack_001");
            ShowNode3(MakeMockBox("TEST-002", "", BoxState.Offline));
        }

        [Button("测试-扩展包匹配(node1在线)")]
        void TestExtPackMatch()
        {
            SetupMockState("小雅", baseSkinPackId: null, extSkinPackId: "ext_pack_007");
            var box = MakeMockBox("TEST-003", "ext_pack_007", BoxState.Online);
            ShowNode1(box);
        }

        private void SetupMockState(string charName, string baseSkinPackId = null, string extSkinPackId = null)
        {
            SetAllNodes(false);
            _resultShown = false;
            _allSkinPackIds.Clear();
            _onClose = () => gameObject.SetActive(false);
            var info = new CabinCharacterUgcInfo { name = charName, characterPortraitUrl = "", id = "char_mock" };
            if (!string.IsNullOrEmpty(baseSkinPackId))
                info.skinPack.Add(new SkinPackInfo { packId = baseSkinPackId });
            _character = new CabinPublishData(info);
            if (!string.IsNullOrEmpty(baseSkinPackId)) _allSkinPackIds.Add(baseSkinPackId);
            if (!string.IsNullOrEmpty(extSkinPackId)) _allSkinPackIds.Add(extSkinPackId);
        }

        private CabinBudBoxData MakeMockBox(string deviceId, string skinPackId, BoxState state)
        {
            var box = new CabinBudBoxData(deviceId) { deviceName = $"{deviceId}的BUD BOX" };
            box.deviceState.skinPackId = skinPackId;
            box.deviceState.emBoxState = state;
            return box;
        }
    }
}
