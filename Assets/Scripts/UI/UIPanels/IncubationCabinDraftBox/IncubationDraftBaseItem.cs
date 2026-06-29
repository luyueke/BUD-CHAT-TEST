using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    /// <summary>
    /// Author:
    /// Desc: 草稿箱列表Item，展示单个角色草稿的封面、名称及各类数量统计。
    ///       订阅 MessageName.OnCabinCharacterUpdated 事件，在数据局部变更时按类型刷新对应 UI，
    ///       无需重建整个列表。
    /// Date:26-04-01
    /// </summary>
    public class IncubationDraftBaseItem : MonoBehaviour
    {
        [SerializeField] private Button SelectBtn;                  // 点击整个卡片触发选中
        [SerializeField] private GameObject SelectedGo;             // 选中态高亮边框/遮罩
        [SerializeField] protected Image CardImg;                   // 卡面底图（根据 colorId 动态加载）
        [SerializeField] private RemoteImageBehaviour CoverImage;   // 封面立绘图（远程 URL 加载）
        [SerializeField] protected Text NameTxt;                      // 角色名（卡片左上小字）
        [SerializeField] private Text CoverDescTxt;                 // 封面台词（底部小字）
        [SerializeField] private Text SkinPackCountTxt;             // 皮肤包数量
        [SerializeField] private Text PendingEmoteCountTxt;         // 待机动作数量
        [SerializeField] private Text VoiceCommandsCountTxt;        // 口令互动数量
        [SerializeField] private GameObject DraftBadgeGo;           // 草稿 Badge（蓝灰）
        [SerializeField] private GameObject PublishedBadgeGo;       // 已发布 Badge（绿）
        [SerializeField] private GameObject UnpublishedBadgeGo;     // 已下架 Badge（灰）

        public string CharacterId => _data?.id;

        private Action<CabinCharacterBaseInfo> onSelected;    // 卡片点击回调
        public CabinCharacterBaseInfo _data { get; private set; }                 // 当前绑定的角色数据
        private UGCClass _status = UGCClass.Draft; // 当前状态标签
        private bool _showBadge = true;             // 是否显示状态徽章，默认 true 兼容现有调用方

        private void Awake()
        {
            SelectBtn.onClick.AddListener(() => onSelected?.Invoke(_data));
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string, CabinCharacterUpdateType>(
                MessageName.OnCabinCharacterUpdated, OnCharacterUpdated);
        }

        // ──────────────── 公共接口 ────────────────

        /// <summary>初始化 Item 数据，绑定点击回调，刷新所有 UI 字段。默认草稿态，兼容旧调用方</summary>
        public void SetData(CabinCharacterBaseInfo data, Action<CabinCharacterBaseInfo> onclick = null)
            => SetData(data, (UGCClass)data.ugcclass, onclick);

        /// <summary>
        /// 同上，额外指定状态标签（草稿/已发布/已下架）。
        /// 状态影响：Badge 显示、名称取 name 还是 pubName。
        /// </summary>
        public void SetData(CabinCharacterBaseInfo data, UGCClass status, Action<CabinCharacterBaseInfo> onclick = null)
        {
            MessageHelper.RemoveListener<string, CabinCharacterUpdateType>(
                MessageName.OnCabinCharacterUpdated, OnCharacterUpdated);

            _data = data;
            _status = status;
            onSelected = onclick;

            MessageHelper.AddListener<string, CabinCharacterUpdateType>(
                MessageName.OnCabinCharacterUpdated, OnCharacterUpdated);

            RefreshAll();
        }

        /// <summary>切换选中高亮态</summary>
        public void SetSelected(bool isSelected)
        {
            SelectedGo.SetActive(isSelected);
        }

        /// <summary>控制草稿/已发布/已下架三个 Badge 的整体显隐。受 _showBadge 标志位约束</summary>
        public void SetBadgesVisible(bool visible)
        {
            DraftBadgeGo?.SetActive(false);
            PublishedBadgeGo?.SetActive(false);
            UnpublishedBadgeGo?.SetActive(false);
            if (_showBadge == false || visible == false)
            {
                return;
            }
            if (_status == UGCClass.Draft)
            {
                DraftBadgeGo?.SetActive(true);
            }
            else if (_status == UGCClass.Published)
            {
                bool IsUnpublished = _data?.IsUnpublished() > emUnpublished.None;
                PublishedBadgeGo?.SetActive(!IsUnpublished);
                UnpublishedBadgeGo?.SetActive(IsUnpublished);
            }
        }

        /// <summary>
        /// 设置是否显示状态徽章（草稿/已发布/已下架）。
        /// 设为 false 后，RefreshAll 和 SetBadgesVisible 均不会激活任何 Badge，
        /// 包括后续由 OnCabinCharacterUpdated 触发的刷新。
        /// </summary>
        public void SetShowBadge(bool show)
        {
            _showBadge = show;
            SetBadgesVisible(_showBadge);
        }

        /// <summary>回池时调用：移除事件监听，重置数据引用</summary>
        public void ClearData()
        {
            MessageHelper.RemoveListener<string, CabinCharacterUpdateType>(
                MessageName.OnCabinCharacterUpdated, OnCharacterUpdated);
            _data = null;
            SetSelected(false);
        }

        /// <summary>直接设置封面纹理（Texture2D 或 RenderTexture），不走远程加载，用于编辑器实时预览</summary>
        public void SetLocalCoverTexture(Texture tex)
        {
            if (tex == null) return;
            // 清空 RemoteImageBehaviour 记录的当前 URL，使任何正在飞行的旧 URL 请求
            // 在完成时触发 "request.path != _CurrentRequestedURL" 检查而自动放弃写入纹理，
            // 避免异步加载完成后覆盖此处刚设置的本地 RT，导致封面出现"新→旧"闪烁。
            CoverImage._CurrentRequestedURL = string.Empty;
            CoverImage.RawImage.texture = tex;
        }

        // ──────────────── 事件处理 ────────────────

        /// <summary>收到角色局部更新事件：仅当 ID 匹配时按类型刷新对应 UI 部分</summary>
        private void OnCharacterUpdated(string characterId, CabinCharacterUpdateType updateType)
        {
            if (_data == null || _data.id != characterId) return;

            switch (updateType)
            {
                case CabinCharacterUpdateType.All: RefreshAll(); break;
                case CabinCharacterUpdateType.Cover: RefreshCover(); break;
                case CabinCharacterUpdateType.CardColor: RefreshCardColor(); break;
                case CabinCharacterUpdateType.CardDesc: RefreshCardDesc(); break;
                case CabinCharacterUpdateType.CharacterDetail: RefreshCharacterDetail(); break;
            }
        }

        // ──────────────── 局部刷新 ────────────────
        protected virtual void UpdateName()
        {
            if (_data == null)
            {
                return;
            }
            NameTxt.text = _data.name;
        }
        private void RefreshAll()
        {
            var info = _data;
            RefreshCover();
            RefreshCardColor();
            UpdateName();
            CoverDescTxt.text = info.coverInfo?.desc;
            SetBadgesVisible(_showBadge);

            int skinPackCount = info.skinPack != null ? info.skinPack.Count : 0;
            SkinPackCountTxt.text = skinPackCount.ToString();

            int pendingEmoteCount = 0;
            if (info.pendingEmote != null)
            {
                if (info.pendingEmote.emoteList != null)
                    pendingEmoteCount += info.pendingEmote.emoteList.Count;
                if (info.pendingEmote.loopEmoteList != null)
                    pendingEmoteCount += info.pendingEmote.loopEmoteList.Count;
            }
            PendingEmoteCountTxt.text = pendingEmoteCount.ToString();

            int voiceCommandsCount = info.voiceCommands != null ? info.voiceCommands.Count : 0;
            VoiceCommandsCountTxt.text = voiceCommandsCount.ToString();
        }

        /// <summary>刷新封面立绘图（读取 info.cover）</summary>
        private void RefreshCover()
        {
            var cover = _data?.cover;
            if (!string.IsNullOrEmpty(cover))
                CoverImage.Load(cover);
        }

        /// <summary>刷新卡片底图：用 colorId 查配置表取 Color 字段，拼路径加载对应 Sprite</summary>
        private void RefreshCardColor()
        {
            var colorId = _data?.coverInfo?.GetDetail()?.colorId;
            if (colorId == null || colorId == 0) return;
            var config = DataTables.GetDraftBoxCardColorConfig(colorId.Value);
            SetCardColor(config);
        }

        protected virtual void SetCardColor(DraftBoxCardColorConfig config)
        {
            if (config == null)
                return;
            var path = $"Assets/Loadable/UI/UIPanel/IncubationCabinDraftBox/CardSprite/{config.Color}.png";
            var sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(path, gameObject);
            if (sprite != null)
                CardImg.sprite = sprite;
        }

        /// <summary>刷新封面台词文本（读取 cabinCoverInfo.desc）</summary>
        private void RefreshCardDesc()
        {
            CoverDescTxt.text = _data?.coverInfo?.desc;
        }

        /// <summary>刷新角色细节（位置/缩放，如有 UI 表现可在此扩展）</summary>
        private void RefreshCharacterDetail()
        {
            // 当前 Item 不展示位置/缩放信息，预留扩展点
        }
    }
}

