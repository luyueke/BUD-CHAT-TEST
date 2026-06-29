using System.Linq;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using GameData;
using GameData.Base;
using GameUI;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 扩展包发布面板。
    /// 左侧填写扩展包名字/描述/价格，右侧展示所属角色卡面及包含内容预览，点击发布提交。
    /// </summary>
    public class CabinExtPackPublishPanel : BasePanel<CabinExtPackPublishPanel>
    {
        [Header("导航")]
        [SerializeField] private CButton Btn_Back;

        [Header("所属角色")]
        [SerializeField] private Text Txt_CharacterName;

        [Header("名字")]
        [SerializeField] private CButton Btn_NameEdit;
        [SerializeField] private SuperTextMesh Txt_Name;
        [SerializeField] private GameObject Go_NameEmpty;
        [SerializeField] private Text Txt_NameLimit;            // "0/10"

        [Header("描述")]
        [SerializeField] private CButton Btn_DescEdit;
        [SerializeField] private SuperTextMesh Txt_Desc;
        [SerializeField] private GameObject Go_DescEmpty;
        [SerializeField] private Text Txt_DescLimit;            // "0/200"

        [Header("价格")]
        [SerializeField] private Toggle[] PriceToggles;         // 动态计算档位，从最低价开始每档+100
        [SerializeField] private CButton Btn_CustomPrice;
        [SerializeField] private GameObject Go_CustomSelected;
        [SerializeField] private Text Txt_CustomPriceValue;
        [SerializeField] private GameObject Go_CustomDefault;
        [SerializeField] private Text Txt_PriceSubText;

        [Header("右侧预览")]
        [SerializeField] private CabinSkinCardItem skinItem;

        [Header("底部按钮")]
        [SerializeField] private LoadingButton Btn_Publish;

        private const int MaxPrice = 9999;
        private const int NameMaxLength = 10;
        private const int DescMaxLength = 200;

        private CabinCharacterPackInfo _packInfo;
        private CabinCharacterUgcInfo _characterUgcInfo;   // 所属角色信息，由打开方传入
        private int[] _fixedPrices;
        private int _minPrice;
        private int _selectedPrice;
        private bool _isCustomPrice;

        private KeyBoardInfo _nameKeyBoardInfo;
        private KeyBoardInfo _descKeyBoardInfo;
        private KeyBoardInfo _priceKeyBoardInfo;

        public override void OnCreate()
        {
            Btn_Back.onClick.AddListener(CloseSelf);
            Btn_NameEdit.onClick.AddListener(OnBtnNameEditClick);
            Btn_DescEdit.onClick.AddListener(OnBtnDescEditClick);
            Btn_CustomPrice.onClick.AddListener(OnBtnCustomPriceClick);
            Btn_Publish.onClick.AddListener(OnBtnPublishClick);

            InitPriceToggles();

            _nameKeyBoardInfo = new KeyBoardInfo
            {
                type = 0, inputMode = 0, maxLength = NameMaxLength,
                inputFlag = 0, textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            _descKeyBoardInfo = new KeyBoardInfo
            {
                type = 0, inputMode = 0, maxLength = DescMaxLength,
                inputFlag = 0, textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            _priceKeyBoardInfo = new KeyBoardInfo
            {
                type = 0, inputMode = 1, maxLength = 4,
                inputFlag = 0, textSecurity = 1,
                returnKeyType = (int)ReturnType.Return
            };
        }

        public override void OnShow(params object[] args)
        {
            _packInfo = args[0] as CabinCharacterPackInfo;
            _characterUgcInfo = args.Length > 1 ? args[1] as CabinCharacterUgcInfo : null;
            _isCustomPrice = false;
            CalculateMinPrice();
            _selectedPrice = _fixedPrices[0];
            _priceKeyBoardInfo.lengthTips = LocalizationManager.Inst.GetLocalizedText(
                "请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice);

            if (PriceToggles != null && PriceToggles.Length > 0)
                PriceToggles[0].SetIsOnWithoutNotify(true);

            RefreshPriceToggleLabels();
            RefreshCharacterSection();
            RefreshNameUI();
            RefreshDescUI();
            RefreshCustomPriceUI();
            RefreshPreview();
        }

        // ──────────────── 价格 ────────────────

        private void InitPriceToggles()
        {
            for (int i = 0; i < PriceToggles.Length; i++)
            {
                int idx = i;
                PriceToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (!isOn) return;
                    _selectedPrice = _fixedPrices[idx];
                    _isCustomPrice = false;
                    RefreshCustomPriceUI();
                });
            }
        }

        private void OnBtnCustomPriceClick()
        {
            _priceKeyBoardInfo.defaultText = "";
            _priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
            _priceKeyBoardInfo.lengthTips = LocalizationManager.Inst.GetLocalizedText(
                "请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice);
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetCustomPriceFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_priceKeyBoardInfo));
        }

        private void OnGetCustomPriceFromNative(string input)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (!int.TryParse(input, out var value) || value < _minPrice || value > MaxPrice)
            {
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText(
                    "请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice));
                return;
            }
            if (PriceToggles != null && PriceToggles.Length > 0)
                PriceToggles[0].group?.SetAllTogglesOff(false);
            _selectedPrice = value;
            _isCustomPrice = true;
            RefreshCustomPriceUI();
        }

        private void CalculateMinPrice()
        {
            // 皮肤单品数量
            int skinCount = CountSkinParts(_packInfo.skinPack);

            // UGC 动作数量（官方/PGC 动作不计入，通过 ugcData 是否有值区分）
            int ugcActionCount = (_packInfo.pendingEmote?.emoteList?.Count(x => x.ugcData != null) ?? 0)
                               + (_packInfo.pendingEmote?.loopEmoteList?.Count(x => x.ugcData != null) ?? 0)
                               + (_packInfo.activation?.Count(x => x.ugcData != null) ?? 0)
                               + (_packInfo.voiceCommands?.Count(x => x.ugcData != null) ?? 0);

            // 公式：100 + 皮肤单品数量*10 + ugc动作数量*50
            int calculated = 100 + skinCount * 10 + ugcActionCount * 50;

            // 计算结果低于 150 时，最低售价为 150
            _minPrice = Mathf.Max(150, calculated);

            _fixedPrices = new int[PriceToggles.Length];
            for (int i = 0; i < _fixedPrices.Length; i++)
                _fixedPrices[i] = _minPrice + i * 100;
        }

        private static int CountSkinParts(System.Collections.Generic.List<SkinPackInfo> skinPack)
        {
            if (skinPack == null) return 0;
            int count = 0;
            foreach (var skin in skinPack)
            {
                if (string.IsNullOrEmpty(skin.avatarJson)) continue;
                try { count += JsonConvert.DeserializeObject<CharacterData>(skin.avatarJson)?.partDatas?.Count ?? 0; }
                catch { }
            }
            return count;
        }

        private void RefreshPriceToggleLabels()
        {
            for (int i = 0; i < PriceToggles.Length && i < _fixedPrices.Length; i++)
            {
                var label = PriceToggles[i].transform.Find("Label")?.GetComponent<Text>();
                if (label != null) label.text = _fixedPrices[i].ToString();
            }
        }

        private void RefreshCustomPriceUI()
        {
            Go_CustomSelected.SetActive(_isCustomPrice);
            Go_CustomDefault.SetActive(!_isCustomPrice);
            if (_isCustomPrice)
                Txt_CustomPriceValue.text = _selectedPrice.ToString();
            if (Txt_PriceSubText != null)
                Txt_PriceSubText.text = $"每个商品你会获得等值的 {_selectedPrice / 2.0} 个        创作者币";
        }

        // ──────────────── 名字 ────────────────

        private void OnBtnNameEditClick()
        {
            _nameKeyBoardInfo.defaultText = _packInfo?.name ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKeyBoardInfo));
        }

        private void OnGetNameFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _packInfo.name = value;
            RefreshNameUI();
            RefreshPreview();
        }

        private void RefreshNameUI()
        {
            var pubName = _packInfo?.name;
            bool isEmpty = string.IsNullOrEmpty(pubName);
            Go_NameEmpty.SetActive(isEmpty);
            Txt_Name.gameObject.SetActive(!isEmpty);
            if (!isEmpty) Txt_Name.text = pubName;
            Txt_NameLimit.text = $"{(pubName?.Length ?? 0)}/{NameMaxLength}";
        }

        // ──────────────── 描述 ────────────────

        private void OnBtnDescEditClick()
        {
            _descKeyBoardInfo.defaultText = _packInfo?.desc ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKeyBoardInfo));
        }

        private void OnGetDescFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _packInfo.desc = value;
            RefreshDescUI();
        }

        private void RefreshDescUI()
        {
            var pubDesc = _packInfo?.desc;
            bool isEmpty = string.IsNullOrEmpty(pubDesc);
            Go_DescEmpty.SetActive(isEmpty);
            Txt_Desc.gameObject.SetActive(!isEmpty);
            if (!isEmpty) Txt_Desc.text = pubDesc;
            Txt_DescLimit.text = $"{(pubDesc?.Length ?? 0)}/{DescMaxLength}";
        }

        // ──────────────── 所属角色 / 右侧预览 ────────────────

        private void RefreshCharacterSection()
        {
            if (_packInfo == null)
                return;

            var characterInfo = _characterUgcInfo;
            if (characterInfo == null)
            {
                return;
            }

            var displayName = !string.IsNullOrEmpty(characterInfo.name)
                ? characterInfo.name : _packInfo.name;

            if (Txt_CharacterName != null) 
                Txt_CharacterName.text = displayName;
        }

        private void RefreshPreview()
        {
            if (_packInfo != null)
            {
                skinItem.SetData(_packInfo);
            }
        }

        // ──────────────── 发布 ────────────────

        private void OnBtnPublishClick()
        {
            if (string.IsNullOrEmpty(_packInfo?.name))
            {
                TipPanel.ShowToast("请填写皮肤名称");
                return;
            }

            _packInfo.paymentInfo = new PaymentInfo
            {
                price = _selectedPrice,
                currencyType = CurrencyType.PinkCoin
            };

            Btn_Publish.ShowLoading();
            SetType setType = string.IsNullOrEmpty(_packInfo.id) ? SetType.ForcePublish : SetType.Publish;
            CabinNetManager.Inst.SetCabinCharacterPackInfo(_packInfo, setType, (isSuccess, _) =>
            {
                Btn_Publish.HideLoading();

                if (!isSuccess)
                    return;

                // 提前捕获角色信息，防止面板关闭后字段被清空
                var charInfo = _characterUgcInfo;
                UIManager.Inst.ClosePanel(PanelId.CabinExtPackPublishPanel);
                CheckIsInBoxThenSync(charInfo);
            });
        }

        /// <summary>
        /// 皮肤发布成功后，检测所属角色是否在 BUD BOX 中展示。
        /// 若在，弹出「前往控制台同步」提示弹窗，提醒用户前往控制台同步最新皮肤配置。
        /// </summary>
        /// <param name="characterUgcInfo">本次发布皮肤所属的角色信息（在面板关闭前提前捕获）</param>
        private void CheckIsInBoxThenSync(CabinCharacterUgcInfo characterUgcInfo)
        {
            if (characterUgcInfo == null)
                return;

            CabinBoxManager.Inst.InitBudBoxDic(success =>
            {
                if (!success)
                    return;

                // 遍历所有绑定的 BOX，判断当前角色是否正在某台 BOX 中展示
                bool isInBox = false;
                var boxList = CabinBoxManager.Inst.GetBudBoxList();

                foreach (var box in boxList)
                {
                    if (box.characterInfo?.id == characterUgcInfo.stockUgcId)
                    {
                        isInBox = true;
                        break;
                    }
                }

                if (!isInBox)
                    return;

                // 角色正在 BUD BOX 中展示，弹出二级确认框提示同步
                var confirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(
                    PanelId.CommonBoxConfirmWithTitlePanel);

                confirmPanel.SetTextAndAction(
                    "前往BUD BOX控制台",
                    "当前伙伴正在BUD BOX中展示，可前往控制台同步最新配置。",
                    "前往控制台",
                    "不了",
                    confirmClick: () =>
                    {
                        CabinBoxManager.Inst.OpenBoxEntrance();
                    },
                    cancelClick: null
                );
            });
        }
    }
}
