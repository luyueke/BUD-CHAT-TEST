using Game.BudBox;
using Message;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台主内容面板，负责皮肤列表展示及操作按钮
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllShowPanel : MonoBehaviour
    {
        [SerializeField] private CabinControllSkinPanel SkinInturnPanel; // 换肤子面板
        [SerializeField] private CabinControllScenePanel ScenePanel;    // 场景选择界面

        [SerializeField] private CabinControllInturnPanel InturnPanel; // 互动子界面

        [SerializeField] private Button BackBtn;    // 返回/关闭按钮

        [Header("角色态")]
        [SerializeField] private GameObject NoPlayer;   // 无角色时显示的占位视图
        [SerializeField] private GameObject HasPlayer;  // 有角色时显示的内容视图


        [Header("操作按钮")]
        [SerializeField] private Button ActiveBtn;  // 唤醒按钮
        [SerializeField] private Text ActiveTex;  // 唤醒按钮
        [SerializeField] private Sprite ActiveOn;   // 唤醒图片
        [SerializeField] private Sprite ActiveOff;  // 待机图片


        [SerializeField] private Button CallBtn;    // 通话按钮
        [SerializeField] private Text CallTex;    // 通话按钮
        [SerializeField] private GameObject CallIng;    // 通话按钮
        [SerializeField] private Sprite CallOn;     // 发起通话图片（未通话时显示）
        [SerializeField] private Sprite CallOff;    // 挂断图片（通话中时显示）

        [SerializeField] private Sprite GraySprite; // 置灰图片（设备离线/连接中时显示）
        [SerializeField] private Color NormalTextColor; // 正常文本颜色
        [SerializeField] private Color GrayTextColor; // 置灰文本颜色

        [SerializeField] private Button SettingBtn;  // 设置按钮
        [SerializeField] private Button ImportBtn;  // 导入角色按钮（无角色态显示）

        // 由父级 IncubationCabinControll 注入的回调
        public Action onActiveClick;
        public Action onCallClick;
        public Action onBackClick;
        public Action onInturnClick;
        public Action onBGClick;
        public Action onSettingClick;
        public Action onImportClick;
        public Action<CabinCharacterBaseInfo> onSkinSelect;

        /// <summary>场景卡片被点击时触发，由父级 IncubationCabinControll 绑定处理</summary>
        public Action<object> onSceneSelect;


        private CabinCharacterUgcInfo _data; // 当前展示的角色数据

        private readonly List<CabinControllSkinItem> _skinItems = new List<CabinControllSkinItem>();

        /// <summary>小窗通话状态下，每秒刷新 CallBtn 文字的计时器</summary>
        private BudTimer _callDisplayTimer;

        /// <summary>CallTex 的原始文字，通话结束后用于恢复</summary>
        private string _callBtnDefaultText;

        #region 初始化

        /// <summary>
        /// 绑定所有按钮点击事件，在 OnCreate 阶段调用一次
        /// </summary>
        public void InitUI()
        {
            SkinInturnPanel.InitUI();
            SkinInturnPanel.onSceneClick = () =>
            {
                SkinInturnPanel.gameObject.SetActive(false);
                ScenePanel.gameObject.SetActive(true);
            };
            // 用闭包延迟读取 onSkinSelect，避免 InitUI 时 onSkinSelect 尚未被外部赋值的时序问题
            SkinInturnPanel.onSkinSelect = (info) => onSkinSelect?.Invoke(info);
            SkinInturnPanel.onChangePlayerClick = () => onImportClick?.Invoke();
            ScenePanel.InitUI();
            ScenePanel.onPlayerClick = () =>
            {
                SkinInturnPanel.gameObject.SetActive(true);
                ScenePanel.gameObject.SetActive(false);
            };
            // 桥接场景选中回调到父级，用闭包延迟读取，与 onSkinSelect 时序保持一致
            ScenePanel.onSceneSelect = (info) => onSceneSelect?.Invoke(info);
            // ── 按钮事件 ──
            BackBtn.onClick.AddListener(() => onBackClick?.Invoke());
            ActiveBtn.onClick.AddListener(() => onActiveClick?.Invoke());
            CallBtn.onClick.AddListener(() => onCallClick?.Invoke());
            SettingBtn.onClick.AddListener(() => onSettingClick?.Invoke());
            ImportBtn.onClick.AddListener(() => onImportClick?.Invoke());

            OnBudBoxActiveChange();
            OnBudBoxCallChange(CabinBoxManager.Inst.GetCurrentDeviceId());

            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxActiveChange, OnBudBoxActiveChange);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxCallChange, OnBudBoxCallChange);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);
        }

        private void OnBudBoxActiveChange(string deviceId = null)
        {
            if (!string.IsNullOrEmpty(deviceId) && deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
                return;

            // 设备非 Online 或未导入角色时显示置灰图片，不区分唤醒/待机状态
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online || _data == null)
            {
                ActiveBtn.image.sprite = GraySprite;
                ActiveTex.color = GrayTextColor;
                return;
            }
            ActiveTex.color = NormalTextColor;
            ActiveBtn.image.sprite = CabinBoxManager.Inst.GetActive(deviceId) == 0 ? ActiveOn : ActiveOff;
     
        }

        private void OnBudBoxCallChange(string deviceId = null)
        {
            if (!string.IsNullOrEmpty(deviceId) && deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
                return;

            // 设备非 Online 或未导入角色时显示置灰图片，不区分通话/未通话状态
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online || _data == null)
            {
                CallBtn.image.sprite = GraySprite;
                CallTex.color = GrayTextColor;
                StopCallDisplayTimer();
                return;
            }

            CallTex.color = NormalTextColor;

            int  call          = CabinBoxManager.Inst.GetCall(deviceId);
            long callBeginTime = CabinBoxManager.Inst.GetCabinBudBoxData(deviceId)?.deviceState.callBeginTime ?? 0;

            // 按钮图标：未通话=CallOn，通话中或连接中=CallOff
            CallBtn.image.sprite = call == 0 ? CallOn : CallOff;

            bool isInCall = call == 1 && callBeginTime > 0;

            if (isInCall)
            {
                // 通话已建立（小窗状态）：启动每秒刷新计时，展示通话时长
                if (_callDisplayTimer == null)
                {
                    // 首次进入时保存默认文字
                    if (_callBtnDefaultText == null)
                        _callBtnDefaultText = CallTex.text;

                    // 立即刷新一次，再按秒循环
                    RefreshCallBtnTime();
                    _callDisplayTimer = TimerManager.Inst.Run("CallBtnDisplayTimer", 1f, 1f, RefreshCallBtnTime);
                }
            }
            else
            {
                // 通话未建立或已结束：停止计时并恢复默认文字
                StopCallDisplayTimer();
            }
        }

        /// <summary>
        /// 每秒刷新 CallBtn 文字为当前通话时长（mm:ss）。
        /// 供小窗模式下控制台按钮展示通话进行时间。
        /// </summary>
        private void RefreshCallBtnTime()
        {
            long beginTime = CabinBoxManager.Inst.GetCabinBudBoxData()?.deviceState.callBeginTime ?? 0;

            if (beginTime <= 0)
            {
                StopCallDisplayTimer();
                return;
            }

            int seconds = Mathf.Max(0, (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - beginTime));
            int m = seconds / 60;
            int s = seconds % 60;
            CallTex.text = $"{m:D2}:{s:D2}";
            CallIng.SetActive(true);
        }

        /// <summary>停止通话时长计时器并恢复 CallBtn 的默认文字</summary>
        private void StopCallDisplayTimer()
        {
            if (_callDisplayTimer != null)
            {
                TimerManager.Inst.Stop(_callDisplayTimer);
                _callDisplayTimer = null;
            }

            // 仅当有保存的默认文字时才恢复，避免面板初始化前触发空引用
            if (_callBtnDefaultText != null && CallTex != null)
            {
                CallTex.text = _callBtnDefaultText;
            }
            CallIng.SetActive(false);
        }

        #endregion

        #region 显示 / 隐藏

        /// <summary>
        /// 刷新面板显示内容：根据是否有角色切换视图，并刷新皮肤列表
        /// </summary>
        public void OnShow(CabinCharacterUgcInfo data)
        {
            _data = data;
            OnBudBoxBaseDataRest(CabinBoxManager.Inst.GetCurrentDeviceId());
        }

        /// <summary>
        /// 面板隐藏时通知皮肤子面板清理资源
        /// </summary>
        public void OnHide()
        {
            SkinInturnPanel.OnHide();
        }

        #endregion

        #region 硬件控制回调

        private void OnBoxDeviceStateChanged(string deviceId)
        {
            if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId()) return;
            RefreshSliderState();
        }

        /// <summary>
        /// 根据设备连接状态刷新两个 Slider 的可交互性及 FillRect 颜色
        /// </summary>
        private void RefreshSliderState()
        {
            // 设备连接状态变化时，同步刷新操作按钮的图片（在线恢复正常图，离线/连接中显示置灰图）
            //RefreshActionBtnsVisibility();
            OnBudBoxActiveChange();
            OnBudBoxCallChange(CabinBoxManager.Inst.GetCurrentDeviceId());
        }

        private void OnBudBoxBaseDataRest(string deviceId)
        {
            if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
                return;
            bool hasCharacter = _data != null;

            // 根据角色状态切换视图和按钮可见性
            NoPlayer.SetActive(!hasCharacter);
            HasPlayer.SetActive(hasCharacter);
            ImportBtn.gameObject.SetActive(!hasCharacter);

            if (hasCharacter)
                RefreshSkinData();
            else
                ClearSkinItems();

            RefreshSliderState();

            OnBudBoxActiveChange(deviceId);
            OnBudBoxCallChange(deviceId);
        }

        #endregion

        #region 皮肤列表

        /// <summary>
        /// 直接跳转到场景选择面板（隐藏换肤面板，显示场景面板）
        /// </summary>
        public void ShowScenePanelDirect()
        {
            SkinInturnPanel.gameObject.SetActive(false);
            ScenePanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// 有角色时调用：将当前角色数据传给皮肤子面板，显示 HasPlayer 视图并加载皮肤列表
        /// </summary>
        private void RefreshSkinData()
        {
            SkinInturnPanel.OnShow(_data);
        }

        /// <summary>
        /// 无角色时调用：通知皮肤子面板切换到 NoPlayer 视图并清空皮肤列表
        /// </summary>
        private void ClearSkinItems()
        {
            SkinInturnPanel.OnShow(null);
        }

        #endregion
        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxActiveChange, OnBudBoxActiveChange);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnBudBoxCallChange);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxBaseDataRest, OnBudBoxBaseDataRest);

            // 清理通话时长计时器，防止面板销毁后回调野引用
            if (_callDisplayTimer != null)
                TimerManager.Inst.Stop(_callDisplayTimer);
        }
    }
}
