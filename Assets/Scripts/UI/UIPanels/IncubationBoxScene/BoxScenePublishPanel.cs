using System;
using GameData.Base;
using Message;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

namespace Game.IncubationBoxScene
{
    /// <summary>
    /// 盒子场景发布面板。
    /// 左侧让用户填写名称、描述和定价（最低 50 粉币，预设档位以 10 粉币递增），
    /// 右侧通过 BudBoxModel 展示盒子 3D 模型（由 CharacterBoxInfo.metaDataUrl 驱动）。
    /// 点击"发布盒子"后调用发布接口，并处理审核拒绝（错误码 501）与申诉流程。
    /// 入口：UGCBoxSceneEditorPanel（上传草稿后）和 BoxSceneDetailView（从草稿列表发布）。
    /// </summary>
    public class BoxScenePublishPanel : BasePanel<BoxScenePublishPanel>
    {
        // ──────────────────────────────────────────────
        // Inspector 字段
        // ──────────────────────────────────────────────

        [Header("导航")]
        [SerializeField] private Button Btn_Back;

        [Header("名字")]
        [SerializeField] private Button Btn_NameEdit;
        [SerializeField] private SuperTextMesh Txt_Name;
        [SerializeField] private GameObject Go_NameEmpty;     // placeholder 文字节点
        [SerializeField] private Text Txt_NameLimit;          // "0/25"

        [Header("描述")]
        [SerializeField] private Button Btn_DescEdit;
        [SerializeField] private SuperTextMesh Txt_Desc;
        [SerializeField] private GameObject Go_DescEmpty;     // placeholder 文字节点
        [SerializeField] private Text Txt_DescLimit;          // "0/250"

        [Header("价格")]
        [SerializeField] private Toggle[] PriceToggles;       // 5 个预设档位（50/60/70/80/90）
        [SerializeField] private Button Btn_CustomPrice;      // 自定义价格按钮
        [SerializeField] private GameObject Go_CustomSelected;// 自定义选中态（勾选图标 + 价格文字）
        [SerializeField] private Text Txt_CustomPriceValue;   // 自定义价格数值
        [SerializeField] private GameObject Go_CustomDefault; // 默认态（"输入自定义金额"文字）
        [SerializeField] private Text Txt_PriceSubText;       // "每个商品你会获得等值的 X 个创作者币"

        [Header("右侧盒子 3D 模型预览")]
        [SerializeField] private BudBoxModel _budBoxModel;

        [Header("发布")]
        [SerializeField] private LoadingButton Btn_Publish;

        // ──────────────────────────────────────────────
        // 常量
        // ──────────────────────────────────────────────

        /// <summary>最低定价（粉币）</summary>
        private const int MinPrice = 50;

        /// <summary>最高定价（粉币）</summary>
        private const int MaxPrice = 9999;

        /// <summary>预设档位递增步长（粉币）</summary>
        private const int PriceStep = 10;

        /// <summary>名称最大字符数</summary>
        private const int NameMaxLength = 25;

        /// <summary>描述最大字符数</summary>
        private const int DescMaxLength = 250;

        // ──────────────────────────────────────────────
        // 运行时状态
        // ──────────────────────────────────────────────

        /// <summary>当前待发布的盒子数据（从调用方传入，已上传封面与元数据）</summary>
        private CharacterBoxInfo _boxInfo;

        /// <summary>发布成功后由调用方提供的回调（如退出游戏或刷新列表）</summary>
        private Action _onSuccessCallback;

        /// <summary>点击 BackBtn 关闭面板时由调用方提供的回调（如隐藏详情视图）</summary>
        private Action _onBackCallback;

        /// <summary>当前选中价格（粉币）</summary>
        private int _selectedPrice = MinPrice;

        /// <summary>是否为自定义价格（即非预设档位）</summary>
        private bool _isCustomPrice;

        /// <summary>是否正在发布中（防止重复点击）</summary>
        private bool _isPublishing;

        /// <summary>5 个预设价格档位数组（MinPrice, MinPrice+PriceStep, ...）</summary>
        private int[] _fixedPrices;

        // 移动端键盘参数
        private KeyBoardInfo _nameKBInfo;
        private KeyBoardInfo _descKBInfo;
        private KeyBoardInfo _priceKBInfo;

        // ──────────────────────────────────────────────
        // 生命周期
        // ──────────────────────────────────────────────

        /// <summary>
        /// 面板创建时初始化：绑定按钮事件、构建 Toggle 监听、初始化键盘参数。
        /// </summary>
        public override void OnCreate()
        {
            Btn_Back.onClick.AddListener(OnBackBtnClick);
            Btn_NameEdit.onClick.AddListener(OnBtnNameEditClick);
            Btn_DescEdit.onClick.AddListener(OnBtnDescEditClick);
            Btn_CustomPrice.onClick.AddListener(OnBtnCustomPriceClick);
            Btn_Publish.onClick.AddListener(OnBtnPublishClick);

            InitPriceToggles();
            InitKeyBoardInfos();
      
        }

        /// <summary>
        /// 面板显示时加载数据：接收盒子信息和成功回调，刷新各区域 UI。
        /// </summary>
        /// <param name="args">args[0]: CharacterBoxInfo（已含 cover URL）；args[1]: Action 成功回调</param>
        public override void OnShow(params object[] args)
        {
            UIManager.Inst.ClosePanel(PanelId.UGCBoxSceneEditorPanel);
            _boxInfo = args[0] as CharacterBoxInfo;
            _onSuccessCallback = args.Length > 1 ? args[1] as Action : null;
            _onBackCallback = args.Length > 2 ? args[2] as Action : null;

            _isPublishing = false;
            _isCustomPrice = false;

            // 计算 5 个预设价格档位
            _fixedPrices = new int[PriceToggles.Length];

            for (int i = 0; i < _fixedPrices.Length; i++)
            {
                _fixedPrices[i] = MinPrice + i * PriceStep;
            }

            // 更新价格键盘的范围提示
            _priceKBInfo.lengthTips = LocalizationManager.Inst.GetLocalizedText(
                "请输入一个介于{0}和{1}之间的整数。", MinPrice, MaxPrice);

            RefreshPriceToggleLabels();

            // 回显已有定价；若无或低于最低价，默认选第一档
            if (_boxInfo?.paymentInfo != null && _boxInfo.paymentInfo.price >= MinPrice)
            {
                RestorePaymentInfo(_boxInfo.paymentInfo.price);
            }
            else
            {
                _selectedPrice = MinPrice;
                _isCustomPrice = false;

                if (PriceToggles != null && PriceToggles.Length > 0)
                {
                    PriceToggles[0].SetIsOnWithoutNotify(true);
                }

                RefreshCustomPriceUI();
            }

            // 加载右侧盒子 3D 模型
            if (_budBoxModel != null)
            {
                _budBoxModel.Clear();
                _budBoxModel.LoadBoxScene(_boxInfo?.metaDataUrl);
            }

            RefreshNameUI();
            RefreshDescUI();
        }

        /// <summary>面板隐藏时重置发布状态，并销毁 3D 模型释放资源。</summary>
        public override void OnHidden()
        {
            _isPublishing = false;
            Btn_Publish?.HideLoading();
            _budBoxModel?.Clear();
        }

        // ──────────────────────────────────────────────
        // 初始化
        // ──────────────────────────────────────────────

        /// <summary>
        /// 绑定 5 个预设 Toggle 的 onValueChanged 事件。
        /// </summary>
        private void InitPriceToggles()
        {
            if (PriceToggles == null)
            {
                return;
            }

            for (int i = 0; i < PriceToggles.Length; i++)
            {
                int idx = i;
                PriceToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (!isOn)
                    {
                        return;
                    }

                    _selectedPrice = _fixedPrices[idx];
                    _isCustomPrice = false;
                    RefreshCustomPriceUI();
                });
            }
        }

        /// <summary>
        /// 初始化名称、描述、价格三种移动端键盘的参数配置。
        /// </summary>
        private void InitKeyBoardInfos()
        {
            _nameKBInfo = new KeyBoardInfo
            {
                type = 0,
                inputMode = 0,
                maxLength = NameMaxLength,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                inputMode = 0,
                maxLength = DescMaxLength,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            _priceKBInfo = new KeyBoardInfo
            {
                type = 0,
                inputMode = 1,       // 数字键盘
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                returnKeyType = (int)ReturnType.Return
            };
        }

        // ──────────────────────────────────────────────
        // 价格
        // ──────────────────────────────────────────────

        /// <summary>
        /// 刷新 5 个预设 Toggle 的标签文字（显示对应粉币数量）。
        /// </summary>
        private void RefreshPriceToggleLabels()
        {
            if (PriceToggles == null)
            {
                return;
            }

            for (int i = 0; i < PriceToggles.Length && i < _fixedPrices.Length; i++)
            {
                var label = PriceToggles[i].transform.Find("Label")?.GetComponent<Text>();

                if (label != null)
                {
                    label.text = _fixedPrices[i].ToString();
                }
            }
        }

        /// <summary>
        /// 回显已保存的价格：若属于预设档位则选中对应 Toggle，否则视为自定义价格。
        /// </summary>
        /// <param name="price">已保存的价格（粉币）</param>
        private void RestorePaymentInfo(int price)
        {
            bool isPreset = false;

            for (int i = 0; i < _fixedPrices.Length; i++)
            {
                if (_fixedPrices[i] == price)
                {
                    _selectedPrice = price;
                    _isCustomPrice = false;
                    PriceToggles[i].SetIsOnWithoutNotify(true);
                    isPreset = true;
                    break;
                }
            }

            if (!isPreset)
            {
                // 自定义价格：取消所有 Toggle 选中态
                if (PriceToggles != null && PriceToggles.Length > 0 && PriceToggles[0].group != null)
                {
                    PriceToggles[0].group.SetAllTogglesOff(false);
                }

                _selectedPrice = price;
                _isCustomPrice = true;
            }

            RefreshCustomPriceUI();
        }

        /// <summary>
        /// 点击自定义价格按钮：弹出数字键盘让用户输入自定义金额。
        /// </summary>
        private void OnBtnCustomPriceClick()
        {
            _priceKBInfo.defaultText = "";
            _priceKBInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetCustomPriceFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_priceKBInfo));
        }

        /// <summary>
        /// 收到移动端键盘回调：验证价格范围后更新选中价格。
        /// </summary>
        /// <param name="input">用户输入的字符串</param>
        private void OnGetCustomPriceFromNative(string input)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            if (!int.TryParse(input, out var value) || value < MinPrice || value > MaxPrice)
            {
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText(
                    "请输入一个介于{0}和{1}之间的整数。", MinPrice, MaxPrice));
                return;
            }

            // 取消所有预设 Toggle 选中
            if (PriceToggles != null && PriceToggles.Length > 0)
            {
                PriceToggles[0].group?.SetAllTogglesOff(false);
            }

            _selectedPrice = value;
            _isCustomPrice = true;
            RefreshCustomPriceUI();
        }

        /// <summary>
        /// 刷新自定义价格区域 UI（选中态、价格数值文字、收益文案）。
        /// </summary>
        private void RefreshCustomPriceUI()
        {
            Go_CustomSelected?.SetActive(_isCustomPrice);
            Go_CustomDefault?.SetActive(!_isCustomPrice);

            if (_isCustomPrice && Txt_CustomPriceValue != null)
            {
                Txt_CustomPriceValue.text = _selectedPrice.ToString();
            }

            if (Txt_PriceSubText != null)
            {
                // 按 50% 收益比例显示创作者币收益
                Txt_PriceSubText.text = $"每个商品你会获得等值的{_selectedPrice / 2.0}个创作者币";
            }
        }

        // ──────────────────────────────────────────────
        // 名称
        // ──────────────────────────────────────────────

        /// <summary>点击名称编辑按钮：弹出文字键盘。</summary>
        private void OnBtnNameEditClick()
        {
            _nameKBInfo.defaultText = _boxInfo?.name ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
        }

        /// <summary>收到名称键盘回调：写入数据并刷新显示。</summary>
        /// <param name="value">用户输入的名称</param>
        private void OnGetNameFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            if (_boxInfo != null)
            {
                _boxInfo.name = value;
            }

            RefreshNameUI();
        }

        /// <summary>刷新名称区域显示（placeholder / 文本 / 字符数）。</summary>
        private void RefreshNameUI()
        {
            var name = _boxInfo?.name;
            bool isEmpty = string.IsNullOrEmpty(name);

            Go_NameEmpty?.SetActive(isEmpty);

            if (Txt_Name != null)
            {
                Txt_Name.gameObject.SetActive(!isEmpty);

                if (!isEmpty)
                {
                    Txt_Name.text = name;
                }
            }

            if (Txt_NameLimit != null)
            {
                Txt_NameLimit.text = $"{name?.Length ?? 0}/{NameMaxLength}";
            }
        }

        // ──────────────────────────────────────────────
        // 描述
        // ──────────────────────────────────────────────

        /// <summary>点击描述编辑按钮：弹出文字键盘。</summary>
        private void OnBtnDescEditClick()
        {
            _descKBInfo.defaultText = _boxInfo?.desc ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        /// <summary>收到描述键盘回调：写入数据并刷新显示。</summary>
        /// <param name="value">用户输入的描述</param>
        private void OnGetDescFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            if (_boxInfo != null)
            {
                _boxInfo.desc = value;
            }

            RefreshDescUI();
        }

        /// <summary>刷新描述区域显示（placeholder / 文本 / 字符数）。</summary>
        private void RefreshDescUI()
        {
            var desc = _boxInfo?.desc;
            bool isEmpty = string.IsNullOrEmpty(desc);

            Go_DescEmpty?.SetActive(isEmpty);

            if (Txt_Desc != null)
            {
                Txt_Desc.gameObject.SetActive(!isEmpty);

                if (!isEmpty)
                {
                    Txt_Desc.text = desc;
                }
            }

            if (Txt_DescLimit != null)
            {
                Txt_DescLimit.text = $"{desc?.Length ?? 0}/{DescMaxLength}";
            }
        }

        // ──────────────────────────────────────────────
        // 发布
        // ──────────────────────────────────────────────

        /// <summary>
        /// 点击"发布盒子"按钮：校验名称 → 组装 PaymentInfo → 调用发布接口。
        /// </summary>
        private void OnBtnPublishClick()
        {
            if (_isPublishing)
            {
                return;
            }

            if (string.IsNullOrEmpty(_boxInfo?.name))
            {
                TipPanel.ShowToast("请填写作品名称");
                return;
            }

            // 组装定价信息
            _boxInfo.paymentInfo = new PaymentInfo
            {
                price = _selectedPrice,
                currencyType = CurrencyType.PinkCoin
            };

            _isPublishing = true;
            Btn_Publish.ShowLoading();

            LoggerUtils.Log($"[BoxScenePublishPanel] 开始发布，id={_boxInfo.id}，price={_selectedPrice}");

            CabinBoxSceneNetManager.Inst.SetCharacterBox(
                SetType.Publish,
                _boxInfo,
                callback: (success, result) =>
                {
                    Btn_Publish.HideLoading();
                    _isPublishing = false;

                    if (success)
                    {
                        OnPublishSuccess();
                    }
                },
                onFail: HandlePublishFail);
        }

        /// <summary>
        /// 处理发布接口失败：501 为审核拒绝（弹申诉弹窗），其他错误码统一处理后 Toast 提示。
        /// </summary>
        /// <param name="errRspStr">原始失败响应 JSON 字符串</param>
        private void HandlePublishFail(string errRspStr)
        {
            Btn_Publish.HideLoading();
            _isPublishing = false;

            var rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(errRspStr);

            if (rsp == null)
            {
                LoggerUtils.LogError($"[BoxScenePublishPanel] 发布失败，无法解析响应: {errRspStr}");
                return;
            }

            if (rsp.result == 501)
            {
                // 审核拒绝：展示拒绝原因并提供申诉入口
                LoggerUtils.Log($"[BoxScenePublishPanel] 发布被审核拒绝，reason={rsp.rmsg}");

                var rejectReason = string.IsNullOrEmpty(rsp.rmsg) ? "内容审核未通过，请修改后重新发布" : rsp.rmsg;
                var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
                panel.SetTextAndAction(
                    "审核未通过",
                    rejectReason,
                    "申诉",
                    "取消",
                    confirmClick: OnDoAppeal,
                    cancelClick: null);
            }
            else
            {
                new HttpErrorCodeHandler().HandleErrorCodeResult(rsp);
                LoggerUtils.LogError($"[BoxScenePublishPanel] 发布失败，code={rsp.result}，msg={rsp.rmsg}");
                TipPanel.ShowToast(rsp.rmsg);
            }
        }

        /// <summary>
        /// 用户点击"申诉"：在本地将审核状态标记为申诉中，视为发布成功并触发后续流程。
        /// 服务端会通过后续轮询更新真实的审核状态。
        /// </summary>
        private void OnDoAppeal()
        {
            LoggerUtils.Log($"[BoxScenePublishPanel] 用户发起申诉，id={_boxInfo?.id}");

            if (_boxInfo != null)
            {
                if (_boxInfo.auditInfo == null)
                {
                    _boxInfo.auditInfo = new AuditStatus { auditResult = (int)AuditResult.Appealing };
                }
                else
                {
                    _boxInfo.auditInfo.auditResult = (int)AuditResult.Appealing;
                }
            }

            OnPublishSuccess();
        }

        /// <summary>
        /// 发布（或申诉）成功后：广播列表刷新消息，执行成功回调，关闭当前面板。
        /// </summary>
        private void OnPublishSuccess()
        {
            LoggerUtils.Log($"[BoxScenePublishPanel] 发布成功，id={_boxInfo?.id}");
            MessageHelper.Broadcast(MessageName.OnCabinPublishListChange);
            _onSuccessCallback?.Invoke();
            CloseSelf();
        }

        /// <summary>
        /// 点击 BackBtn 关闭面板：执行 Back 回调后关闭面板，不触发成功回调。
        /// </summary>
        private void OnBackBtnClick()
        {
            _onBackCallback?.Invoke();
            CloseSelf();
        }

        /// <summary>
        /// 关闭面板，不执行任何回调（回调已由 OnPublishSuccess / OnBackBtnClick 各自负责）。
        /// </summary>
        public override void CloseSelf()
        {
            base.CloseSelf();
        }
    }
}
