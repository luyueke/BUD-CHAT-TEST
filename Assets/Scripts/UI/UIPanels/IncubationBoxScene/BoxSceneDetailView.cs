using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UGCAsset;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using GameData.BaseInfo;
using GameData.UGCData;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// Box 场景工作室右侧详情视图，展示选中条目的封面、名称、更新时间，
    /// 并提供发布、编辑、重命名、复制、删除五个操作按钮。
    /// 草稿模式显示"发布"按钮，已发布模式隐藏该按钮。
    /// </summary>
    public class BoxSceneDetailView : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        // UI 字段（Inspector 绑定）
        // ──────────────────────────────────────────────

        /// <summary>关闭/收起详情视图按钮</summary>
        [SerializeField] private Button _closeBtn;

        /// <summary>发布按钮（仅草稿模式显示）</summary>
        [SerializeField] private Button _publishBtn;

        /// <summary>编辑按钮（打开 Box 场景编辑器）</summary>
        [SerializeField] private Button _editBtn;

        /// <summary>重命名按钮</summary>
        [SerializeField] private Button _renameBtn;

        /// <summary>复制草稿按钮</summary>
        [SerializeField] private Button _copyBtn;

        /// <summary>删除按钮</summary>
        [SerializeField] private Button _deleteBtn;

        /// <summary>Box 场景名称文本</summary>
        [SerializeField] private BUD_Text _nameText;

        /// <summary>最后编辑时间文本</summary>
        [SerializeField] private Text _updateTimeText;

        /// <summary>封面图（远端 URL 加载）</summary>
        [SerializeField] private RemoteImageBehaviour _mapCover;

        // ──────────────────────────────────────────────
        // 运行时状态
        // ──────────────────────────────────────────────

        /// <summary>当前展示的 Box 场景数据</summary>
        private CharacterBoxInfo _curInfo;

        /// <summary>当前条目所属的列表类型（草稿 / 已发布）</summary>
        private StudioSubType _curStudioType;

        // ──────────────────────────────────────────────
        // 公共初始化
        // ──────────────────────────────────────────────

        /// <summary>
        /// 初始化 UI 按钮监听，在 BoxSceneStudioMainPanel.OnCreate() 中调用一次。
        /// </summary>
        public void InitUI()
        {
            _closeBtn.onClick.AddListener(HidePanel);
            _publishBtn.onClick.AddListener(OnPublishBtnClick);
            _editBtn.onClick.AddListener(OnEditBtnClick);
            _renameBtn.onClick.AddListener(OnRenameBtnClick);
            _copyBtn.onClick.AddListener(OnCopyBtnClick);
            _deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        }

        /// <summary>
        /// 用选中的条目数据刷新详情视图并显示面板。
        /// </summary>
        /// <param name="info">要展示的 Box 场景数据</param>
        /// <param name="studioType">条目来源列表类型（Drafts=草稿，Published=已发布）</param>
        public void UpdateInfo(CharacterBoxInfo info, StudioSubType studioType)
        {
            if (info == null)
            {
                HidePanel();
                return;
            }

            _curInfo = info;
            _curStudioType = studioType;

            _nameText.text = info.name;
            _updateTimeText.text = $"最后编辑 {TimestampConverter.ConvertToDateTimeString(info.updateTime)}";

            // 草稿才显示"发布"按钮
            _publishBtn.gameObject.SetActive(studioType == StudioSubType.Drafts);

            // 加载封面图（若有 URL）
            if (!string.IsNullOrEmpty(info.cover))
            {
                _mapCover.Load(info.cover);
            }

            gameObject.SetActive(true);
        }

        // ──────────────────────────────────────────────
        // 按钮回调
        // ──────────────────────────────────────────────

        /// <summary>
        /// 点击"发布"按钮：打开 BoxScenePublishPanel 让用户填写名称、描述和价格后再发布。
        /// 非 VIP 用户发布前需下载草稿元数据，检查是否使用了相册图片（VIP 专属功能）；
        /// 若存在相册图片则弹出 JoinVipPanel 拦截发布，保持与编辑器内 OnPublishClicked 一致的行为。
        /// </summary>
        private void OnPublishBtnClick()
        {
            if (CheckDataIllegal())
                return;

            // VIP 用户或草稿无元数据（从未保存过）时，无需检查相册图片，直接打开发布面板
            bool isVip = VipDataManager.Inst.isVip;
            if (isVip || string.IsNullOrEmpty(_curInfo.metaDataUrl))
            {
                UIManager.Inst.OpenPanel(PanelId.BoxScenePublishPanel, _curInfo, (Action)OnDetailPublishSuccess, (Action)OnDetailPublishBack);
                return;
            }

            // 非 VIP：下载草稿元数据，检查各面是否包含相册图片
            _publishBtn.interactable = false;
            var assetRequest = Asset.LoadRemoteAssetAsync(_curInfo.metaDataUrl);

            if (assetRequest == null)
            {
                LoggerUtils.LogError($"[BoxSceneDetailView] 发布前元数据请求创建失败，URL={_curInfo.metaDataUrl}");
                _publishBtn.interactable = true;
                // 请求创建失败时放行，由后续服务端发布流程兜底
                UIManager.Inst.OpenPanel(PanelId.BoxScenePublishPanel, _curInfo, (Action)OnDetailPublishSuccess, (Action)OnDetailPublishBack);
                return;
            }

            assetRequest.completed += req =>
            {
                _publishBtn.interactable = true;
                assetRequest.Release();

                // 尝试解析元数据，检测是否存在相册图片
                bool hasPhotos = false;

                if (req.result == Request.Result.Success)
                {
                    var remoteReq = req as RemoteAssetRequest;
                    var json = System.Text.Encoding.UTF8.GetString(remoteReq.asset);
                    var boxSceneData = JsonConvert.DeserializeObject<UGCBoxSceneData>(json);

                    if (boxSceneData?.parts != null)
                    {
                        for (int i = 0; i < boxSceneData.parts.Count; i++)
                        {
                            if (boxSceneData.parts[i].photos != null && boxSceneData.parts[i].photos.Count > 0)
                            {
                                hasPhotos = true;
                                break;
                            }
                        }
                    }
                }

                // 使用了相册图片但非 VIP：拦截发布，引导开通 VIP
                if (hasPhotos)
                {
                    string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：添加手机相册图片");
                    UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, titleStr, new List<JoinVipType>
                    {
                        JoinVipType.Image
                    });
                    return;
                }

                UIManager.Inst.OpenPanel(PanelId.BoxScenePublishPanel, _curInfo, (Action)OnDetailPublishSuccess, (Action)OnDetailPublishBack);
            };
        }

        /// <summary>
        /// BoxScenePublishPanel 发布成功后的回调：隐藏详情视图并刷新已发布列表。
        /// 仅在发布（或申诉）成功时由 BoxScenePublishPanel 触发，BackBtn 关闭时不会调用此方法。
        /// </summary>
        private void OnDetailPublishSuccess()
        {
            HidePanel();
            RefreshPublishedList();
        }

        /// <summary>
        /// BoxScenePublishPanel 点击 BackBtn 关闭时的回调：仅隐藏详情视图，不切换已发布页签。
        /// </summary>
        private void OnDetailPublishBack()
        {
            HidePanel();
        }

        /// <summary>
        /// 点击"编辑"按钮：从 COS 下载历史像素数据后再打开 Box 场景 UGC 编辑器面板。
        /// 若 metaDataUrl 为空（首次创建尚未保存过），直接以空白状态打开编辑器。
        /// 下载期间禁用按钮，防止重复点击；下载失败时 Toast 提示并恢复按钮可用。
        /// </summary>
        private void OnEditBtnClick()
        {
            if (CheckDataIllegal())
                return;

            // VIP 用户使用 64×64 格子，非 VIP 使用 32×32 格子（参考 UGCResourceEditPanel 链路）
            var boxSceneInfo = new BoxSceneInfo
            {
                id = _curInfo.id,
                cover = _curInfo.cover,
                metaDataUrl = _curInfo.metaDataUrl,
                canvasType = VipDataManager.Inst.isVip ? (int)CanvasType.Canvas_64 : (int)CanvasType.Canvas_32,
            };

            // 没有历史数据 URL（草稿从未保存过），直接以空白状态打开
            if (string.IsNullOrEmpty(_curInfo.metaDataUrl))
            {
                OpenEditorWithData(new UGCBoxSceneData { id = _curInfo.id }, boxSceneInfo);
                return;
            }

            // 禁用按钮，防止重复点击
            _editBtn.interactable = false;

            // 异步下载 COS 上的历史像素 JSON，完成后再打开编辑器
            var assetRequest = Asset.LoadRemoteAssetAsync(_curInfo.metaDataUrl);

            if (assetRequest == null)
            {
                LoggerUtils.LogError($"[BoxSceneDetailView] 元数据请求创建失败，URL={_curInfo.metaDataUrl}");
                TipPanel.ShowToast("加载历史数据失败，请稍后重试");
                _editBtn.interactable = true;
                return;
            }

            assetRequest.completed += req =>
            {
                _editBtn.interactable = true;

                if (req.result != Request.Result.Success)
                {
                    LoggerUtils.LogError($"[BoxSceneDetailView] 元数据下载失败，URL={_curInfo.metaDataUrl}，error={req.error}");
                    TipPanel.ShowToast("加载历史数据失败，请稍后重试");
                    return;
                }

                var remoteReq = req as RemoteAssetRequest;
                var json = System.Text.Encoding.UTF8.GetString(remoteReq.asset);
                var boxSceneData = JsonConvert.DeserializeObject<UGCBoxSceneData>(json);

                if (boxSceneData == null)
                {
                    LoggerUtils.LogError($"[BoxSceneDetailView] 元数据 JSON 解析失败，URL={_curInfo.metaDataUrl}");
                    TipPanel.ShowToast("加载历史数据失败，请稍后重试");
                    return;
                }

                // 确保 id 与当前草稿一致
                boxSceneData.id = _curInfo.id;

                OpenEditorWithData(boxSceneData, boxSceneInfo);
                assetRequest.Release();
            };
        }

        /// <summary>
        /// 用已准备好的数据打开 Box 场景编辑器并隐藏详情视图。
        /// </summary>
        /// <param name="boxSceneData">像素绘制数据（含5个面的 parts）</param>
        /// <param name="boxSceneInfo">草稿元数据（id、cover、metaDataUrl）</param>
        private void OpenEditorWithData(UGCBoxSceneData boxSceneData, BoxSceneInfo boxSceneInfo)
        {
            UIManager.Inst.OpenPanel(PanelId.UGCBoxSceneEditorPanel, boxSceneData, boxSceneInfo, _curInfo);
            HidePanel();
        }

        /// <summary>
        /// 点击"重命名"按钮：打开名称输入面板。
        /// </summary>
        private void OnRenameBtnClick()
        {
            var panel = UIManager.Inst.OpenPanel<EditNamePanel>(PanelId.EditNamePanel, "重命名", _curInfo.name, "确定");
            panel.SetOnClickAction(ConfirmRename);
        }

        /// <summary>
        /// 重命名确认回调：调用 SetCharacterBox(Edit) 更新名称，成功后刷新草稿列表。
        /// </summary>
        /// <param name="newName">用户输入的新名称</param>
        private void ConfirmRename(string newName)
        {
            if (CheckDataIllegal())
                return;

            _curInfo.name = newName;
            CabinBoxSceneNetManager.Inst.SetCharacterBox(SetType.Edit, _curInfo, (success, info) =>
            {
                UIManager.Inst.ClosePanel(PanelId.EditNamePanel);

                if (!success)
                {
                    LoggerUtils.LogError("Box 场景重命名失败");
                    return;
                }

                HidePanel();
                RefreshDraftsList();
            });
        }

        /// <summary>
        /// 点击"复制"按钮：调用 SetCharacterBox(Copy)，成功后刷新草稿列表。
        /// </summary>
        private void OnCopyBtnClick()
        {
            if (CheckDataIllegal())
                return;

            CabinBoxSceneNetManager.Inst.SetCharacterBox(SetType.Copy, _curInfo, (success, info) =>
            {
                if (!success)
                {
                    LoggerUtils.LogError("Box 场景复制失败");
                    return;
                }

                HidePanel();
                RefreshDraftsList();
            });
        }

        /// <summary>
        /// 点击"删除"按钮：弹出确认弹窗，用户确认后执行删除。
        /// </summary>
        private void OnDeleteBtnClick()
        {
            var confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            confirmPanel.SetText("确认删除", "你确定要删除该 Box 场景吗？\n一旦删除就无法找回", "删除", "取消");
            confirmPanel.SetOnClickAction(ConfirmDelete, CancelDelete);
        }

        /// <summary>
        /// 删除确认回调：调用 SetCharacterBox(Delete)，成功后关闭面板并刷新对应列表。
        /// </summary>
        private void ConfirmDelete()
        {
            if (CheckDataIllegal())
                return;

            CabinBoxSceneNetManager.Inst.SetCharacterBox(SetType.Delete, _curInfo, (success, info) =>
            {
                if (!success)
                {
                    LoggerUtils.LogError("Box 场景删除失败");
                    return;
                }

                HidePanel();

                if (_curStudioType == StudioSubType.Drafts)
                {
                    RefreshDraftsList();
                }
                else
                {
                    RefreshPublishedList();
                }
            });
        }

        /// <summary>
        /// 删除取消回调，不做任何操作。
        /// </summary>
        private void CancelDelete()
        {
        }

        // ──────────────────────────────────────────────
        // 私有辅助
        // ──────────────────────────────────────────────

        /// <summary>
        /// 隐藏详情视图面板。
        /// </summary>
        private void HidePanel()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 检查当前数据是否合法，若为 null 则记录错误并返回 true。
        /// </summary>
        /// <returns>数据非法时返回 true，合法时返回 false</returns>
        private bool CheckDataIllegal()
        {
            if (_curInfo == null)
            {
                LoggerUtils.LogError("BoxSceneDetailView - 当前数据为空");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 广播草稿列表变更消息，触发 BoxSceneStudioMainPanel 刷新草稿列表。
        /// </summary>
        private void RefreshDraftsList()
        {
            MessageHelper.Broadcast(MessageName.OnCabinSceneDraftListChange);
        }

        /// <summary>
        /// 广播已发布列表变更消息，触发 BoxSceneStudioMainPanel 刷新已发布列表。
        /// </summary>
        private void RefreshPublishedList()
        {
            MessageHelper.Broadcast(MessageName.OnCabinScenePublishListChange);
        }
    }
}
