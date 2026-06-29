using Game.BudBox;
using Message;
using Sirenix.OdinInspector;
using UI.Base;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: BOX控制台顶层面板，负责初始化各子面板并管理子面板切换
    /// Date: 26-04-09
    /// </summary>
    public class IncubationCabinControll : BasePanel<IncubationCabinControll>
    {
        [SerializeField] private CabinControllBoxPanel BoxPanel;           // 角色展示区（无/有角色态）
        [SerializeField] private CabinControllShowPanel ShowPanel;         // 主操作面板（皮肤列表 + 功能按钮）
        [SerializeField] private CabinControllSettingPanel SettingPanel;   // 设置子面板

        private CabinBudBoxData _boxDate => CabinBoxManager.Inst.GetCabinBudBoxData(); // 当前绑定的 BudBox 硬件配置数据
        private CabinCharacterUgcInfo _characterInfo => CabinBoxManager.Inst.GetBoxCharacterData(); // 当前绑定的角色 UGC 信息（外观/皮肤等）

        // 详情子面板集合（与控制台主视图互斥显示）
        private MonoBehaviour[] _detailPanels;

        #region 生命周期

        public override void OnCreate()
        {
            _detailPanels = new MonoBehaviour[] { ShowPanel, SettingPanel };
            BoxPanel.InitUI();
            ShowPanel.InitUI();
            SettingPanel.InitUI();

            BoxPanel.onImportClick = OnImportClick;
            BoxPanel.onReNameClick = OnReNameClick;

            ShowPanel.onBackClick = CloseSelf;
            ShowPanel.onActiveClick = OnActiveClick;
            ShowPanel.onCallClick = OnCallClick;
            ShowPanel.onImportClick = OnImportClick;
            ShowPanel.onSettingClick = ShowSettingPanel;
            ShowPanel.onSkinSelect = OnSkinSelect;
            ShowPanel.onSceneSelect = OnSceneSelect;

            SettingPanel.onBackClick = CloseSelf;
            SettingPanel.onShowClick = ShowControllPanel;
            SettingPanel.onNewLinkBoxClick = OnNewLinkBoxClick;
            SettingPanel.onUnLinkBoxClick = OnUnLinkBoxClick;

            MessageHelper.AddListener(MessageName.OnImportBoxCharacterFinish, RefreshCharacterData);
            MessageHelper.AddListener<bool>(MessageName.OnDetectionChange, OnDetectionChange);
            MessageHelper.AddListener(MessageName.OnDelectBoxCharacterFinish, OnDelectBoxCharacterFinish);
            MessageHelper.AddListener(MessageName.OnActiveBudBoxChanged, RefreshCharacterData);

        }

        /// <summary>
        /// 面板显示时刷新角色数据并重置到控制台主视图
        /// </summary>
        public override void OnShow(params object[] args)
        {
            RefreshCharacterData();
            // CabinBoxManager.Inst.RequestLatestBaseVersion();
        }

        /// <summary>
        /// 面板隐藏时通知各子面板清理状态
        /// </summary>
        public override void OnHidden()
        {
            BoxPanel.OnHide();
            ShowPanel.OnHide();
        }

        #endregion

        #region 数据拉取

        /// <summary>
        /// 重置到控制台主视图（数据已由 CabinBoxManager 缓存，直接刷新显示）
        /// </summary>
        private void RefreshCharacterData()
        {
            ShowControllPanel();
        }

        /// <summary>
        /// 角色删除完成消息回调：回到控制台主视图（无角色态）
        /// </summary>
        private void OnDelectBoxCharacterFinish()
        {
            ShowControllPanel();
        }

        /// <summary>
        /// 同步角色数据到硬件：通过 MQTT 发送 set_character 指令（携带 skinPackId）
        /// </summary>
        private void SyncCharacterData()
        {
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_character);
        }

        #endregion

        #region 子面板切换

        /// <summary>
        /// 回到控制台主视图：隐藏所有详情子面板，刷新 BoxPanel 和 BasePanel
        /// </summary>
        private void ShowControllPanel()
        {
            foreach (var p in _detailPanels)
                p.gameObject.SetActive(false);

            BoxPanel.OnShow(_characterInfo);
            ShowPanel.gameObject.SetActive(true);
            ShowPanel.OnShow(_characterInfo);
        }

        /// <summary>
        /// 打开设置子面板（需设备存在且在线，否则提示）
        /// </summary>
        private void ShowSettingPanel()
        {
            if (CabinBoxManager.Inst.GetCabinBudBoxData() == null)
            {
                TipPanel.ShowToast("设备不存在!");
                return;
            }
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                // 离线时仍允许打开设置面板（可查看配置但无法实际下发指令）
                // TipPanel.ShowToast("设备离线无法设置!");
                // return;
            }
            ShowPanel.gameObject.SetActive(false);
            SettingPanel.gameObject.SetActive(true);
            SettingPanel.OnShow();
        }
        #endregion

        #region 皮肤选中

        /// <summary>
        /// 皮肤选中回调：更新本地 pending 状态后，通过 UIManager 打开皮肤预览面板。
        /// </summary>
        private void OnSkinSelect(CabinCharacterBaseInfo skin)
        {
            // 更新本地 pending 状态（isDefault + skinPackId），不立即下发硬件
            //CabinBoxManager.Inst.SetCharacterUseSkin(skin.skinPack[0]);


        }

        /// <summary>
        /// 场景卡片选中回调：直接打开切换盒子场景弹窗。
        /// 设备在线/角色导入状态检查由弹窗内的同步按钮点击时处理。
        /// </summary>
        private void OnSceneSelect(object info)
        {
            UIManager.Inst.OpenPanel(PanelId.CabinChangeBoxScenePanel, info);
        }

        #endregion

        #region 数据变更检测

        /// <summary>
        /// 数据变更广播回调：刷新同步状态；当 MQTT 确认同步完成（hasChange=false）时同步刷新角色模型。
        /// </summary>
        private void OnDetectionChange(bool hasChange)
        {
            BoxPanel.RefreshSyncState(hasChange);

            // MQTT 回包确认同步后，刷新 BoxPanel 角色模型为新皮肤
            if (!hasChange && _characterInfo != null)
            {
                BoxPanel.RefreshSkin(_characterInfo);
            }
        }

        #endregion

        #region 其他
        /// <summary>
        /// 唤醒按钮点击：通过 UIManager 打开唤醒指令面板，并注入口令播放回调
        /// </summary>
        private void OnActiveClick()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("设备未连接，无法唤醒。");
                return;
            }

            if (_characterInfo == null)
            {
                TipPanel.ShowToast("请先导入角色");
                return;
            }

            string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();

            if (string.IsNullOrEmpty(deviceId))
                return;

            if (!GlobalFuncExtensions.CheckCanClick())
            {
                TipPanel.ShowToast("操作太频繁");
                return;
            }

            var panel = UIManager.Inst.OpenPanel<CabinBoxCmdPanel>(PanelId.CabinBoxCmdPanel);
            // 点击播放：发送 MQTT 通知硬件播放口令，本地不再预览动画和语音
            panel.SetPlayVoiceCmd((cmd, onDone) =>
            {
                CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_active, textID: cmd.text);
                onDone?.Invoke();
            });
        }

        /// <summary>
        /// 通话按钮点击。
        /// call=0：发起新通话；call=1 且 callBeginTime>0：小窗状态重进通话界面。
        /// call=1 且 callBeginTime=0（连接中）时面板已打开，忽略重复点击。
        /// </summary>
        private void OnCallClick()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("设备未连接，无法通话。");
                return;
            }

            if (_characterInfo == null)
            {
                TipPanel.ShowToast("请先导入角色");
                return;
            }

            string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();

            if (string.IsNullOrEmpty(deviceId))
                return;

            if (!GlobalFuncExtensions.CheckCanClick())
            {
                TipPanel.ShowToast("操作太频繁");
                return;
            }

            int call = CabinBoxManager.Inst.GetCall();
            long callBeginTime = _boxDate?.deviceState.callBeginTime ?? 0;

            if (call == 0)
            {
                // 发起全新通话
                OpenCallPanel();
            }
            else if (call == 1 && callBeginTime > 0)
            {
                // 通话已建立（小窗状态），重新进入通话界面
                OpenCallPanel();
            }
            // call == 1 && callBeginTime == 0：正在连接中，PorVideoCallNodePanel 已打开，不重复操作
        }

        /// <summary>
        /// 打开 PorVideoCallNode 通话面板，传入当前角色与设备数据。
        /// </summary>
        private void OpenCallPanel()
        {
            if (_characterInfo == null)
            {
                LoggerUtils.LogError("[IncubationCabinControll] 无法打开通话面板：当前设备未绑定角色");
                return;
            }

            var character = new CabinPublishData(_characterInfo);
            var box = _boxDate;

            UIManager.Inst.OpenPanel(PanelId.PorVideoCallNodePanel, character, box);
        }

        /// <summary>
        /// 导入角色按钮点击：设备在线时打开角色列表面板，选中后执行导入
        /// </summary>
        private void OnImportClick()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("BOX离线了，请先为BOX连网吧");
                return;
            }

            System.Action<CabinCharacterBaseInfo, System.Action<bool>> onImport = (data, onDone) =>
            {
                CabinBoxManager.Inst.ImportBoxCharacterData((CabinCharacterUgcInfo)data);
                onDone?.Invoke(true);
            };
            UIManager.Inst.OpenPanel<IncubationCabinRolesMainPanel>(PanelId.IncubationCabinRolesMainPanel, onImport);
        }

        /// <summary>
        /// 新增 Box 绑定按钮点击：打开绑定流程面板
        /// </summary>
        private void OnNewLinkBoxClick()
        {
            // TODO: 新增BOX Box 绑定逻辑
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinLinkBox);
        }

        /// <summary>
        /// 解绑 Box 按钮点击：先弹出二次确认弹窗，用户确认后再走服务器接口执行解绑，成功后关闭面板
        /// </summary>
        private void OnUnLinkBoxClick()
        {
            CommonBoxConfirmWithTitlePanel confirmPanel =
                UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            confirmPanel.SetTextAndAction(
                "解除绑定",
                "解除后，BUD BOX将与当前账号断开关联。",
                "确认",
                "取消",
                confirmClick: () =>
                {
                    // 仅在设备在线时才发送 MQTT 休眠指令，离线时跳过（解绑为服务器接口，不依赖设备连接）
                    if (CabinBoxManager.Inst.GetBoxState() == BoxState.Online)
                    {
                        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.start_rest);
                    }
                    string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
                    CabinBoxManager.Inst.UnbindBudBox(deviceId, isSuccess =>
                    {
                        if (isSuccess)
                        {
                            CloseSelf();
                        }
                        else
                        {
                            TipPanel.ShowToast("解绑失败，请稍后重试。");
                        }
                    });
                },
                cancelClick: null
            );
        }

        /// <summary>
        /// 重命名按钮点击：打开重命名弹窗，确认后调用接口更新设备名称
        /// </summary>
        private void OnReNameClick()
        {
            if (_boxDate == null)
            {
                return;
            }
            var pop = UIManager.Inst.OpenPanel<IncubationReNamePop>(PanelId.IncubationReNamePop);
            pop.SetData(
                onConfirm: (newName) =>
                {
                    if (_boxDate == null)
                    {
                        return;
                    }
                    CabinBoxManager.Inst.UpdateBudBox(_boxDate.deviceId, newName);
                },
                currentName: _boxDate?.deviceName ?? string.Empty,
                placeholder: "输入你的BUD BOX名字",
                title: "输入名字"

            );
        }

        /// <summary>
        /// 关闭面板：若有未同步的数据变更，弹出确认框提示用户；否则直接关闭
        /// </summary>
        public override void CloseSelf()
        {
            //bool isChange = CabinBoxManager.Inst.GetBoxDataChange();
            //if (isChange)
            //{
            //    CommonBoxConfirmWithTitlePanel confirmPanel =
            //        UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            //    confirmPanel.SetTextAndAction(
            //        "内容还未同步到BUD BOX",
            //        "现在退出不会保存修改的内容。",
            //        "确认",
            //        "取消",
            //        confirmClick: () => base.CloseSelf(),
            //        cancelClick: null
            //    );
            //    return;
            //}
            base.CloseSelf();
        }

        /// <summary>
        /// 销毁时清理 Box 数据缓存并注销所有消息监听
        /// </summary>
        protected override void OnDestroy()
        {
            CabinBoxManager.Inst.ClearBudBoxData();
            MessageHelper.RemoveListener(MessageName.OnImportBoxCharacterFinish, RefreshCharacterData);
            MessageHelper.RemoveListener<bool>(MessageName.OnDetectionChange, OnDetectionChange);
            MessageHelper.RemoveListener(MessageName.OnDelectBoxCharacterFinish, OnDelectBoxCharacterFinish);
            MessageHelper.RemoveListener(MessageName.OnActiveBudBoxChanged, RefreshCharacterData);

            base.OnDestroy();
        }

        #endregion

        [Button("绑定测试")]
        void BindTest()
        {
            //  CabinBoxManager.Inst.BindCabinBox("BUD-E02EDBF8"); //009
            CabinBoxManager.Inst.BindCabinBox("BUD-13EDD13E"); //012
        }
        [Button("解绑测试")]
        void UnBindTest()
        {
            CabinBoxManager.Inst.UnbindBudBox("devicceID222");
        }

        [Button("亮屏")]
        void openBacklight()
        {
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.openBacklight);
        }
        [Button("熄屏")]
        void closeBacklight()
        {
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.closeBacklight);
        }

        [Button("tttt")]
        void tt()
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinWifiSetting, "CabinControll");

            // UIManager.Inst.OpenPanel(PanelId.IncubationCabinWifiSetting);

        }
    }
}
