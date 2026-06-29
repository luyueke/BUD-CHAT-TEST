using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 扩展包列表 Item，展示在 PublishedBoxMenu 的扩展包区域内。
    /// 根据 paymentInfo 切换草稿态（编辑/发布按钮）与已发布态（价格/下架按钮）。
    /// </summary>
    public class IncubationExtensionPackItem : MonoBehaviour
    {
        [SerializeField] private Text NameTxt;                  // 扩展包名称
        [SerializeField] private CabinSkinCardItem incubationDraftBoxItem; //卡面对象
        [Header("状态徽章")]
        [SerializeField] private GameObject PublishedBadgeGo;   // 已发布 Badge
        [SerializeField] private GameObject DraftBadgeGo;       // 草稿 Badge
        [SerializeField] private GameObject UnPublishedBadgeGo;       // 已下架 Badge

        [Header("已发布状态操作区")]
        [SerializeField] private Text PriceTxt;                 // 价格文本（如 "15"）
        [SerializeField] private Button Btn_Unpublish;          // 下架此扩展包
        [SerializeField] private Button Btn_Copy;               // 拷贝扩展包


        [Header("草稿状态操作区")]
        [SerializeField] private Button Btn_Edit;               // 编辑扩展包草稿
        [SerializeField] private Button Btn_Publish;            // 发布扩展包
        [SerializeField] private Button Btn_Delect;             // 删除扩展包
        [SerializeField] private Button Btn_CopyEx;               // 拷贝扩展包

        [Header("已下架状态操作区")]
        [SerializeField] private Button Btn_Delect1;             // 删除扩展包
        [SerializeField] private Button Btn_CopyEx1;               // 拷贝扩展包

        [SerializeField] private Sprite[] Spr_Publish = new Sprite[2];             // 删除扩展包


        public CabinCharacterPackInfo _data;
        private Action _onDelect;    // 删除回调
        private Action _onUnpublish;    // 下架回调
        private Action _onEdit;         // 编辑回调
        private Action _onPublish;      // 发布回调
        private Action<CabinCharacterPackInfo> _onCopy;         // 拷贝回调

        public string PackId => _data?.id;

        private void Awake()
        {
            Btn_Delect.onClick.AddListener(() => _onDelect?.Invoke());
            Btn_Delect1.onClick.AddListener(() => _onDelect?.Invoke());
            Btn_Unpublish.onClick.AddListener(() => _onUnpublish?.Invoke());
            Btn_Edit.onClick.AddListener(() => _onEdit?.Invoke());
            Btn_Publish.onClick.AddListener(() => _onPublish?.Invoke());
            Btn_Copy.onClick.AddListener(() => _onCopy?.Invoke(_data));
            Btn_CopyEx.onClick.AddListener(() => _onCopy?.Invoke(_data));
            Btn_CopyEx1.onClick.AddListener(() => _onCopy?.Invoke(_data));
        }

        /// <summary>
        /// 设置扩展包数据并刷新 UI。
        /// 根据 data.paymentInfo 自动切换草稿态（编辑/发布按钮）与已发布态（价格/下架按钮）。
        /// </summary>
        public void SetData(CabinCharacterPackInfo data, CabinCharacterUgcInfo characterUgcInfo,
            Action onDelect = null, Action onUnpublish = null, Action onEdit = null, Action onPublish = null, Action<CabinCharacterPackInfo> onCopy = null)
        {
            _data = data;
            _onDelect = onDelect;
            _onUnpublish = onUnpublish;
            _onEdit = onEdit;
            _onPublish = onPublish;
            _onCopy = onCopy;
            bool isPublished = data.ugcclass == (int)UGCClass.Published;

            NameTxt.text = data.name;

            PublishedBadgeGo.SetActive(false);
            UnPublishedBadgeGo.SetActive(false);
            DraftBadgeGo.SetActive(false);

            if (isPublished)
            {
                PublishedBadgeGo.SetActive(data.IsUnpublished() == emUnpublished.None);
                UnPublishedBadgeGo.SetActive(data.IsUnpublished() > emUnpublished.None);
            }
            else
            {
                DraftBadgeGo.SetActive(true);
            }

            if (isPublished)
                PriceTxt.text = data.paymentInfo.price.ToString();

            if (characterUgcInfo.ugcclass == (int)UGCClass.Published)
            {
                Btn_Publish.image.sprite = Spr_Publish[1];
            }
            else
            {
                Btn_Publish.image.sprite = Spr_Publish[0];
            }
            incubationDraftBoxItem.SetData(data);
        }
    }
}