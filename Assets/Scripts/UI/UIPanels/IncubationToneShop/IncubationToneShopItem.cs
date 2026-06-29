using System;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationToneShopItem : MonoBehaviour
    {
        [SerializeField] private Text NameText;
        [SerializeField] private RemoteImageBehaviour CoverImage;
        [SerializeField] private CButton PreviewBtn;
        [SerializeField] private Button SelectBtn;
        [SerializeField] private Button Btn_Create;
        [SerializeField] private Button Btn_GoStore;
        [SerializeField] private GameObject Go_Selected;
        [SerializeField] private GameObject Go_Normal;

        private CabinToneInfo _toneInfo;
        private Action _onPreview;
        private Action _onSelect;
        private CabinCharacterUgcInfo _cabinCharacter;

        private void Awake()
        {
            if (PreviewBtn != null)
            {
                PreviewBtn.onClick.AddListener(OnPreviewBtnClick);
            }

            if (SelectBtn != null)
            {
                SelectBtn.onClick.AddListener(OnSelectBtnClick);
            }

            if (Btn_Create != null)
            {
                Btn_Create.onClick.AddListener(OnCreateBtnClick);
            }

            if (Btn_GoStore != null)
            {
                Btn_GoStore.onClick.AddListener(OnGoStoreBtnClick);
            }
        }

        public void SetData(
            RecommendItemData data,
            Action<RecommendItemData> onPreview = null,
            Action<RecommendItemData> onSelect = null)
        {
            if (data == null)
            {
                return;
            }

            var toneInfo = data.UgcInfo as CabinToneInfo;
            Action wrappedPreview = onPreview != null ? () => onPreview.Invoke(data) : null;
            Action wrappedSelect = onSelect != null ? () => onSelect.Invoke(data) : null;
            SetDataInternal(toneInfo, wrappedPreview, wrappedSelect);
        }

        /// <summary>
        /// 用于孵化舱基础信息界面：按 toneId 查找并显示音色名称，
        /// SelectBtn 充当"更换音色"按钮，PreviewBtn 隐藏
        /// </summary>
        public void SetData(string toneId, Action onSelect = null, Action onPreview = null)
        {
            SetDataInternal(null, onPreview, onSelect);

            if (string.IsNullOrEmpty(toneId))
            {
                return;
            }

            var toneInfo = CabinToneNetManager.Inst.GetToneInfo(toneId);
            if (toneInfo != null)
            {
                SetDataInternal(toneInfo, onPreview, onSelect);
                return;
            }

            var cfg = DataTables.GetCabinUgcAnimBgmConfig(toneId);
            if (cfg != null)
            {
                SetNameText(cfg.toneName);
                return;
            }

            CabinToneNetManager.Inst.GetCabinToneInfo(toneId, isSucc =>
            {
                if (this != null && isSucc)
                {
                    var info = CabinToneNetManager.Inst.GetToneInfo(toneId);
                    if (info != null)
                    {
                        SetDataInternal(info, onPreview, onSelect);
                    }
                }
            });
        }

        public void InitSelectMode(CabinToneInfo info, Action<CabinToneInfo> onSelect, Action<CabinToneInfo> onPreview)
        {
            Action wrappedSelect = onSelect != null
                ? () =>
                {
                    onPreview?.Invoke(info);
                    onSelect.Invoke(info);
                    SetSelectState(true);
                }
                : null;
            Action wrappedPreview = onPreview != null ? () => onPreview.Invoke(info) : null;
            SetDataInternal(info, wrappedPreview, wrappedSelect);
        }

        public void InitCreateMode(CabinCharacterUgcInfo cabinCharacter)
        {
            _cabinCharacter = cabinCharacter;
            Btn_Create?.gameObject.SetActive(true);
            Go_Normal?.SetActive(false);
            Btn_GoStore?.gameObject.SetActive(false);
            SelectBtn?.gameObject.SetActive(false);
            PreviewBtn?.gameObject.SetActive(false);
            Go_Selected?.SetActive(false);
            if (NameText != null)
            {
                NameText.text = string.Empty;
            }
        }

        public void InitGoStoreMode()
        {
            Btn_GoStore?.gameObject.SetActive(true);
            Go_Normal?.SetActive(false);
            Btn_Create?.gameObject.SetActive(false);
            SelectBtn?.gameObject.SetActive(false);
            PreviewBtn?.gameObject.SetActive(false);
            Go_Selected?.SetActive(false);
            if (NameText != null)
            {
                NameText.text = string.Empty;
            }
        }

        private void SetDataInternal(CabinToneInfo toneInfo, Action onPreview, Action onSelect)
        {
            _toneInfo = toneInfo;
            _onPreview = onPreview;
            _onSelect = onSelect;

            if (PreviewBtn != null)
            {
                PreviewBtn.gameObject.SetActive(onPreview != null);
            }

            if (SelectBtn != null)
            {
                SelectBtn.gameObject.SetActive(onSelect != null);
            }

            Btn_Create?.gameObject.SetActive(false);
            Btn_GoStore?.gameObject.SetActive(false);
            Go_Normal?.SetActive(true);
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (NameText != null)
            {
                NameText.text = _toneInfo?.name ?? string.Empty;
            }

            if (CoverImage == null || _toneInfo == null)
            {
                return;
            }

            if (_toneInfo.IsPgc())
            {
                // 清空待处理 URL，防止进行中的 UGC 异步加载完成后覆盖此 PGC 封面
                CoverImage._CurrentRequestedURL = string.Empty;
                // 确保封面图可见（UGC 加载过程中会将其隐藏）
                CoverImage.gameObject.SetActive(true);
                CoverImage.RawImage.texture = XAssetLoaderMgr.Inst.LoadResource<Texture>(
                    $"Assets/Loadable/UI/UIPanel/IncubationToneShop/cover/{_toneInfo.id}.png", gameObject);
            }
            else
            {
                var coverUrl = _toneInfo.cover;
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    CoverImage.gameObject.SetActive(false);
                    CoverImage.Load(coverUrl, true,
                        (from, success) => { CoverImage.gameObject.SetActive(true); });
                }
            }
        }

        private void SetNameText(string text)
        {
            if (NameText != null)
            {
                NameText.text = text;
            }
        }

        public void SetSelectState(bool isSelected)
        {
            if (Go_Selected != null)
            {
                Go_Selected.SetActive(isSelected);
            }
        }

        private void OnPreviewBtnClick()
        {
            _onPreview?.Invoke();
        }

        private void OnSelectBtnClick()
        {
            _onSelect?.Invoke();
        }

        private void OnCreateBtnClick()
        {
            UIManager.Inst.OpenPanel<CabinPublishUgcAnimTonePanel>(PanelId.CabinPublishUgcAnimTonePanel, _cabinCharacter);
        }

        private void OnGoStoreBtnClick()
        {
            UIManager.Inst.OpenPanel<PublishUgcAnimTonePanel>(PanelId.IncubationToneShopPanel);
        }
    }
}
