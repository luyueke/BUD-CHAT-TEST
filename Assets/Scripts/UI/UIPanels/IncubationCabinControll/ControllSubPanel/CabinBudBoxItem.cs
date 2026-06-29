using Game.BudBox;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// BudBox 列表中的单条 Item，展示设备名称与在线状态，点击后选中该 Box
    /// </summary>
    public class CabinBudBoxItem : MonoBehaviour
    {
        [SerializeField] private Text DeviceNameTxt; // 设备名称文本
        [SerializeField] private Text StateTxt;      // 设备在线状态文本
        [SerializeField] private Button SelectBtn;   // 选中按钮

        [Header("角色 & Box 预览")]
        [SerializeField] private BudBoxModel _budBoxModel; // 统一管理角色和 box 3D 模型
        [SerializeField] private Camera PhotoCamera;       // 渲染预览到 RT 的专用相机
        [SerializeField] private RawImage PreviewImage;    // 显示 RT 的 UI RawImage

        /// <summary>点击选中时触发，携带该 Box 的设备数据，由父级绑定连接逻辑</summary>
        public Action<CabinBudBoxData> onSelectClick;

        private CabinBudBoxData _data;
        private RenderTexture _renderTexture; // 动态创建的 RenderTexture，用于角色和 box 预览

        // 绑定选中按钮，并监听设备状态变化和设备名称变更消息
        void Awake()
        {
            SelectBtn.onClick.AddListener(() =>
            {
                if (_data != null && _data.deviceState.emBoxState == BoxState.Online)
                    CabinBoxManager.Inst.SendSyncBaseMsgTo(_data.deviceId);
                onSelectClick?.Invoke(_data);
            });
            MessageHelper.AddListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.AddListener<string>(MessageName.OnBudBoxNameChange, RefreshBoxNameText);
            MessageHelper.AddListener<string>(MessageName.OnBoxCharacterChanged, OnBoxCharacterChanged);
            MessageHelper.AddListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
        }

        private void OnDestroy()
        {
            // 销毁角色模型，防止内存泄漏
            DestroyCharacter();
            MessageHelper.RemoveListener<string>(MessageName.OnBoxDeviceStateChanged, OnBoxDeviceStateChanged);
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxNameChange, RefreshBoxNameText);
            MessageHelper.RemoveListener<string>(MessageName.OnBoxCharacterChanged, OnBoxCharacterChanged);
            MessageHelper.RemoveListener<bool>(MessageName.OnBoxSceneSyncResult, OnBoxSceneSyncResult);
        }
        /// <summary>
        /// 绑定设备数据并刷新显示：设备名称、在线状态，以及在 CharacterRoot 下生成角色模型
        /// </summary>
        public void Init(CabinBudBoxData data)
        {
            _data = data;
            DeviceNameTxt.text = data.deviceId;
            RefreshStateTxt();
            RefreshBoxNameText();

            // 有角色数据时生成模型，否则销毁已有模型
            if (data.characterInfo != null)
            {
                CreateCharacter(data.characterInfo);
            }
            else
            {
                DestroyCharacter();
            }
        }

        /// <summary>
        /// 通过 BudBoxModel 异步创建角色模型并加载盒子场景，
        /// 与 CabinControllBoxPanel.CreateCharacter() 保持一致。
        /// </summary>
        /// <param name="data">当前绑定的角色 UGC 信息</param>
        private void CreateCharacter(CabinCharacterUgcInfo data)
        {
            _budBoxModel.Clear();

            // 通过 CabinBoxManager 统一查找与 DeviceState.skinPackId 匹配的皮肤包
            CabinBoxManager.Inst.GetSkinPackInfo((defaultSkin) =>
            {
                // 回调内再次清理，防止多次快速调用时出现残留实例
                _budBoxModel.Clear();

                if (defaultSkin == null)
                {
                    LoggerUtils.Log($"[CabinBudBoxItem] 未找到匹配的皮肤包，设备：{_data?.deviceId}");
                    return;
                }

                _budBoxModel.SetupCharacterFromSkinPack(defaultSkin);
                _budBoxModel.LoadBoxScene(_data?.boxInfo?.metaDataUrl);

                // 首次创建时动态生成 RenderTexture（512×512，ARGB32，depth 16）
                // 若已存在则复用，避免重复分配 GPU 资源
                if (_renderTexture == null)
                {
                    _renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                    _renderTexture.wrapMode = TextureWrapMode.Clamp;
                    _renderTexture.filterMode = FilterMode.Bilinear;
                }

                if (PhotoCamera != null)
                {
                    PhotoCamera.clearFlags = CameraClearFlags.SolidColor;
                    PhotoCamera.backgroundColor = Color.clear;
                    PhotoCamera.targetTexture = _renderTexture;
                }

                if (PreviewImage != null)
                {
                    PreviewImage.texture = _renderTexture;
                }
            }, _data.deviceId); // 传入当前 Item 对应的设备 ID，精确查找该设备的皮肤包
        }

        /// <summary>
        /// 销毁角色和 box 模型，并释放动态创建的 RenderTexture
        /// </summary>
        private void DestroyCharacter()
        {
            _budBoxModel.Clear();

            // 释放 RT：先 Release GPU 资源，再 Destroy 托管对象，最后清除相机和 RawImage 引用
            if (_renderTexture != null)
            {
                if (PhotoCamera != null)
                {
                    PhotoCamera.targetTexture = null;
                }

                if (PreviewImage != null)
                {
                    PreviewImage.texture = null;
                }

                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }

        private void OnBoxDeviceStateChanged(string deviceId)
        {
            if (_data == null || _data.deviceId != deviceId) return;
            RefreshStateTxt();
        }

        private void RefreshStateTxt()
        {
            if (StateTxt == null || _data == null) return;
            StateTxt.text = _data.deviceState.emBoxState switch
            {
                BoxState.Online => "在线",
                BoxState.Offline => "离线",
                BoxState.Connecting => "离线",
                _ => string.Empty
            };
        }
        private void RefreshBoxNameText(string deviceId)
        {
            if (!deviceId.Equals(_data.deviceId))
            {
                return;
            }
            RefreshBoxNameText();
        }
        private void RefreshBoxNameText()
        {
            var name = _data.deviceName ?? "";
            // 超过 5 个字符时截断为前 5 字 + "..."，避免设备名过长导致 UI 溢出
            DeviceNameTxt.text = name.Length > 20 ? name.Substring(0, 20) + "..." : name;
        }

        /// <summary>
        /// Box 角色变更回调：当前 Item 对应设备的角色（或皮肤）发生变化时，重建角色预览模型。
        /// </summary>
        /// <param name="deviceId">触发变更的设备 ID</param>
        private void OnBoxCharacterChanged(string deviceId)
        {
            if (_data == null || _data.deviceId != deviceId)
                return;

            if (_data.characterInfo != null)
            {
                CreateCharacter(_data.characterInfo);
            }
            else
            {
                DestroyCharacter();
            }
        }

        /// <summary>
        /// Box 场景同步结果回调：同步成功后立刻刷新本 Item 的盒子 3D 模型。
        /// 仅对当前选中设备（与 CabinBoxManager 正在操控的设备一致）的 Item 生效。
        /// </summary>
        /// <param name="isSuccess">true=同步成功</param>
        private void OnBoxSceneSyncResult(bool isSuccess)
        {
            if (!isSuccess)
                return;

            if (!_budBoxModel.HasCharacter)
                return;

            // 仅刷新当前选中设备对应的 Item，其他设备的 Item 不受影响
            var currentDeviceId = CabinBoxManager.Inst.GetCurrentDeviceId();

            if (_data == null || _data.deviceId != currentDeviceId)
                return;

            var boxInfo = CabinBoxManager.Inst.GetCabinBudBoxData(_data.deviceId)?.boxInfo;
            _budBoxModel.LoadBoxScene(boxInfo?.metaDataUrl);
        }
    }
}
