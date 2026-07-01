using Game.Avatar;
using Game.BudBox;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 养成舱皮肤预览面板，通过 UIManager.OpenPanel 打开。
    /// 流程：点击"同步角色" → 发送 MQTT → 隐藏按钮/显示"同步中..."；
    ///       收到回包 → 显示"展示中"；
    ///       玩家点击关闭按钮 → CloseSelf()。
    /// </summary>
    public class CabinChangeSkinPanel : BasePanel<CabinChangeSkinPanel>
    {
        /// <summary>角色模型挂载节点</summary>
        [SerializeField] private Transform CharacterRoot;

        /// <summary>渲染到 RT 的专用相机</summary>
        [SerializeField] private Camera PhotoCamera;

        /// <summary>同步角色按钮（右下角）</summary>
        [SerializeField] private Button SyncBtn;

        /// <summary>关闭按钮，玩家手动点击后关闭预览面板</summary>
        [SerializeField] private Button CloseBtn;

        /// <summary>"同步中..." 文字对象，初始隐藏，点击同步按钮后显示</summary>
        [SerializeField] private GameObject SyncingTextGo;

        /// <summary>"展示中" 文字对象，收到 MQTT 回包后显示，表示皮肤已同步到设备</summary>
        [SerializeField] private GameObject ShowingTextGo;

        private CharacterWrap _characterWrap;

        /// <summary>是否处于等待 MQTT 回包状态，用于区分本地选皮肤与 MQTT 回包触发的 OnDetectionChange</summary>
        private bool _isSyncing;

        private SkinPackInfo skinPack;
        #region 生命周期

        /// <summary>
        /// 绑定按钮事件并订阅 MQTT 回包消息，面板创建时调用一次。
        /// </summary>
        public override void OnCreate()
        {
            SyncBtn.onClick.AddListener(OnSyncClick);
            CloseBtn.onClick.AddListener(CloseSelf);
            MessageHelper.AddListener<bool>(MessageName.OnDetectionChange, OnDetectionChange);
        }

        /// <summary>
        /// 面板打开时：从参数取得皮肤数据，重置状态并生成 3D 角色预览。
        /// </summary>
        /// <param name="args">args[0]：CabinCharacterBaseInfo，点击的皮肤所属角色信息</param>
        public override void OnShow(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                CloseSelf();
                return;
            }

            var skin = args[0] as CabinCharacterBaseInfo;

            if (skin == null)
            {
                CloseSelf();
                return;
            }

            _isSyncing = false;
            SyncBtn.gameObject.SetActive(true);
            SyncingTextGo.SetActive(false);
            ShowingTextGo.SetActive(false);

            CreateCharacter(skin);
        }

        /// <summary>
        /// 面板隐藏时清理角色模型并重置同步状态。
        /// </summary>
        public override void OnHidden()
        {
            _isSyncing = false;
            DestroyCharacter();
        }

        /// <summary>
        /// 销毁时注销消息监听并释放角色模型。
        /// </summary>
        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener<bool>(MessageName.OnDetectionChange, OnDetectionChange);
            DestroyCharacter();
            base.OnDestroy();
        }

        #endregion

        #region 角色模型

        /// <summary>
        /// 根据皮肤数据创建 UI 角色：解析 avatarJson 后通过 AvatarController 生成并挂载到 CharacterRoot。
        /// </summary>
        /// <param name="data">角色信息，使用 skinPack[0].avatarJson 作为外观数据</param>
        private void CreateCharacter(CabinCharacterBaseInfo data)
        {
            DestroyCharacter();

            // 以用户自身角色数据为兜底，优先取皮肤包中的 avatarJson
            var characterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            skinPack = data.skinPack[0];
            if (skinPack != null && !string.IsNullOrEmpty(skinPack.avatarJson))
            {
                characterData = CharacterData.DeserializeObject(skinPack.avatarJson);
            }

            _characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
            _characterWrap.SetParent(CharacterRoot, true);
        }

        /// <summary>销毁当前角色模型并释放引用。</summary>
        private void DestroyCharacter()
        {
            if (_characterWrap != null)
            {
                Destroy(_characterWrap.CustomAvatar);
                _characterWrap = null;
            }
        }

        #endregion

        #region 同步逻辑

        /// <summary>
        /// 点击"同步角色"按钮：若设备不在线则飘字提示并返回；
        /// 否则隐藏按钮，显示"同步中..."文字，向硬件下发 set_character MQTT 指令。
        /// </summary>
        private void OnSyncClick()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("设备离线无法同步");
                return;
            }

            _isSyncing = true;
            SyncBtn.gameObject.SetActive(false);
            SyncingTextGo.SetActive(true);
            CabinBoxManager.Inst.SetCharacterUseSkin(skinPack);
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_character);

            // 切换皮肤交互埋点：用户点击「同步角色」确认应用时上报
            IncubationCabinControll.ReportThinkingData("switch_skin");
        }

        /// <summary>
        /// 监听数据变更广播。仅在 _isSyncing 为 true（即等待 MQTT 回包）时响应：
        /// 隐藏"同步中..."，显示"展示中"，面板保持打开。
        /// </summary>
        /// <param name="isChange">是否仍有未同步的变更（来自 CabinBoxManager）</param>
        private void OnDetectionChange(bool isChange)
        {
            if (!_isSyncing)
                return;

            _isSyncing = false;

            // 切换到"展示中"状态，表示皮肤已成功同步到设备
            SyncingTextGo.SetActive(false);
            ShowingTextGo.SetActive(true);

            // 皮肤同步确认后，广播角色变更，通知列表面板刷新对应 Item 模型
            string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();

            if (string.IsNullOrEmpty(deviceId))
                return;

            MessageHelper.Broadcast<string>(MessageName.OnBoxCharacterChanged, deviceId);
        }

        #endregion
    }
}
