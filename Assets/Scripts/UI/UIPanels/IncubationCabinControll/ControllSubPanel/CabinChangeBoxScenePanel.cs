using Game.BudBox;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 切换盒子场景弹窗：在孵化舱控制台点击场景卡片后打开，
    /// 展示盒子 3D 模型 + 当前伙伴角色待机动画，用户确认后向硬件下发 set_scene MQTT 指令。
    /// 同步成功后关闭弹窗；同步失败（10s 超时）Toast 提示并保留弹窗。
    /// 3D 预览逻辑（角色 / 盒子模型）已封装到 <see cref="BudBoxModel"/>，
    /// 本面板只负责 UI 交互和同步状态管理。
    /// </summary>
    public class CabinChangeBoxScenePanel : BasePanel<CabinChangeBoxScenePanel>
    {
        /// <summary>3D 预览视图组件，负责角色与盒子模型的生成和销毁</summary>
        [SerializeField] private BudBoxModel _previewView;

        /// <summary>「切换」按钮，点击后发起同步</summary>
        [SerializeField] private Button SyncBtn;

        /// <summary>关闭按钮，手动关闭弹窗</summary>
        [SerializeField] private Button CloseBtn;

        /// <summary>「同步中...」文字对象，发起同步后显示，默认隐藏</summary>
        [SerializeField] private GameObject SyncingTextGo;

        /// <summary>「展示中」文字对象，当前选中场景已是设备场景时显示，默认隐藏</summary>
        [SerializeField] private GameObject ShowingTextGo;

        /// <summary>当前选中的场景数据，由 OnShow 注入</summary>
        private BoxSceneInfo _sceneInfo;

        /// <summary>是否处于等待 MQTT 回包状态</summary>
        private bool _isSyncing;

        /// <summary>同步超时计时器句柄</summary>
        private BudTimer _syncTimeoutTimer;

        /// <summary>同步超时时长（秒）</summary>
        private const float SyncTimeoutSeconds = 10f;

        #region 生命周期

        /// <summary>
        /// 绑定按钮事件并订阅同步结果消息，面板创建时调用一次。
        /// </summary>
        public override void OnCreate()
        {
            SyncBtn.onClick.AddListener(OnSyncClick);
            CloseBtn.onClick.AddListener(CloseSelf);
            MessageHelper.AddListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
        }

        /// <summary>
        /// 面板打开时：从参数取得场景数据，重置 UI 状态，调用预览组件生成角色和盒子模型。
        /// </summary>
        /// <param name="args">args[0]：BoxSceneInfo，点击的场景卡片数据</param>
        public override void OnShow(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                CloseSelf();
                return;
            }

            _sceneInfo = args[0] as BoxSceneInfo;

            if (_sceneInfo == null)
            {
                CloseSelf();
                return;
            }

            // 重置同步 UI 状态
            _isSyncing = false;
            SyncingTextGo.SetActive(false);

            // 判断是否已是当前设备展示的场景，若已是则直接显示「展示中」，隐藏「切换」按钮
            string currentScenePath = CabinBoxManager.Inst.GetBoxScene();
            bool isAlreadyShowing = !string.IsNullOrEmpty(_sceneInfo.id)
                                    && _sceneInfo.id == currentScenePath;

            SyncBtn.gameObject.SetActive(!isAlreadyShowing);
            ShowingTextGo.SetActive(isAlreadyShowing);

            // 获取角色数据后交由预览组件统一处理模型生成
            var characterInfo = CabinBoxManager.Inst.GetBoxCharacterData();
            _previewView.Setup(characterInfo, _sceneInfo);
        }

        /// <summary>
        /// 面板隐藏时：清理预览模型、停止超时计时器，重置同步状态。
        /// </summary>
        public override void OnHidden()
        {
            _isSyncing = false;
            StopSyncTimeoutTimer();
            _previewView.Clear();
        }

        /// <summary>
        /// 销毁时注销消息监听，并确保资源全部释放。
        /// </summary>
        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
            StopSyncTimeoutTimer();
            _previewView.Clear();
            base.OnDestroy();
        }

        #endregion

        #region 同步逻辑

        /// <summary>
        /// 点击「切换」按钮：检查设备在线状态和是否已导入角色，显示「同步中...」，向硬件下发 set_scene MQTT，并启动 10s 超时计时。
        /// </summary>
        private void OnSyncClick()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("设备离线无法同步");
                return;
            }

            if (CabinBoxManager.Inst.GetBoxCharacterData() == null)
            {
                TipPanel.ShowToast("请先导入角色");
                return;
            }

            _isSyncing = true;

            SyncBtn.gameObject.SetActive(false);
            SyncingTextGo.SetActive(true);

            CabinBoxManager.Inst.SetBoxScenePath(_sceneInfo?.id ?? string.Empty, _sceneInfo?.metaDataUrl);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_scene);

            // 切换场景交互埋点：用户点击「切换」确认应用时上报
            IncubationCabinControll.ReportThinkingData("switch_scene");

            StartSyncTimeoutTimer();
        }

        /// <summary>
        /// 接收 Box 场景同步结果广播（仅在 _isSyncing == true 时响应）。
        /// 同步成功则关闭弹窗；失败状态由超时机制（OnSyncTimeout）统一处理。
        /// </summary>
        /// <param name="isSuccess">true=同步成功，false=失败（当前未使用）</param>
        private void OnBoxSceneSyncResult(bool isSuccess)
        {
            if (!_isSyncing)
                return;

            if (!isSuccess)
                return;

            _isSyncing = false;
            StopSyncTimeoutTimer();

            // 场景同步成功，广播场景变更，通知列表面板刷新对应 Item 模型
            string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();

            if (!string.IsNullOrEmpty(deviceId))
            {
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxSceneChange, deviceId);
            }

            CloseSelf();
        }

        /// <summary>启动 10 秒同步超时计时器，超时后触发 OnSyncTimeout。</summary>
        private void StartSyncTimeoutTimer()
        {
            StopSyncTimeoutTimer();
            _syncTimeoutTimer = TimerManager.Inst.RunOnce("BoxSceneSyncTimeout", SyncTimeoutSeconds, OnSyncTimeout);
        }

        /// <summary>停止并清空超时计时器句柄，避免重复触发。</summary>
        private void StopSyncTimeoutTimer()
        {
            if (_syncTimeoutTimer != null)
            {
                TimerManager.Inst.Stop(_syncTimeoutTimer);
                _syncTimeoutTimer = null;
            }
        }

        /// <summary>
        /// 同步超时回调：重置同步状态，恢复「切换」按钮，并弹出 Toast 提示用户重试。
        /// </summary>
        private void OnSyncTimeout()
        {
            _isSyncing = false;
            _syncTimeoutTimer = null;
            SyncingTextGo.SetActive(false);
            SyncBtn.gameObject.SetActive(true);
            TipPanel.ShowToast("同步失败，请重试");
        }

        #endregion
    }
}
