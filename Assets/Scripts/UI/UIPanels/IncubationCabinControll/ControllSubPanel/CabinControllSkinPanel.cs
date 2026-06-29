using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.BudBox;
using Game.Database;
using Message;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台皮肤子界面，展示皮肤列表并提供切换到互动界面的入口。
    ///       每次打开时检查伙伴库中的互动配置与当前 BOX 是否一致，不一致时显示「同步到BOX」按钮。
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllSkinPanel : MonoBehaviour
    {
        [SerializeField] private Button SceneBtn;                        // 切换到场景界面的按钮
        [SerializeField] private Button ChangePlayerBtn;                 // 更换角色按钮（有角色态显示）

        [Header("更换角色按钮状态")]
        [SerializeField] private Sprite ChangePlayerNormalSprite; // 在线时的正常图片
        [SerializeField] private Sprite ChangePlayerGraySprite;   // 离线时的置灰图片
        [SerializeField] private Text ChangePlayerTex;            // 更换角色按钮文本
        [SerializeField] private Text PlayerName;               // 角色名称
        [SerializeField] private Color NormalTextColor;           // 正常文本颜色
        [SerializeField] private Color GrayTextColor;             // 置灰文本颜色
        [SerializeField] private RemoteImageBehaviour CharacterPortraitImage; // 角色头像图片加载组件
        [SerializeField] private Button CabinHeadBg;                         // 头像背景按钮，点击跳转到伙伴详情页
        [SerializeField] private GameObject SkinItemPrefab; // 皮肤列表 Item 预制体
        [SerializeField] private Transform SkinContent;     // 皮肤列表容器

        [Header("角色态")]
        [SerializeField] private GameObject NoPlayer;       // 无角色时显示的占位视图
        [SerializeField] private GameObject HasPlayer;      // 有角色时显示的内容视图

        [Header("同步到BOX")]
        /// <summary>「同步到BOX」按钮，伙伴库配置与 BOX 不一致时显示</summary>
        [SerializeField] private Button SyncToBoxBtn;
        /// <summary>同步状态文本，未同步时显示「同步角色」，点击后显示「同步中...」</summary>
        [SerializeField] private Text SyncTipsText;

        public Action onSceneClick;
        public Action onChangePlayerClick;
        public Action<CabinCharacterBaseInfo> onSkinSelect;

        private CabinCharacterUgcInfo _data;
        private readonly List<CabinSkinCardItem> _skinItems = new List<CabinSkinCardItem>();
        // packId → item 的映射，用于 OnSkinSelect 时精确更新选中/取消状态
        private readonly Dictionary<string, CabinSkinCardItem> _skinItemMap = new Dictionary<string, CabinSkinCardItem>();

        /// <summary>是否正在等待 MQTT 回包，用于防止重复点击并驱动文本显示</summary>
        private bool _isSyncing;

        #region 初始化

        /// <summary>
        /// 绑定互动按钮点击事件，在父级 InitUI 中调用一次
        /// </summary>
        public void InitUI()
        {
            SceneBtn.onClick.AddListener(() => onSceneClick?.Invoke());
            ChangePlayerBtn.onClick.AddListener(OnChangePlayerBtnClick);

            SyncToBoxBtn?.onClick.AddListener(OnSyncToBoxClick);

            CabinHeadBg.onClick.AddListener(OnHeadBgClick);

            // 订阅数据变更广播，面板存活期间实时更新按钮状态
            MessageHelper.AddListener<bool>(MessageName.OnDetectionChange, OnDetectionChange);
            // 订阅设备连接状态变更，实时刷新更换角色按钮的图片
            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        }

        /// <summary>
        /// 销毁时注销消息监听，避免内存泄漏
        /// </summary>
        private void OnDestroy()
        {
            MessageHelper.RemoveListener<bool>(MessageName.OnDetectionChange, OnDetectionChange);
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
        }

        #endregion

        #region 显示 / 隐藏

        /// <summary>
        /// 缓存角色数据，切换有无角色视图，并按需刷新皮肤列表和同步按钮状态
        /// </summary>
        /// <param name="data">角色数据，为 null 表示当前 Box 无绑定角色</param>
        public void OnShow(CabinCharacterUgcInfo data)
        {
            _data = data;
            _isSyncing = false;
            RefreshPlayerState();

            // 每次显示时同步更换角色按钮的图片（在线正常，离线置灰）
            RefreshChangePlayerBtnState();

            // 每次显示时检查数据一致性，决定是否展示「同步到BOX」按钮
            RefreshSyncBtnState();

            if (_data != null)
            {
                PlayerName.text = _data.name;
                CharacterPortraitImage?.Load(_data.characterPortraitUrl);
                RefreshSkinData();
            }
            else
            {
                ClearSkinItems();
            }
        }

        /// <summary>
        /// 根据是否有角色数据切换 NoPlayer / HasPlayer 视图
        /// </summary>
        private void RefreshPlayerState()
        {
            bool hasPlayer = _data != null;
            NoPlayer.SetActive(!hasPlayer);
            HasPlayer.SetActive(hasPlayer);
        }

        /// <summary>
        /// 隐藏时清理数据引用并销毁所有皮肤 Item，同时隐藏同步按钮
        /// </summary>
        public void OnHide()
        {
            _data = null;
            _isSyncing = false;
            CharacterPortraitImage?.ResetRawImage();
            ClearSkinItems();

            SyncToBoxBtn?.gameObject.SetActive(false);
            SyncTipsText?.gameObject.SetActive(false);
        }

        #endregion

        #region 更换角色按钮

        /// <summary>
        /// 点击头像背景按钮：跳转到当前伙伴的角色详情页（IncubationCabinRolesPanel）。
        /// </summary>
        private void OnHeadBgClick()
        {
            if (_data == null)
                return;

            CabinRolesNetManager.Inst.OpenPanelWithFreshData(_data.id);
        }

        /// <summary>
        /// 更换角色按钮点击回调。
        /// 设备离线或连接中时弹出 Toast 提示，在线时正常触发换角色流程。
        /// </summary>
        private void OnChangePlayerBtnClick()
        {
            if (CabinBoxManager.Inst.GetBoxState() != BoxState.Online)
            {
                TipPanel.ShowToast("BOX离线了，请先为BOX连网吧");
                return;
            }

            onChangePlayerClick?.Invoke();
        }

        /// <summary>
        /// 根据设备在线状态刷新更换角色按钮的图片和文本颜色：在线显示正常样式，离线/连接中显示置灰样式。
        /// </summary>
        private void RefreshChangePlayerBtnState()
        {
            if (ChangePlayerBtn == null)
            {
                LoggerUtils.LogError("[CabinControllSkinPanel] ChangePlayerBtn 未绑定，请在预制体 Inspector 中赋值");
                return;
            }

            if (ChangePlayerBtn.image == null)
            {
                LoggerUtils.LogError("[CabinControllSkinPanel] ChangePlayerBtn 的 Target Graphic 未设置为 Image 组件，请检查预制体");
                return;
            }

            if (ChangePlayerTex == null)
            {
                LoggerUtils.LogError("[CabinControllSkinPanel] ChangePlayerTex 未绑定，请在预制体 Inspector 中赋值");
                return;
            }

            bool isOnline = CabinBoxManager.Inst.GetBoxState() == BoxState.Online;
            ChangePlayerBtn.image.sprite = isOnline ? ChangePlayerNormalSprite : ChangePlayerGraySprite;
            ChangePlayerTex.color = isOnline ? NormalTextColor : GrayTextColor;
        }

        /// <summary>
        /// 监听设备连接状态变更广播，实时刷新更换角色按钮图片。
        /// </summary>
        /// <param name="deviceId">发生状态变化的设备 ID</param>
        private void OnBoxDeviceStateChanged(string deviceId)
        {
            if (deviceId != CabinBoxManager.Inst.GetCurrentDeviceId())
                return;

            RefreshChangePlayerBtnState();
            // 设备离线时同步隐藏「同步到BOX」按钮
            RefreshSyncBtnState();
        }

        #endregion

        #region 同步到BOX

        /// <summary>
        /// 刷新「同步到BOX」按钮及提示文本的可见性和内容。
        /// 需满足：有角色 + BOX 在线 + 硬件已上报 MD5 + 数据不一致，才显示按钮。
        /// </summary>
        private void RefreshSyncBtnState()
        {
            // 同步中时不重置显示，由 OnDetectionChange 回包后统一处理
            if (_isSyncing)
                return;

            if (SyncToBoxBtn == null)
            {
                LoggerUtils.LogError("[CabinControllSkinPanel] SyncToBoxBtn 未绑定，请在预制体 Inspector 中赋值");
                return;
            }

            if (SyncTipsText == null)
            {
                LoggerUtils.LogError("[CabinControllSkinPanel] SyncTipsText 未绑定，请在预制体 Inspector 中赋值");
                return;
            }

            bool isBoxOnline = CabinBoxManager.Inst.GetBoxState() == BoxState.Online;
            bool hasChange = _data != null && isBoxOnline && CabinBoxManager.Inst.GetBoxDataChange();

            SyncToBoxBtn.gameObject.SetActive(hasChange);
            SyncTipsText.gameObject.SetActive(hasChange);

            if (hasChange)
            {
                // 数据不一致时提示用户同步
                SyncTipsText.text = "同步角色";
            }
        }

        /// <summary>
        /// 点击「同步到BOX」按钮：向硬件下发 set_character MQTT 指令，并将文本切换为「同步中...」。
        /// </summary>
        private void OnSyncToBoxClick()
        {
            _isSyncing = true;
            SyncToBoxBtn.interactable = false;
            SyncTipsText.text = "同步中...";
            CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_character);
        }

        /// <summary>
        /// 监听数据变更广播（来自 sync_baseMsg 或 set_character 回包），实时刷新按钮可见性及皮肤选中状态。
        /// </summary>
        /// <param name="isChange">是否存在未同步的变更</param>
        private void OnDetectionChange(bool isChange)
        {
            _isSyncing = false;

            if (SyncToBoxBtn != null)
            {
                SyncToBoxBtn.interactable = true;
            }

            RefreshSyncBtnState();
            // set_character 回包后，skinPackId 已由 CabinBoxManager 更新，刷新皮肤列表选中态
            RefreshSkinSelection();
        }

        #endregion

        #region 皮肤列表

        /// <summary>
        /// 根据当前角色数据重建皮肤列表
        /// </summary>
        private void RefreshSkinData()
        {
            var skinPack = _data?.skinPack;

            List<CabinCharacterBaseInfo> dataList = new List<CabinCharacterBaseInfo>();

            if (_data != null)
            {
                dataList.Add(_data);
            }

            if (_data.extensionPackList?.Count > 0)
            {
                CabinNetManager.Inst.GetExtensionPackBatchInfo(_data.extensionPackList, (isS, _List) =>
                {
                    if (isS)
                    {
                        foreach (var packInfo in _List)
                        {
                            // 自己创建的但是不一定发布了 所以不能这样判断
                            // 判断是否为当前用户创建（创作者本人无需购买，直接拥有）
                            //bool isCreatedByMe = !string.IsNullOrEmpty(packInfo.creator)
                            //    && packInfo.creator == AccountDataManager.Inst.Uid;

                            // 判断背包中是否已拥有该扩展包
                            var inv = !string.IsNullOrEmpty(packInfo.id) ? BagDatabase.Inst.Select(packInfo.id) : null;
                            bool isOwnedInBag = inv != null && inv.OwnedNum > 0;

                            if (isOwnedInBag)
                            {
                                dataList.Add(packInfo);
                            }
                        }
                    }
                    RefreshSkinList(dataList);
                });
            }
            else
            {
                RefreshSkinList(dataList);
            }
        }

        private void RefreshSkinList(List<CabinCharacterBaseInfo> dataList)
        {
            // 按创建时间从新到旧排序
            dataList.Sort((a, b) => b.createTime.CompareTo(a.createTime));

            ClearSkinItems();
            string curSkinPackId = CabinBoxManager.Inst.GetSkinPackId();
            foreach (var data in dataList)
            {
                var go = Instantiate(SkinItemPrefab, SkinContent);
                go.SetActive(true);
                var item = go.GetComponent<CabinSkinCardItem>();
                item.SetData(data, OnSkinSelect);
                item.SetShowBadge(false); // CabinControllSkinPanel 不展示状态徽章

                // 守卫：skinPack 为空时跳过该条目，避免越界
                if (data.skinPack == null || data.skinPack.Count == 0)
                {
                    LoggerUtils.Log($"[CabinControllSkinPanel] data.skinPack 为空，跳过该皮肤条目");
                    Destroy(go);
                    continue;
                }

                string packId = data.skinPack[0].packId;
                _skinItemMap[packId] = item; // 建立 packId → item 映射，便于后续按 packId 更新选中状态

                bool isUse = packId.Equals(curSkinPackId);
                item.SetSelected(isUse);
                _skinItems.Add(item);
            }
        }

        /// <summary>
        /// 根据当前设备的 skinPackId 刷新皮肤列表的选中态。
        /// 在 MQTT set_character 回包后调用，确保 UI 与 deviceState.skinPackId 保持一致。
        /// </summary>
        private void RefreshSkinSelection()
        {
            if (_skinItemMap == null || _skinItemMap.Count == 0)
                return;

            string curSkinPackId = CabinBoxManager.Inst.GetSkinPackId();

            foreach (var kvp in _skinItemMap)
            {
                kvp.Value.SetSelected(kvp.Key.Equals(curSkinPackId));
            }
        }

        private void OnSkinSelect(CabinCharacterBaseInfo info)
        {
            if (info==null)
            {
                return;
            }
            string newPackId = info.skinPack[0].packId;
            string curSkinPackId = CabinBoxManager.Inst.GetSkinPackId();

            if (curSkinPackId.Equals(newPackId))
                return;

            string curDeviceID = CabinBoxManager.Inst.GetCurrentDeviceId();
            if (!CabinBoxManager.Inst.GetSkinPackId(curDeviceID).Equals(newPackId))
            {
                // 打开皮肤预览面板，传入皮肤数据
                UIManager.Inst.OpenPanel<CabinChangeSkinPanel>(PanelId.CabinChangeSkinPanel, info);
            }
        }


        /// <summary>
        /// 销毁所有皮肤列表 Item 并清空缓存
        /// </summary>
        private void ClearSkinItems()
        {
            foreach (var item in _skinItems)
            {
                item.ClearData();
                Destroy(item.gameObject);
            }
            _skinItems.Clear();
            _skinItemMap.Clear();
        }

        #endregion
    }
}
