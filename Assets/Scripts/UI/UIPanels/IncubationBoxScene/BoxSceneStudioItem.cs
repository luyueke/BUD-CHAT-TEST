using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// Box 场景工作室列表中的单个条目视图组件。
    /// 支持草稿（CharacterBoxInfo）和已发布（CharacterBoxPublishItem）两种数据模式。
    /// 需挂载在对应预制体上，并在 Inspector 中绑定 UI 字段。
    /// </summary>
    public class BoxSceneStudioItem : MonoBehaviour
    {
        /// <summary>条目点击选中按钮</summary>
        [SerializeField] private Button _selectBtn;

        /// <summary>Box 场景名称文本，仅"去创作"入口显示</summary>
        [SerializeField] private Text _nameText;

        /// <summary>封面图远程加载组件，去创作模式下隐藏</summary>
        [SerializeField] private RemoteImageBehaviour _coverImage;

        /// <summary>发布信息根节点，已发布且非审核中时显示</summary>
        [SerializeField] private GameObject _publishRoot;

        /// <summary>购买/消费次数文本（_publishRoot 的子节点）</summary>
        [SerializeField] private Text _buyNumText;

        /// <summary>价格文本（_publishRoot 的子节点）</summary>
        [SerializeField] private Text _priceText;

        /// <summary>货币类型图标（_publishRoot 的子节点）</summary>
        [SerializeField] private Image _priceIcon;

        /// <summary>去创作</summary>
        [SerializeField] private GameObject _goCreatRoot;


        /// <summary>审核状态提示按钮（审核中 / 申诉中），未审核时隐藏</summary>
        [SerializeField] private Button _underReviewBtn;

        /// <summary>审核状态文本，显示"审核中"或"申诉中"</summary>
        [SerializeField] private Text _underReviewText;

        // ──────────────────────────────────────────────
        // 运行时数据
        // ──────────────────────────────────────────────

        /// <summary>草稿模式数据</summary>
        private CharacterBoxInfo _draftData;

        /// <summary>已发布模式数据</summary>
        private CharacterBoxPublishItem _publishedData;

        /// <summary>草稿模式点击回调</summary>
        private Action<CharacterBoxInfo> _onDraftClick;

        /// <summary>已发布模式点击回调</summary>
        private Action<CharacterBoxPublishItem> _onPublishedClick;

        /// <summary>当前是否为草稿模式</summary>
        private bool _isDraft;

        // ──────────────────────────────────────────────
        // 初始化
        // ──────────────────────────────────────────────

        /// <summary>
        /// 以"去创作"模式初始化条目，作为草稿列表的第一个入口 item。
        /// 点击后跳转到 UGC Box 场景编辑器新建场景。
        /// </summary>
        /// <param name="onCreateClick">点击时触发的创建回调</param>
        public void InitAsCreateItem(Action onCreateClick)
        {
            // 确保 OSA 复用时名称文本可见（普通条目会将其隐藏）
            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(false);
            }

            RefreshNameText(string.Empty);

            if (_goCreatRoot != null)
            {
                _goCreatRoot.SetActive(true);
            }

            if (_coverImage != null)
            {
                _coverImage.gameObject.SetActive(false);
            }

            if (_publishRoot != null)
            {
                _publishRoot.SetActive(false);
            }

            if (_underReviewBtn != null)
            {
                _underReviewBtn.gameObject.SetActive(false);
            }

            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(() => onCreateClick?.Invoke());
        }

        /// <summary>
        /// 以草稿模式初始化条目，绑定数据和点击回调。
        /// </summary>
        /// <param name="info">草稿数据</param>
        /// <param name="onClick">点击回调</param>
        public void InitWithDraftData(CharacterBoxInfo info, Action<CharacterBoxInfo> onClick)
        {
            _draftData = info;
            _onDraftClick = onClick;
            _isDraft = true;

            RefreshNameText(info.name);
            RefreshAuditState(info.auditInfo);

            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(OnSelectBtnClick);
        }

        /// <summary>
        /// 以已发布模式初始化条目，绑定数据和点击回调。
        /// </summary>
        /// <param name="item">已发布数据（含互动信息）</param>
        /// <param name="onClick">点击回调</param>
        public void InitWithPublishedData(CharacterBoxPublishItem item, Action<CharacterBoxPublishItem> onClick)
        {
            _publishedData = item;
            _onPublishedClick = onClick;
            _isDraft = false;

            var info = item.characterBoxInfo;
            RefreshNameText(info != null ? info.name : string.Empty);
            RefreshAuditState(info?.auditInfo);

            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(OnSelectBtnClick);
        }

        /// <summary>
        /// 以统一数据模式初始化条目，供 BoxSceneStudioAdapter（OSA 适配器）调用。
        /// 草稿和已发布均通过 CharacterBoxPublishItem 统一传入，studioType 区分渲染模式。
        /// 点击回调统一返回 CharacterBoxPublishItem，调用方可按需取 characterBoxInfo 或 interactInfo。
        /// </summary>
        /// <param name="data">统一数据包（草稿时 interactInfo 为 null）</param>
        /// <param name="onSelect">点击回调，参数为被点击的数据包</param>
        /// <param name="studioType">列表类型（草稿 / 已发布），决定条目渲染模式</param>
        public void InitSelectMode(CharacterBoxPublishItem data, Action<CharacterBoxPublishItem> onSelect, StudioSubType studioType)
        {
            if (data == null)
                return;

            if (_goCreatRoot != null)
            {
                _goCreatRoot.SetActive(false);
            }

            // 名称文本仅"去创作"入口使用，普通条目隐藏
            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(false);
            }

            // 统一走 published 回调路径，避免 _draftData 为 null 导致点击无响应
            _publishedData = data;
            _onPublishedClick = onSelect;
            _isDraft = false;

            var info = data.characterBoxInfo;

            // 加载封面图，有 URL 则显示，无 URL 则隐藏封面区域
            if (_coverImage != null)
            {
                var coverUrl = info?.cover;
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    _coverImage.gameObject.SetActive(true);
                    _coverImage.Load(coverUrl);
                }
                else
                {
                    _coverImage.gameObject.SetActive(false);
                }
            }

            // 发布信息（购买数、价格）与审核状态互斥显示
            bool isPublished = studioType == StudioSubType.Published;
            RefreshPublishInfo(data, isPublished);
            RefreshAuditState(info?.auditInfo);

            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(OnSelectBtnClick);
        }

        // ──────────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────────

        /// <summary>
        /// 刷新发布信息区域（购买次数、价格、货币图标）的显示状态。
        /// 仅在已发布模式且非审核中/申诉中时显示 _publishRoot；
        /// 审核状态下 _publishRoot 隐藏，由 _underReviewBtn 代替显示。
        /// </summary>
        /// <param name="item">已发布数据包（含互动信息和盒子信息）</param>
        /// <param name="isPublished">是否为已发布列表模式</param>
        private void RefreshPublishInfo(CharacterBoxPublishItem item, bool isPublished)
        {
            if (_publishRoot == null)
                return;

            if (!isPublished)
            {
                _publishRoot.SetActive(false);
                return;
            }

            // 审核中/申诉中时隐藏 publishRoot，由 _underReviewBtn 代替显示
            var auditInfo = item?.characterBoxInfo?.auditInfo;
            var auditResult = auditInfo != null ? (AuditResult)auditInfo.auditResult : AuditResult.Passed;
            bool isUnderReview = auditResult == AuditResult.PendingToAudit || auditResult == AuditResult.Appealing;

            _publishRoot.SetActive(!isUnderReview);

            if (isUnderReview)
                return;

            // 购买/消费次数
            var consumeAmount = item?.interactInfo?.consumeAmount ?? 0;

            if (_buyNumText != null)
            {
                _buyNumText.text = consumeAmount.ToString();
            }

            // 价格及货币图标：有价格则显示图标和数值，免费则显示"免费"文案
            var info = item?.characterBoxInfo;
            var priceValue = info?.paymentInfo?.price ?? 0;
            bool hasPriceIcon = priceValue > 0;

            if (_priceIcon != null)
            {
                _priceIcon.gameObject.SetActive(hasPriceIcon);

                if (hasPriceIcon)
                {
                    _priceIcon.sprite = PgcUtils.LoadCurrencyIcon(info.paymentInfo.currencyType, gameObject);
                }
            }

            if (_priceText != null)
            {
                if (hasPriceIcon)
                {
                    _priceText.text = priceValue.ToString();
                }
                else
                {
                    _priceText.SetLocalText("免费");
                }
            }
        }

        /// <summary>
        /// 刷新名称文本显示。
        /// </summary>
        /// <param name="name">名称字符串</param>
        private void RefreshNameText(string name)
        {
            if (_nameText == null)
                return;

            _nameText.text = name;
        }

        /// <summary>
        /// 根据审核状态刷新审核提示按钮的显示。
        /// 审核中或申诉中时显示对应文字，其余状态隐藏。
        /// </summary>
        /// <param name="auditInfo">审核状态信息，可为 null</param>
        private void RefreshAuditState(AuditStatus auditInfo)
        {
            if (_underReviewBtn == null)
                return;

            if (auditInfo == null)
            {
                _underReviewBtn.gameObject.SetActive(false);
                return;
            }

            var auditResult = (AuditResult)auditInfo.auditResult;

            if (auditResult == AuditResult.PendingToAudit)
            {
                _underReviewText.text = "审核中";
                _underReviewBtn.gameObject.SetActive(true);
            }
            else if (auditResult == AuditResult.Appealing)
            {
                _underReviewText.text = "申诉中";
                _underReviewBtn.gameObject.SetActive(true);
            }
            else
            {
                _underReviewBtn.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 条目被点击时，根据当前模式触发对应回调。
        /// </summary>
        private void OnSelectBtnClick()
        {
            if (_isDraft)
            {
                if (_draftData == null)
                    return;

                _onDraftClick?.Invoke(_draftData);
            }
            else
            {
                if (_publishedData == null)
                    return;

                _onPublishedClick?.Invoke(_publishedData);
            }
        }
    }
}
