using System;
using System.Collections;
using System.IO;
using System.Linq;
using Es;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.Base;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: BOX角色草稿发布面板
    ///       左侧填写名字/描述/价格，右侧展示草稿封面预览，点击发布提交
    /// Date: 26-04-09
    /// </summary>
    public class IncubationPublishPanel : BasePanel<IncubationPublishPanel>
    {
        [Header("导航")]
        [SerializeField] private CButton Btn_Back;

        [Header("名字")]
        [SerializeField] private CButton Btn_NameEdit;
        [SerializeField] private SuperTextMesh Txt_Name;
        [SerializeField] private GameObject Go_NameEmpty;       // placeholder 文字节点
        [SerializeField] private Text Txt_NameLimit;            // "0/25"

        [Header("描述")]
        [SerializeField] private CButton Btn_DescEdit;
        [SerializeField] private SuperTextMesh Txt_Desc;
        [SerializeField] private GameObject Go_DescEmpty;       // placeholder 文字节点
        [SerializeField] private Text Txt_DescLimit;            // "0/250"

        [Header("价格")]
        [SerializeField] private Toggle[] PriceToggles;         // 按顺序对应动态计算的最低价及 +100 递增档位
        [SerializeField] private CButton Btn_CustomPrice;       // 自定义价格按钮
        [SerializeField] private GameObject Go_CustomSelected;  // 自定义选中态（含勾选图标和价格文字）
        [SerializeField] private Text Txt_CustomPriceValue;     // 自定义价格数值
        [SerializeField] private GameObject Go_CustomDefault;   // 默认态（"输入自定义金额"文字）
        [SerializeField] private Text Txt_PriceSubText;         // "每个商品你会获得等值的 X 个创作者币"

        [Header("右侧封面预览")]
        [SerializeField] private CabinCharacterCardItem PreviewItem;

        [Header("发布")]
        [SerializeField] private LoadingButton Btn_Publish;

        [Header("拍照")]
        [SerializeField] private Transform _portraitCharacterRoot;  // 预制件中固定的角色生成位置
        [SerializeField] private Camera _portraitCamera;            // 预制件中配置好的拍照相机

        // ──────────────── 常量 ────────────────

        private const int MaxPrice = 9999;
        private const int NameMaxLength = 25;
        private const int DescMaxLength = 250;

        // ──────────────── 运行时状态 ────────────────

        private CabinPublishData _data;
        private int[] _fixedPrices;
        private int _minPrice;
        private int _selectedPrice;
        private bool _isCustomPrice = false;

        private KeyBoardInfo _nameKeyBoardInfo;
        private KeyBoardInfo _descKeyBoardInfo;
        private KeyBoardInfo _priceKeyBoardInfo;

        // ──────────────── 生命周期 ────────────────

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
                type = 0,
                inputMode = 0,
                maxLength = NameMaxLength,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            _descKeyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                inputMode = 0,
                maxLength = DescMaxLength,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            _priceKeyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                returnKeyType = (int)ReturnType.Return
            };
        }

        public override void OnShow(params object[] args)
        {
            CabinCharacterUgcInfo cabinCharacterUgcInfo = args[0] as CabinCharacterUgcInfo;
            _data = new CabinPublishData(cabinCharacterUgcInfo);
            _isCustomPrice = false;

            if (string.IsNullOrEmpty(_data.characterInfo.name))
                _data.characterInfo.name = _data.characterInfo.name;

            CalculateMinPrice();
            _selectedPrice = _fixedPrices[0];
            _priceKeyBoardInfo.lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice);

            PreviewItem.SetData(_data.characterInfo, null);
            RefreshPriceToggleLabels();
            if (PriceToggles != null && PriceToggles.Length > 0)
                PriceToggles[0].SetIsOnWithoutNotify(true);
            RefreshNameUI();
            RefreshDescUI();
            RefreshCustomPriceUI();
        }

        public override void OnHidden()
        {
            PreviewItem.ClearData();
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

        private void CalculateMinPrice()
        {
            var info = _data.characterInfo;

            // 皮肤单品数量（官方和UGC均计入）
            int skinCount = CountSkinParts(info.skinPack);

            // 免费表：CabinPgcEmoConfig 中登记的 emoID 视为官方免费动作，不计价
            var freeEmoIdSet = BuildFreeEmoIdSet();

            // 计价动作数量：UGC 自制 + 钻石/扭蛋获取的 PGC 动作；免费表中的官方动作不计入
            int chargeableActionCount =
                  (info.pendingEmote?.emoteList?.Count(x => IsChargeableAction(x.emoteId, x.ugcData != null, freeEmoIdSet)) ?? 0)
                + (info.pendingEmote?.loopEmoteList?.Count(x => IsChargeableAction(x.emoteId, x.ugcData != null, freeEmoIdSet)) ?? 0)
                + (info.activation?.Count(x => IsChargeableAction(x.emoteId, x.ugcData != null, freeEmoIdSet)) ?? 0)
                + (info.voiceCommands?.Count(x => IsChargeableAction(x.emoteId, x.ugcData != null, freeEmoIdSet)) ?? 0);

            // UGC 音色数量（官方音色不计入，最多 1 个；通过 CabinToneNetManager 查询是否为 PGC）
            int ugcVoiceCount = 0;
            if (!string.IsNullOrEmpty(info.toneId))
            {
                var toneInfo = CabinToneNetManager.Inst.GetToneInfo(info.toneId);
                if (toneInfo != null && !toneInfo.IsPgc())
                    ugcVoiceCount = 1;
            }

            // 公式：100 + 皮肤单品数量*10 + 计价动作数量*50 + ugc音色数量*10
            int calculated = 100 + skinCount * 10 + chargeableActionCount * 50 + ugcVoiceCount * 10;

            // 计算结果低于 200 时，最低售价为 200
            _minPrice = Mathf.Max(200, calculated);

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
                try
                {
                    // 仅用于计数，不走 CharacterData.DeserializeObject（后者会填充缺失部位默认值导致数量虚高）
                    var charData = JsonConvert.DeserializeObject<CharacterData>(skin.avatarJson);
                    count += charData?.partDatas?.Count ?? 0;
                }
                catch { }
            }
            return count;
        }

        /// <summary>
        /// 汇总 CabinPgcEmoConfig 全表（所有 usetype）的免费 emoID，构建查询集合。
        /// 表中登记的 emoID 视为官方免费动作，不计入定价。
        /// 此外固定包含两个特殊动作 default（默认动作）、leisure（休闲动作），始终视为免费。
        /// </summary>
        /// <returns>所有免费 emoID 的集合（含 default、leisure）；表为空时仅含这两个特殊动作</returns>
        private static System.Collections.Generic.HashSet<string> BuildFreeEmoIdSet()
        {
            var set = new System.Collections.Generic.HashSet<string>();

            // 两个固定特殊动作：default（默认动作）、leisure（休闲动作）始终视为官方免费动作，不计价
            set.Add("default");
            set.Add("leisure");

            var list = Es.DataTables.GetCabinPgcEmoConfigList();

            if (list == null)
                return set;

            foreach (var config in list)
            {
                // emoID 为逗号分隔的字符串，逐行拆分后并入集合
                if (config == null || string.IsNullOrEmpty(config.emoID))
                    continue;

                var ids = config.emoID.Split(',');
                foreach (var id in ids)
                {
                    var trimmed = id.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        set.Add(trimmed);
                }
            }

            return set;
        }

        /// <summary>
        /// 判断单个动作是否计入定价。
        /// 规则：UGC 自制动作始终计价；emoID 在免费表中视为官方免费动作不计价；
        /// 其余（钻石购买/扭蛋获取的 PGC 动作）正常计价。
        /// </summary>
        /// <param name="emoteId">动作 ID</param>
        /// <param name="hasUgcData">是否为 UGC 自制动作（ugcData != null）</param>
        /// <param name="freeSet">免费 emoID 集合</param>
        /// <returns>true 表示该动作计入定价</returns>
        private static bool IsChargeableAction(string emoteId, bool hasUgcData, System.Collections.Generic.HashSet<string> freeSet)
        {
            // UGC 自制动作始终计价
            if (hasUgcData)
                return true;

            // 空槽位（无动作）不计价
            if (string.IsNullOrEmpty(emoteId))
                return false;

            // 免费表中的官方动作不计价，其余（钻石/扭蛋 PGC）计价
            return !freeSet.Contains(emoteId);
        }

        private void RefreshPriceToggleLabels()
        {
            for (int i = 0; i < PriceToggles.Length && i < _fixedPrices.Length; i++)
            {
                var label = PriceToggles[i].transform.Find("Label")?.GetComponent<Text>();
                if (label != null) label.text = _fixedPrices[i].ToString();
            }
        }

        private void OnBtnCustomPriceClick()
        {
            _priceKeyBoardInfo.defaultText = "";
            _priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
            _priceKeyBoardInfo.lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice);
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetCustomPriceFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_priceKeyBoardInfo));
        }

        private void OnGetCustomPriceFromNative(string input)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (!int.TryParse(input, out var value) || value < _minPrice || value > MaxPrice)
            {
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", _minPrice, MaxPrice));
                return;
            }
            if (PriceToggles != null && PriceToggles.Length > 0)
                PriceToggles[0].group?.SetAllTogglesOff(false);
            _selectedPrice = value;
            _isCustomPrice = true;
            RefreshCustomPriceUI();
        }

        private void RefreshCustomPriceUI()
        {
            Go_CustomSelected.SetActive(_isCustomPrice);
            Go_CustomDefault.SetActive(!_isCustomPrice);
            if (_isCustomPrice)
                Txt_CustomPriceValue.text = _selectedPrice.ToString();

            if (Txt_PriceSubText != null)
                Txt_PriceSubText.text = $"每个商品你会获得等值的{_selectedPrice / 2.0}个";
        }

        // ──────────────── 名字 ────────────────

        private void OnBtnNameEditClick()
        {
            _nameKeyBoardInfo.defaultText = _data.characterInfo.name ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKeyBoardInfo));
        }

        private void OnGetNameFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _data.characterInfo.name = value;
            RefreshNameUI();
        }

        private void RefreshNameUI()
        {
            var pubName = _data?.characterInfo?.name;
            bool isEmpty = string.IsNullOrEmpty(pubName);
            Go_NameEmpty.SetActive(isEmpty);
            Txt_Name.gameObject.SetActive(!isEmpty);
            if (!isEmpty)
                Txt_Name.text = pubName;
            Txt_NameLimit.text = $"{(pubName?.Length ?? 0)}/{NameMaxLength}";
        }

        // ──────────────── 描述 ────────────────

        private void OnBtnDescEditClick()
        {
            _descKeyBoardInfo.defaultText = _data.characterInfo.desc ?? "";
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKeyBoardInfo));
        }

        private void OnGetDescFromNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _data.characterInfo.desc = value;
            RefreshDescUI();
        }

        private void RefreshDescUI()
        {
            var pubDesc = _data?.characterInfo?.desc;
            bool isEmpty = string.IsNullOrEmpty(pubDesc);
            Go_DescEmpty.SetActive(isEmpty);
            Txt_Desc.gameObject.SetActive(!isEmpty);
            if (!isEmpty)
                Txt_Desc.text = pubDesc;
            Txt_DescLimit.text = $"{(pubDesc?.Length ?? 0)}/{DescMaxLength}";
        }

        // ──────────────── 发布 ────────────────

        /// <summary>
        /// 点击发布按钮：校验数据，解析 skinPack[0] 的角色信息，
        /// 启动截图协程，截图上传完成后再调用发布接口。
        /// </summary>
        private void OnBtnPublishClick()
        {
            if (string.IsNullOrEmpty(_data?.characterInfo?.name))
            {
                TipPanel.ShowToast("请填写作品名称");
                return;
            }

            // 校验 skinPack[0] 数据完整性
            var skinPack = _data?.characterInfo?.skinPack;

            if (skinPack == null || skinPack.Count == 0 || string.IsNullOrEmpty(skinPack[0].avatarJson))
            {
                TipPanel.ShowToast("角色数据不完整，无法生成头像");
                return;
            }

            // 在同步方法内解析，避免协程 try-catch 中使用 yield 的限制
            CharacterData characterData;
            try
            {
                characterData = CharacterData.DeserializeObject(skinPack[0].avatarJson);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError($"IncubationPublishPanel - 解析角色数据失败: {e.Message}");
                TipPanel.ShowToast("角色数据解析失败");
                return;
            }

            if (characterData == null)
            {
                TipPanel.ShowToast("角色数据解析失败");
                return;
            }

            _data.characterInfo.paymentInfo = new PaymentInfo
            {
                price = _selectedPrice,
                currencyType = CurrencyType.PinkCoin
            };

            Btn_Publish.ShowLoading();
            StartCoroutine(PublishWithPhotoCoroutine(characterData));
        }

        /// <summary>
        /// 发布协程：在预制件预设位置生成角色，用预制件相机截图并上传至 COS，
        /// 将头像 URL 写入 characterPortraitUrl 后调用服务器发布接口。
        /// 相机位置、角度、参数均在预制件中直接调节。
        /// </summary>
        /// <param name="characterData">已解析好的角色数据，来自 skinPack[0].avatarJson</param>
        private IEnumerator PublishWithPhotoCoroutine(CharacterData characterData)
        {
            // ── 1. 在预制件根节点下生成角色，等待所有部件加载完成 ────────
            bool characterLoaded = false;
            var characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(
                characterData,
                _portraitCharacterRoot,
                callback: () => { characterLoaded = true; });

            while (!characterLoaded)
            {
                yield return null;
            }

            // ── 2. 保存相机原始参数，绑定 RenderTexture，透明背景 ─────────
            var origClearFlags = _portraitCamera.clearFlags;
            var origBackground = _portraitCamera.backgroundColor;
            var origTargetTexture = _portraitCamera.targetTexture;

            var rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            _portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            var detailData= _data.characterInfo.coverInfo.GetDetail();
            var colorId = detailData.colorId;
            var config = DataTables.GetDraftBoxCardColorConfig(colorId);
            if (ColorUtility.TryParseHtmlString($"#{config.Color}", out Color colorstart))
                _portraitCamera.backgroundColor = colorstart;
            _portraitCamera.targetTexture = rt;

            // 等待当前帧渲染完毕后再读像素
            yield return new WaitForEndOfFrame();

            // ── 3. 读取像素并编码为 PNG ───────────────────────────────────
            var tex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] imgBytes = tex.EncodeToPNG();
            Destroy(tex);

            // ── 4. 还原相机参数，释放 RenderTexture，销毁临时生成的角色 ──
            _portraitCamera.clearFlags = origClearFlags;
            _portraitCamera.backgroundColor = origBackground;
            _portraitCamera.targetTexture = origTargetTexture;
            rt.Release();
            Destroy(characterWrap.Avatar.gameObject);

            // ── 7. 保存到本地并上传至 COS ─────────────────────────────────
            string coverUrl = null;
            bool uploadDone = false;

            string fileName = LocalDataUtils.Inst.SaveImgRes(imgBytes);
            var uri = $"IncubationCabin/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";

            CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
            {
                if (string.IsNullOrEmpty(err))
                {
                    coverUrl = url;
                }
                else
                {
                    LoggerUtils.LogError($"IncubationPublishPanel - 上传头像失败: {err}");
                }

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                uploadDone = true;
            });

            while (!uploadDone)
            {
                yield return null;
            }

            // ── 8. 赋值头像 URL（上传失败则保持空值，不阻断发布）────────
            if (!string.IsNullOrEmpty(coverUrl))
            {
                _data.characterInfo.characterPortraitUrl = coverUrl;
            }

            // ── 9. 调用发布接口 ────────────────────────────────────────────
            CabinNetManager.Inst.SetCabinCharacterInfo(_data.characterInfo, SetType.Publish, (isSuccess) =>
            {
                Btn_Publish.HideLoading();

                if (!isSuccess)
                {
                    return;
                }

                OnPublishSuccess();
            });
        }

        /// <summary>
        /// 发布成功后关闭面板并通知草稿列表刷新。
        /// </summary>
        private void OnPublishSuccess()
        {
            CloseSelf();
            MessageHelper.Broadcast(MessageName.OnCabinDraftListChange);
        }

#if UNITY_EDITOR
        // ──────────────── 编辑器调试：截图效果预览 ────────────────

        /// <summary>测试时临时生成的角色，用于在编辑器中预览截图构图</summary>
        private CharacterWrap _testCharacterWrap;

        /// <summary>
        /// 编辑器专用调试按钮：生成/清理测试角色，方便在运行时调整相机构图。
        /// 仅在 UNITY_EDITOR 宏下编译，不影响正式包体。
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 300f, 220f, 120f));
            GUILayout.Label("── 截图调试 ──");

            if (_testCharacterWrap == null)
            {
                if (GUILayout.Button("生成测试角色"))
                {
                    OnTestSpawnCharacter();
                }
            }
            else
            {
                if (GUILayout.Button("清理测试角色"))
                {
                    Destroy(_testCharacterWrap.Avatar.gameObject);
                    _testCharacterWrap = null;
                }
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// 从 skinPack[0] 解析角色数据并生成到 _portraitCharacterRoot，使用默认待机姿势。
        /// </summary>
        private void OnTestSpawnCharacter()
        {
            var skinPack = _data?.characterInfo?.skinPack;

            if (skinPack == null || skinPack.Count == 0 || string.IsNullOrEmpty(skinPack[0].avatarJson))
            {
                UnityEngine.Debug.LogWarning("IncubationPublishPanel - 测试角色：skinPack[0] 数据不完整");
                return;
            }

            var characterData = CharacterData.DeserializeObject(skinPack[0].avatarJson);

            if (characterData == null)
            {
                UnityEngine.Debug.LogWarning("IncubationPublishPanel - 测试角色：角色数据解析失败");
                return;
            }

            _testCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(
                characterData,
                _portraitCharacterRoot,
                callback: () => UnityEngine.Debug.Log("IncubationPublishPanel - 测试角色加载完成"));
        }
#endif
    }
}
