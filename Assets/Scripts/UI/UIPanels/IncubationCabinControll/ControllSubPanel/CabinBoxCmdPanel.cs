using Game.BudBox;
using Message;
using System;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 唤醒指令面板（独立 UIPanel，通过 UIManager 管理生命周期）
    /// 从 IncubationCabinControll 点击「唤醒」后由 UIManager.OpenPanel 打开，负责：
    /// 1. 向 BUD BOX 发送唤醒指令（set_active=1）
    /// 2. 展示所有口令列表，支持单条预览播放
    /// 3. 「返回待机」将 Box 切回待机并关闭本面板
    /// 4. 设备离线时弹出通用弹窗并关闭本面板
    /// </summary>
    public class CabinBoxCmdPanel : BasePanel<CabinBoxCmdPanel>
    {
        [SerializeField] private Button BackBtn;        // 返回待机按钮
        [SerializeField] private GameObject CmdItemPrefab; // 口令列表 Item 预制体
        [SerializeField] private Transform CmdListContent; // ScrollRect 的 Content 容器

        /// <summary>口令播放回调：由打开方（IncubationCabinControll）通过 SetPlayVoiceCmd 注入</summary>
        private Action<voiceCommands, Action> _onPlayVoiceCmd;

        private readonly List<CabinBoxCmdItem> _cmdItems = new List<CabinBoxCmdItem>();

        #region 生命周期

        /// <summary>
        /// 绑定按钮事件并注册消息监听，整个生命周期仅执行一次
        /// </summary>
        public override void OnCreate()
        {
            BackBtn.onClick.AddListener(OnStandbyClicked);
            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        }

        /// <summary>
        /// 面板每次显示时：向 Box 发送唤醒指令，并根据当前角色数据刷新口令列表
        /// </summary>
        public override void OnShow(params object[] args)
        {
            // 向 Box 发送唤醒指令
            CabinBoxManager.Inst.SetActive(1);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_active);

            var characterInfo = CabinBoxManager.Inst.GetBoxCharacterData();
            RefreshCmdList(characterInfo?.usingVoiceCommands);
        }

        /// <summary>
        /// 面板隐藏时：清理口令列表，释放动态创建的 Item
        /// </summary>
        public override void OnHidden()
        {
            ClearCmdItems();
        }

        /// <summary>
        /// 销毁时：注销消息监听，清理残余 Item
        /// </summary>
        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            ClearCmdItems();
            base.OnDestroy();
        }

        #endregion

        /// <summary>
        /// 注入口令播放回调，由 IncubationCabinControll 在 OpenPanel 后调用，
        /// 将播放操作委托给 BoxPanel.PreviewVoiceCommands
        /// </summary>
        public void SetPlayVoiceCmd(Action<voiceCommands, Action> callback)
        {
            _onPlayVoiceCmd = callback;

            // 刷新已创建的 Item 的回调引用
            foreach (var item in _cmdItems)
                item.onPlayClick = _onPlayVoiceCmd;
        }

        #region 口令列表

        /// <summary>
        /// 清空并重建口令列表
        /// </summary>
        private void RefreshCmdList(List<voiceCommands> cmds)
        {
            ClearCmdItems();

            if (cmds == null)
                return;

            for (int i = 0; i < cmds.Count; i++)
            {
                var cmd = cmds[i];
                if (cmd == null || cmd.disabled == 1)
                {
                    continue;
                }
                var go = Instantiate(CmdItemPrefab, CmdListContent);
                go.SetActive(true);
                var item = go.GetComponent<CabinBoxCmdItem>();
                item.Init(cmds[i], i);
                item.onPlayClick = _onPlayVoiceCmd;
                _cmdItems.Add(item);
            }
        }

        private void ClearCmdItems()
        {
            foreach (var item in _cmdItems)
            {
                item.ClearData();
                Destroy(item.gameObject);
            }

            _cmdItems.Clear();
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 点击「返回待机」：向 Box 发送待机指令，并关闭本面板
        /// </summary>
        private void OnStandbyClicked()
        {
            CabinBoxManager.Inst.SetActive(0);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_active);
            CloseSelf();
        }

        #endregion

        #region 硬件状态监听

        private void OnBoxDeviceStateChanged(string deviceId)
        {
            if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
                return;

            // 仅在面板可见时响应，避免隐藏状态下重复触发
            if (!gameObject.activeInHierarchy)
                return;

            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
                ShowOfflinePopup();
        }

        /// <summary>
        /// 设备离线时弹出带标题的确认框，用户点击确认后关闭本面板回到控制台
        /// </summary>
        private void ShowOfflinePopup()
        {
            var popup = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            popup.SetTextAndAction(
                "BUD BOX已离线",
                "BUD BOX离线了,请先为BUD BOX连网吧",
                "确定",
                string.Empty,
                confirmClick: () => CloseSelf(),
                cancelClick: null
            );
        }

        #endregion
    }
}
