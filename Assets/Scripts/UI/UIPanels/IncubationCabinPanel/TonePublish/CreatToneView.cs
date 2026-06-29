using Game;
using Game.Audio;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class CreatToneView : MonoBehaviour
    {
        [Header("SelectRoot")]
        [SerializeField] private GameObject SelectRoot;
        [SerializeField] private Button Btn_RecordTone;
        [SerializeField] private Button Btn_AddTone;

        [Header("ToCopy")]
        [SerializeField] private GameObject ToCopy;
        [SerializeField] private Text CopyStateText;
        [SerializeField] private Button CopyBtn_Start;
        [SerializeField] private Button CopyBtn_Stop;
        [SerializeField] private Text RecordTimeText;
        [SerializeField] private Text RecordScriptText;  // 录制界面朗读文案

        [Header("Preview")]
        [SerializeField] private GameObject Preview;
        [SerializeField] private Text TimeText;
        [SerializeField] private Button PreviewBtn;
        [SerializeField] private Button PauseBtn;               // 暂停按钮，与 PreviewBtn 互斥显示
        [SerializeField] private Button RestCreatBtn;
        [SerializeField] private Button CreatToneBtn;
        [SerializeField] private Slider PreviewProgressSlider;  // 预览音频播放进度条

        [Header("Createing")]
        [SerializeField] private GameObject Createing;

        [Header("ToPublish")]
        [SerializeField] private GameObject ToPublishGo;
        [SerializeField] private Text ToPublish_TimeText;
        [SerializeField] private Button ToPublish_PreviewBtn;
        [SerializeField] private Button ToPublish_PauseBtn;         // 暂停按钮，与 ToPublish_PreviewBtn 互斥显示
        [SerializeField] private Button RestCopyBtn;
        [SerializeField] private Slider ToPublishProgressSlider;    // 克隆音色试听进度条

        [Header("克隆次数")]
        [SerializeField] private GameObject cloneCountRoot;
        [SerializeField] private Text cloneCountText;
        [SerializeField] private Button cloneCountBtn;

        [Header("CreatTone")]
        [SerializeField] private CButton PublishToneBtn;      // 发布按钮（亮起态），条件满足时可见
        [SerializeField] private Button GrayPublishToneBtn;   // 发布按钮（置灰态），条件不满足时可见，点击弹提示

        [Header("Language")]
        [SerializeField] private Text TitleText;
        [SerializeField] private Button LanguageDown;
        [SerializeField] private RectTransform LanguageArrow;
        [SerializeField] private GameObject LanguageDownList;
        [SerializeField] private Toggle EnglishLanguageDown;
        [SerializeField] private Toggle JapanLanguageDown;
        [SerializeField] private LanguageItem ChinaItem;
        [SerializeField] private LanguageItem EnglishItem;
        [SerializeField] private LanguageItem JapanItem;

        // (metaDataUrl, languageList) -> 通知主面板进入 PublishTone 视图
        public Action<string, List<ToneLanguageData>> onGoToPublish;

        private int _recordSeconds = 0;
        private Coroutine _recordTimerCoroutine;
        private Coroutine _previewProgressCoroutine;      // Preview 进度条更新协程句柄
        private bool _isPreviewPaused = false;            // Preview 音频是否处于暂停状态
        private Coroutine _toPublishProgressCoroutine;    // ToPublish 进度条更新协程句柄
        private bool _isToPublishPaused = false;          // ToPublish 音频是否处于暂停状态
        private AudioSource _previewAudioSource;
        private string _recordedAudioUrl = "";
        private string _voiceId = "";
        private float _uploadedAudioDuration = 0f;
        private bool _isUploadedAudio = false;
        private bool _recordStartFailed = false;

        // 上传音频时长上限（秒）与业务上限对齐，原生 Album Picker 只展示 ≤AudioDurationMax 的视频；
        // 客户端回调中再次校验下限（AudioDurationMin）
        private const int UploadAudioLimit = AudioDurationMax;

        // 音频时长合法范围（秒），录制和上传均适用
        private const int AudioDurationMin = 10;
        private const int AudioDurationMax = 20;

        /// <summary>各语言录制引导文案，key 为语言类型（0=中文，1=英文，2=日文）</summary>
        private static readonly Dictionary<int, string> RecordScripts = new Dictionary<int, string>
        {
            {
                0,
                "欢迎来到碧优蒂的世界。\r\n在这里，我和大家默认有三个简单的共识：\r\n第一，开心是最重要的，希望这里能让你放松。\r\n第二，摸鱼是可以的，合理的休息很有必要。\r\n第三，就是记得有我在，你可以随时和我互动，我会一直在这里。"
            },
            {
                1,
                "Welcome to BUD\r\nHere, we have three simple understandings. \r\nFirst, happiness is the priority — may this be a place for you to unwind. \r\nSecond, it's okay to slack off sometimes — reasonable breaks are necessary. \r\nThird, remember that I'm here — you can interact with me anytime, and I'll always be right here.\r\nMake yourself at home."
            },
            {
                2,
                "ビューティーの世界へようこそ。\r\nここでは、3つの簡単な共通認識があります。\r\n第一に、楽しむことが最も重要です。ここがあなたの安らぎの場となりますように。\r\n第二に、時には息抜きも必要です。合理的な休息は大切ですから。\r\n第三に、私がここにいることを覚えていてください。いつでも私とやり取りでき、私はずっとここにいます。\r\nどうぞ、ごゆっくり。"
            }
        };

        /// <summary>各语言对应的标题文本，key 为语言类型（0=中文，1=英文，2=日文）</summary>
        private static readonly Dictionary<int, string> LanguageTitles = new Dictionary<int, string>
        {
            { 0, "中文音色" },
            { 1, "英语音色" },
            { 2, "日语音色" }
        };

        /// <summary>各语言对应的音色试听文本，用于生成音色后调用 GetBatchPreviewByVoiceId 合成试听语音</summary>
        private static readonly Dictionary<int, string> TonePreviewTexts = new Dictionary<int, string>
        {
            {
                0,
                "欢迎来到碧优蒂的世界。在这里，我和大家默认有三个简单的共识：第一，开心是最重要的，希望这里能让你放松。第二，摸鱼是可以的，合理的休息很有必要。第三，就是记得有我在，你可以随时和我互动，我会一直在这里。"
            },
            {
                1,
                "Welcome to BUDHere, we have three simple understandings. First, happiness is the priority — may this be a place for you to unwind. Second, it's okay to slack off sometimes — reasonable breaks are necessary. Third, remember that I'm here — you can interact with me anytime, and I'll always be right here.Make yourself at home."
            },
            {
                2,
                "ビューティーの世界へようこそ。ここでは、3つの簡単な共通認識があります。第一に、楽しむことが最も重要です。ここがあなたの安らぎの場となりますように。第二に、時には息抜きも必要です。合理的な休息は大切ですから。第三に、私がここにいることを覚えていてください。いつでも私とやり取りでき、私はずっとここにいます。どうぞ、ごゆっくり。"
            }
        };

        // 多语言状态机
        private enum ToneCreationStep { SelectRoot, ToCopy, Preview, Createing, ToPublish }

        private class LanguageToneState
        {
            public ToneCreationStep step = ToneCreationStep.SelectRoot;
            public string audioUrl = "";
            public string voiceId = "";
            public string durationText = "";
            public bool isUploaded = false;
            /// <summary>生成音色后，通过 GetBatchPreviewByVoiceId 获取到的试听音频 URL</summary>
            public string previewAudioUrl = "";
            /// <summary>克隆试听音频的实际时长（秒），由 FetchPreviewAudioDuration 下载后写入</summary>
            public float previewAudioDuration = 0f;
            /// <summary>该语言的试听音频请求是否正在进行中（用于区分「请求中」与「请求已失败」）</summary>
            public bool isPreviewRequesting = false;
        }

        private Dictionary<int, LanguageToneState> _languageStates;
        private int _currentLanguage = 0;
        private bool _isDropdownOpen = false;
        private ToneCreationStep _currentStep = ToneCreationStep.SelectRoot;

        public void Init()
        {
            Btn_RecordTone.onClick.AddListener(OnBtnRecordToneClick);
            Btn_AddTone.onClick.AddListener(OnBtnAddToneClick);
            CopyBtn_Start.onClick.AddListener(OnCopyBtnStartClick);
            CopyBtn_Stop.onClick.AddListener(OnCopyBtnStopClick);
            PreviewBtn.onClick.AddListener(PlayRecordedAudio);
            PauseBtn.onClick.AddListener(PauseRecordedAudio);
            RestCreatBtn.onClick.AddListener(GoSelectRoot);
            CreatToneBtn.onClick.AddListener(OnCreatToneBtnClick);
            ToPublish_PreviewBtn.onClick.AddListener(PlayTonePreviewAudio);
            ToPublish_PauseBtn.onClick.AddListener(PauseToPublishAudio);
            RestCopyBtn.onClick.AddListener(GoSelectRoot);
            PublishToneBtn.onClick.AddListener(OnPublishToneBtnClick);
            GrayPublishToneBtn.onClick.AddListener(OnGrayPublishToneBtnClick);

            if (cloneCountBtn != null)
                cloneCountBtn.onClick.AddListener(OpenCloneShop);

            // Language
            _languageStates = new Dictionary<int, LanguageToneState>
            {
                { 0, new LanguageToneState() }
            };
            _currentLanguage = 0;
            _isDropdownOpen = false;

            LanguageDown.onClick.AddListener(OnLanguageDownBtnClick);
            EnglishLanguageDown.onValueChanged.AddListener(OnEnglishLanguageDownChanged);
            JapanLanguageDown.onValueChanged.AddListener(OnJapanLanguageDownChanged);
            ChinaItem.Init(() => SwitchToLanguage(0));
            EnglishItem.Init(() => SwitchToLanguage(1), OnDeleteEnglishBtnClick);
            JapanItem.Init(() => SwitchToLanguage(2), OnDeleteJapanBtnClick);

            EnglishLanguageDown.SetIsOnWithoutNotify(false);
            JapanLanguageDown.SetIsOnWithoutNotify(false);
            EnglishItem.gameObject.SetActive(false);
            JapanItem.gameObject.SetActive(false);
            LanguageDownList.SetActive(false);
            LanguageArrow.localRotation = Quaternion.identity;
            UpdateLanguageSelectState(0);

            ShowSubState(SelectRoot);
            CopyBtn_Stop.gameObject.SetActive(false);
            RecordTimeText.text = "0s";
            // 初始化朗读文案和标题为当前语言（默认中文）
            UpdateRecordScript();
            UpdateTitleText();

            // 初始化进度条：只读（不可拖拽），起始位置为 0
            InitProgressSlider(PreviewProgressSlider);
            InitProgressSlider(ToPublishProgressSlider);

            // 初始化按钮互斥状态：默认显示 PreviewBtn，隐藏 PauseBtn
            SetPreviewButtonState(isPlaying: false);
            SetToPublishButtonState(isPlaying: false);

            // 初始化发布按钮状态：初始无任何克隆，置灰
            RefreshPublishBtnState();
        }

        private void ShowSubState(GameObject target)
        {
            SelectRoot.SetActive(target == SelectRoot);
            ToCopy.SetActive(target == ToCopy);
            Preview.SetActive(target == Preview);
            Createing.SetActive(target == Createing);
            ToPublishGo.SetActive(target == ToPublishGo);

            if (target == SelectRoot)
            {
                _currentStep = ToneCreationStep.SelectRoot;
            }
            else if (target == ToCopy)
            {
                _currentStep = ToneCreationStep.ToCopy;
            }
            else if (target == Preview)
            {
                _currentStep = ToneCreationStep.Preview;
                // 每次进入 Preview 视图都重置：停止播放、进度条归零、显示 PreviewBtn
                StopPreviewAudio();
                // 同时停止可能残留的 ToPublish 进度协程（切语言时两个状态会交替）
                StopToPublishProgress();
            }
            else if (target == Createing)
            {
                _currentStep = ToneCreationStep.Createing;
            }
            else if (target == ToPublishGo)
            {
                _currentStep = ToneCreationStep.ToPublish;
                // 每次进入 ToPublish 视图都重置：停止试听、进度条归零、显示 PreviewBtn
                StopToPublishAudio();
            }
        }

        /// <summary>切换 Preview 区域播放/暂停按钮的互斥显示状态。</summary>
        private void SetPreviewButtonState(bool isPlaying)
            => SetAudioButtonState(PreviewBtn, PauseBtn, isPlaying);

        /// <summary>切换 ToPublish 区域播放/暂停按钮的互斥显示状态。</summary>
        private void SetToPublishButtonState(bool isPlaying)
            => SetAudioButtonState(ToPublish_PreviewBtn, ToPublish_PauseBtn, isPlaying);

        /// <summary>停止 ToPublish 进度条更新协程，并将进度条归零。</summary>
        private void StopToPublishProgress()
            => StopProgressCoroutine(ref _toPublishProgressCoroutine, ToPublishProgressSlider);

        /// <summary>
        /// 完全停止 ToPublish 试听音频（包括暂停中的音频），重置所有相关状态：
        /// - 停止 AkSoundManager UGC URL 音频
        /// - 清除暂停标志
        /// - 停止进度条协程并归零
        /// - 恢复 ToPublish_PreviewBtn 显示（隐藏 PauseBtn）
        /// </summary>
        private void StopToPublishAudio()
        {
            AkSoundManager.Inst.StopUGCAudio(gameObject);
            _isToPublishPaused = false;
            StopToPublishProgress();
            SetToPublishButtonState(isPlaying: false);
        }

        /// <summary>停止 Preview 进度条更新协程，并将进度条归零。</summary>
        private void StopPreviewProgress()
            => StopProgressCoroutine(ref _previewProgressCoroutine, PreviewProgressSlider);

        /// <summary>
        /// 完全停止预览音频（包括暂停中的音频），重置所有相关状态：
        /// - 停止 AkSoundManager UGC URL 音频 / 本地 AudioSource
        /// - 清除暂停标志
        /// - 停止进度条协程并归零
        /// - 恢复 PreviewBtn 显示（隐藏 PauseBtn）
        /// </summary>
        private void StopPreviewAudio()
        {
            AkSoundManager.Inst.StopUGCAudio(gameObject);

            if (_previewAudioSource != null)
            {
                _previewAudioSource.Stop();
            }

            _isPreviewPaused = false;
            StopPreviewProgress();
            SetPreviewButtonState(isPlaying: false);
        }

        private void GoSelectRoot()
        {
            // 返回初始状态前停止正在播放的预览音频
            StopPreviewAudio();
            ShowSubState(SelectRoot);
            CopyStateText.text = string.Empty;
            CopyBtn_Start.gameObject.SetActive(true);
            CopyBtn_Stop.gameObject.SetActive(false);
            _recordSeconds = 0;
            RecordTimeText.text = "0s";
            _isUploadedAudio = false;
        }

        private void OnBtnRecordToneClick()
        {
            ShowSubState(ToCopy);
            CopyStateText.text = string.Empty;
            CopyBtn_Start.gameObject.SetActive(true);
            CopyBtn_Stop.gameObject.SetActive(false);
            _recordSeconds = 0;
            RecordTimeText.text = "0s";
            _isUploadedAudio = false;
        }

        private void OnBtnAddToneClick()
        {
            bool isCancel = false;
            Action onCancel = () => { isCancel = true; };
            UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
            AlbumUtils.Inst.UploadMusic(UploadAudioLimit, (remoteUrl) =>
            {
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);

                if (isCancel || string.IsNullOrEmpty(remoteUrl))
                {
                    return;
                }

                // 校验音频时长：需在 10s–20s 范围内
                if (_uploadedAudioDuration < AudioDurationMin || _uploadedAudioDuration > AudioDurationMax)
                {
                    TipPanel.ShowToast("请保证视频在10-20s以内");
                    return;
                }

                _recordedAudioUrl = remoteUrl;
                _isUploadedAudio = true;
                string durationText = string.Format("{0:00}:{1:00}", (int)_uploadedAudioDuration / 60, (int)_uploadedAudioDuration % 60);
                TimeText.text = durationText;
                ToPublish_TimeText.text = durationText;
                GetOrCreateLanguageState(_currentLanguage).durationText = durationText;
                ShowSubState(Preview);
            }, _ => { UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel); }, len => { _uploadedAudioDuration = len; });
        }

        private void OnCopyBtnStartClick()
        {
            if (DeviceInfoManager.Inst != null
    && DeviceInfoManager.Inst.DeviceBaseData != null
    && DeviceInfoManager.Inst.CheckVersion_1_0_20())
            {

                // 先检查麦克风权限，真机上未授权时 Microphone.devices 返回空数组
                if (!ChatUtils.GetMicrophonePermission())
                {
                    // 未授权时请求系统权限弹窗：
                    // - 用户同意 → open 回调 → 直接开始录音
                    // - 用户拒绝 → OpenConfirm → 跳转系统设置页引导手动开启
                    ChatUtils.ReqMicrophonePermission(
                        () => { },       // ac：跳转设置后无需额外操作
                        DoStartRecording // open：授权成功后开始录音
                    );
                    return;
                }

                DoStartRecording();
            }
            else
            {
                UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, GameData.Base.ForceUpdate.NeedUpdateFeature);
                return;
            }

        }

        /// <summary>
        /// 实际执行录音启动逻辑，调用前需确保麦克风权限已授权。
        /// </summary>
        private void DoStartRecording()
        {
            _recordStartFailed = false;
            CopyStateText.text = "正在录制...";
            CopyBtn_Start.gameObject.SetActive(false);
            CopyBtn_Stop.gameObject.SetActive(true);
            CabinToneNetManager.Inst.BeginRecordVoice(OnRecordVoiceResult);

            if (!_recordStartFailed)
            {
                _recordTimerCoroutine = StartCoroutine(RecordTimerCoroutine());
            }
        }

        private void OnCopyBtnStopClick()
        {
            CabinToneNetManager.Inst.StopRecordVoice();
            StopRecordTimer();
            CopyStateText.text = string.Empty;
            CopyBtn_Start.gameObject.SetActive(true);
            CopyBtn_Stop.gameObject.SetActive(false);
        }

        private void OnRecordVoiceResult(RecordVoiceResult result)
        {
            StopRecordTimer();
            CopyStateText.text = string.Empty;
            CopyBtn_Start.gameObject.SetActive(true);
            CopyBtn_Stop.gameObject.SetActive(false);

            if (result == RecordVoiceResult.Success)
            {
                // 校验录制时长：需在 10s–20s 范围内
                if (_recordSeconds < AudioDurationMin || _recordSeconds > AudioDurationMax)
                {
                    TipPanel.ShowToast("请保证视频在10-20s以内");
                    GoSelectRoot();
                    return;
                }

                string durationText = string.Format("{0:00}:{1:00}", _recordSeconds / 60, _recordSeconds % 60);
                CabinToneNetManager.Inst.UploadVoice(CabinToneNetManager.Inst.recordVoiceFilePath, url =>
                {
                    _recordedAudioUrl = url;
                    _isUploadedAudio = false;
                    TimeText.text = durationText;
                    ToPublish_TimeText.text = durationText;
                    GetOrCreateLanguageState(_currentLanguage).durationText = durationText;
                    ShowSubState(Preview);
                }, _ =>
                {
                    TipPanel.ShowToast("上传失败，请重试");
                    GoSelectRoot();
                });
            }
            else
            {
                _recordStartFailed = true;
                TipPanel.ShowToast("录制音频不符合要求，请重新录制");
            }
        }

        /// <summary>
        /// 播放（或从暂停恢复）由生成音色合成的试听音频，并同步更新按钮状态和进度条。
        /// - 暂停中 → 恢复播放（ResumeUGCAudio），不重新加载音频
        /// - 未播放 → 首次播放，启动进度协程
        /// 若试听音频 URL 尚未就绪，提示用户稍后重试。
        /// </summary>
        private void PlayTonePreviewAudio()
        {
            // ── 从暂停状态恢复播放 ──
            if (_isToPublishPaused)
            {
                _isToPublishPaused = false;
                SetToPublishButtonState(isPlaying: true);
                AkSoundManager.Inst.ResumeUGCAudio(gameObject);
                return;
            }

            // ── 首次播放（或上次已完全停止后重新开始）──
            var state = GetOrCreateLanguageState(_currentLanguage);
            var previewUrl = state.previewAudioUrl;

            if (string.IsNullOrEmpty(previewUrl))
            {
                // URL 尚未就绪：若已有 voiceId 且当前无在途请求，则按需补拉一次，成功后自动播放
                if (!string.IsNullOrEmpty(state.voiceId) && !state.isPreviewRequesting)
                {
                    int lang = _currentLanguage;
                    RequestPreviewAudio(lang, state.voiceId, () =>
                    {
                        // 仅当用户仍停留在该语言的 ToPublish 视图、且未手动暂停时才自动播放，避免跨语言误播
                        if (_currentLanguage == lang && _currentStep == ToneCreationStep.ToPublish && !_isToPublishPaused)
                        {
                            PlayTonePreviewAudio();
                        }
                    });
                }

                TipPanel.ShowToast("正在生成试听音频，请稍后重试");
                return;
            }

            StopToPublishProgress();
            AkSoundManager.Inst.PlayUGCAudioByUrl(previewUrl, false, gameObject);
            // ToPublish 音频始终为 URL 流式音频，使用计时器按时长估算进度
            _toPublishProgressCoroutine = StartCoroutine(UpdateToPublishProgressByTime(state.previewAudioDuration));
            SetToPublishButtonState(isPlaying: true);
        }

        /// <summary>
        /// 暂停正在播放的 ToPublish 试听音频，进度条停止推进，切回 PreviewBtn 显示。
        /// </summary>
        private void PauseToPublishAudio()
        {
            _isToPublishPaused = true;
            SetToPublishButtonState(isPlaying: false);
            AkSoundManager.Inst.PauseUGCAudio(gameObject);
        }

        /// <summary>
        /// 播放（或从暂停恢复）已录制或已上传的预览音频，并同步更新按钮状态和进度条。
        /// - 暂停中 → 恢复播放（UnPause），不重新加载音频
        /// - 未播放 → 首次播放，重新加载并启动进度协程
        /// </summary>
        private void PlayRecordedAudio()
        {
            // ── 从暂停状态恢复播放 ──
            if (_isPreviewPaused)
            {
                _isPreviewPaused = false;
                SetPreviewButtonState(isPlaying: true);

                if (_isUploadedAudio)
                {
                    AkSoundManager.Inst.ResumeUGCAudio(gameObject);
                }
                else
                {
                    if (_previewAudioSource != null)
                    {
                        _previewAudioSource.UnPause();
                    }
                }

                return;
            }

            // ── 首次播放（或上次已完全停止后重新开始）──
            // 先停止上一次的进度更新，确保不会叠加多个协程
            StopPreviewProgress();

            if (_isUploadedAudio)
            {
                if (string.IsNullOrEmpty(_recordedAudioUrl))
                {
                    return;
                }

                AkSoundManager.Inst.PlayUGCAudioByUrl(_recordedAudioUrl, false, gameObject);
                // Wwise URL 流式音频无法读取播放位置，改用计时器按总时长估算进度
                _previewProgressCoroutine = StartCoroutine(UpdatePreviewProgressByTime(_uploadedAudioDuration));
                SetPreviewButtonState(isPlaying: true);
                return;
            }

            CabinToneNetManager.Inst.LoadRecordVoiceClip(clip =>
            {
                if (clip == null)
                {
                    // 加载失败，恢复 PreviewBtn 显示
                    SetPreviewButtonState(isPlaying: false);
                    return;
                }

                if (_previewAudioSource == null)
                {
                    _previewAudioSource = gameObject.GetComponent<AudioSource>();
                    if (_previewAudioSource == null)
                    {
                        _previewAudioSource = gameObject.AddComponent<AudioSource>();
                    }
                }
                _previewAudioSource.clip = clip;
                _previewAudioSource.Play();
                // 本地录音通过 AudioSource.time 精确追踪进度
                _previewProgressCoroutine = StartCoroutine(UpdatePreviewProgressByAudioSource());
            });

            // 加载本地 clip 是异步的，先切到 PauseBtn 给用户即时反馈
            SetPreviewButtonState(isPlaying: true);
        }

        /// <summary>
        /// 暂停正在播放的预览音频，进度条停止推进，切回 PreviewBtn 显示。
        /// </summary>
        private void PauseRecordedAudio()
        {
            _isPreviewPaused = true;
            SetPreviewButtonState(isPlaying: false);

            if (_isUploadedAudio)
            {
                AkSoundManager.Inst.PauseUGCAudio(gameObject);
            }
            else
            {
                if (_previewAudioSource != null)
                {
                    _previewAudioSource.Pause();
                }
            }
        }

        private void OnCreatToneBtnClick()
        {
            if (!CheckCanGenerate())
                return;

            // 进入生成中状态前停止预览音频和进度条，避免协程继续空跑
            StopPreviewAudio();
            ShowSubState(Createing);
            // 进入生成中时立即关闭语言选择下拉，防止用户切换语言
            SetDropdownOpen(false);

            // 收集其他语言已克隆的 voiceId
            var usedVoiceIdList = new List<string>();
            foreach (var kvp in _languageStates)
            {
                if (kvp.Key != _currentLanguage && !string.IsNullOrEmpty(kvp.Value.voiceId))
                {
                    usedVoiceIdList.Add(kvp.Value.voiceId);
                }
            }
            string usedVoiceIds = string.Join(",", usedVoiceIdList);

            CabinToneNetManager.Inst.CloneCabinCharacterTone(_recordedAudioUrl, _currentLanguage, usedVoiceIds, (success, voiceId) =>
            {
                if (success)
                {
                    OnToneCreated(voiceId);
                }
                else
                {
                    TipPanel.ShowToast("生成音色失败，请重试");
                    ShowSubState(Preview);
                }
            });
        }

        // 生成音色接口回包成功后由外部调用
        public void OnToneCreated(string voiceId)
        {
            _voiceId = voiceId;
            ShowSubState(ToPublishGo);
            RefreshCloneCountUI();
            SaveCurrentLanguageState(); // 将 voiceId 同步到 _languageStates 字典，确保发布检查能找到中文音色

            // 音色克隆完成，刷新发布按钮状态
            RefreshPublishBtnState();

            // 用刚生成的音色请求对应语言的试听音频
            RequestPreviewAudio(_currentLanguage, voiceId);
        }

        /// <summary>
        /// 为指定语言/音色请求试听音频。成功后写入该语言状态的 previewAudioUrl 并下载时长，
        /// 通过 onReady 回调通知调用方音频已就绪；失败时记录错误日志，不写入 URL。
        /// 同一语言同时只允许一个在途请求，避免重复点击叠加请求。
        /// </summary>
        /// <param name="language">语言类型（0=中文，1=英文，2=日文）</param>
        /// <param name="voiceId">克隆得到的音色 ID</param>
        /// <param name="onReady">音频 URL 就绪后的回调（可空）</param>
        private void RequestPreviewAudio(int language, string voiceId, Action onReady = null)
        {
            if (string.IsNullOrEmpty(voiceId))
                return;

            var state = GetOrCreateLanguageState(language);

            if (state.isPreviewRequesting)
                return;

            state.isPreviewRequesting = true;
            var previewText = TonePreviewTexts.ContainsKey(language) ? TonePreviewTexts[language] : "";
            CabinToneNetManager.Inst.GetBatchPreviewByVoiceId(voiceId, new List<string> { previewText }, language, (success, data) =>
            {
                state.isPreviewRequesting = false;

                if (success && data?.list != null && data.list.Count > 0 && !string.IsNullOrEmpty(data.list[0].url))
                {
                    var url = data.list[0].url;
                    state.previewAudioUrl = url;
                    // 下载预览音频以获取实际时长，并更新 ToPublish_TimeText
                    StartCoroutine(FetchPreviewAudioDuration(url, language));
                    onReady?.Invoke();
                }
                else
                {
                    LoggerUtils.LogError($"[CreatToneView] 获取语言 {language} 的试听音频失败，voiceId={voiceId}");
                }
            });
        }

        private void OnPublishToneBtnClick()
        {
            // 发布条件检查（按钮置灰时点击不可达，此处作为防御性兜底）
            if (!CanPublish(out string toast))
            {
                TipPanel.ShowToast(toast);
                return;
            }

            // 进入发布视图前停止预览音频
            StopPreviewAudio();
            SaveCurrentLanguageState();

            var languageList = new List<ToneLanguageData>();
            foreach (var kvp in _languageStates)
            {
                if (!string.IsNullOrEmpty(kvp.Value.voiceId))
                {
                    languageList.Add(new ToneLanguageData() { type = kvp.Key, voiceId = kvp.Value.voiceId });
                }
            }

            string metaDataUrl = (_languageStates.ContainsKey(0) && !string.IsNullOrEmpty(_languageStates[0].previewAudioUrl))
                ? _languageStates[0].previewAudioUrl
                : _recordedAudioUrl;

            onGoToPublish?.Invoke(metaDataUrl, languageList);
        }

        /// <summary>
        /// 点击置灰态发布按钮时，弹出提示说明当前无法发布的原因。
        /// </summary>
        private void OnGrayPublishToneBtnClick()
        {
            CanPublish(out string toast);
            TipPanel.ShowToast(toast);
        }

        // ==================== 多语言状态管理 ====================

        private LanguageToneState GetOrCreateLanguageState(int language)
        {
            if (!_languageStates.ContainsKey(language))
            {
                _languageStates[language] = new LanguageToneState();
            }
            return _languageStates[language];
        }

        private void SaveCurrentLanguageState()
        {
            var state = GetOrCreateLanguageState(_currentLanguage);
            state.step = _currentStep;
            state.audioUrl = _recordedAudioUrl;
            state.voiceId = _voiceId;
            state.isUploaded = _isUploadedAudio;
        }

        private void RestoreLanguageState(int language)
        {
            _currentLanguage = language;
            var state = GetOrCreateLanguageState(language);
            _recordedAudioUrl = state.audioUrl;
            _voiceId = state.voiceId;
            _isUploadedAudio = state.isUploaded;

            switch (state.step)
            {
                case ToneCreationStep.ToPublish:
                    ToPublish_TimeText.text = state.durationText;
                    ShowSubState(ToPublishGo);
                    break;
                case ToneCreationStep.Preview:
                // Createing 被中断时，恢复到 Preview
                case ToneCreationStep.Createing:
                    TimeText.text = state.durationText;
                    ShowSubState(Preview);
                    break;
                case ToneCreationStep.ToCopy:
                    GoSelectRoot();
                    break;
                default:
                    ShowSubState(SelectRoot);
                    break;
            }
        }

        // ==================== 发布按钮状态管理 ====================

        /// <summary>
        /// 检查当前是否满足发布条件，并输出对应的提示文案。
        /// 条件一：中文音色（language=0）必须已完成克隆。
        /// 条件二：已勾选添加的英语/日语音色也必须全部完成克隆（或取消勾选/删除）。
        /// </summary>
        /// <param name="toast">不满足条件时输出提示文案；满足时为空字符串。</param>
        /// <returns>所有条件满足时返回 true。</returns>
        private bool CanPublish(out string toast)
        {
            // 中文音色为必填
            if (!_languageStates.ContainsKey(0) || string.IsNullOrEmpty(_languageStates[0].voiceId))
            {
                toast = "必须生成一个中文音色才能发布哦～";
                return false;
            }

            // 已添加（勾选）的英语/日语音色须全部克隆完成
            var incompleteLangs = new List<string>();

            if (EnglishLanguageDown.isOn && (!_languageStates.ContainsKey(1) || string.IsNullOrEmpty(_languageStates[1].voiceId)))
            {
                incompleteLangs.Add("英语");
            }

            if (JapanLanguageDown.isOn && (!_languageStates.ContainsKey(2) || string.IsNullOrEmpty(_languageStates[2].voiceId)))
            {
                incompleteLangs.Add("日语");
            }

            if (incompleteLangs.Count > 0)
            {
                toast = $"删除或克隆完成【{string.Join("、", incompleteLangs)}】音色才可以发布哦～";
                return false;
            }

            toast = "";
            return true;
        }

        /// <summary>
        /// 根据当前语言克隆状态刷新发布按钮的显示状态。
        /// 条件满足时显示亮起态按钮（PublishToneBtn），隐藏置灰态按钮（GrayPublishToneBtn）；反之亦然。
        /// 应在任何可能影响发布条件的状态变更后调用。
        /// </summary>
        private void RefreshPublishBtnState()
        {
            bool canPublish = CanPublish(out _);
            PublishToneBtn.gameObject.SetActive(canPublish);
            GrayPublishToneBtn.gameObject.SetActive(!canPublish);
        }

        /// <summary>根据当前语言更新录制界面的朗读引导文案。</summary>
        private void UpdateRecordScript()
            => SetTextFromLanguageDict(RecordScriptText, RecordScripts);

        /// <summary>根据当前语言更新标题文本（中文音色 / 英语音色 / 日语音色）。</summary>
        private void UpdateTitleText()
            => SetTextFromLanguageDict(TitleText, LanguageTitles);

        private void SwitchToLanguage(int language)
        {
            // 生成音色期间禁止切换语言，防御性兜底
            if (_currentStep == ToneCreationStep.Createing)
                return;

            if (_currentLanguage == language)
            {
                return;
            }

            // 切换语言前停止当前语言的预览音频（Preview 和 ToPublish 均需停止）
            StopPreviewAudio();
            StopToPublishAudio();
            SaveCurrentLanguageState();
            SetDropdownOpen(false);
            RestoreLanguageState(language);
            UpdateLanguageSelectState(language);
            // 切换语言后同步更新录制界面的朗读文案和标题
            UpdateRecordScript();
            UpdateTitleText();
        }

        private void OnLanguageDownBtnClick()
        {
            // 生成音色期间不允许切换语言，弹出提示
            if (_currentStep == ToneCreationStep.Createing)
            {
                TipPanel.ShowToast("音色生成中，请等待生成完成后再切换");
                return;
            }

            SetDropdownOpen(!_isDropdownOpen);
        }

        private void SetDropdownOpen(bool open)
        {
            _isDropdownOpen = open;
            LanguageDownList.SetActive(open);
            LanguageArrow.localEulerAngles = new Vector3(0f, 0f, open ? 180f : 0f);
        }

        private void OnDeleteEnglishBtnClick()
            => OnDeleteLanguageBtnClick(1, EnglishLanguageDown);

        private void OnDeleteJapanBtnClick()
            => OnDeleteLanguageBtnClick(2, JapanLanguageDown);

        private void OnEnglishLanguageDownChanged(bool isOn)
        {
            EnglishItem.gameObject.SetActive(isOn);
            // 语言添加/移除后，重新检查发布条件
            RefreshPublishBtnState();
        }

        private void OnJapanLanguageDownChanged(bool isOn)
        {
            JapanItem.gameObject.SetActive(isOn);
            // 语言添加/移除后，重新检查发布条件
            RefreshPublishBtnState();
        }

        private void UpdateLanguageSelectState(int selectedLanguage)
        {
            ChinaItem.SetSelected(selectedLanguage == 0);
            EnglishItem.SetSelected(selectedLanguage == 1);
            JapanItem.SetSelected(selectedLanguage == 2);
        }

        // ==================== 克隆次数 ====================

        /// <summary>
        /// 刷新右上角克隆次数显示。无月卡且次数为 0 时隐藏整个展示区。
        /// 面板打开时及克隆成功后均需调用。
        /// </summary>
        public void RefreshCloneCountUI()
        {
            int cloneAmount = AccountDataManager.Inst.BalanceInfo.AiCredit.cloneAmount;
            bool isMonthly = AICreditManager.Inst.isCreditMonthCardUser();
            bool hasAccess = cloneAmount > 0 || isMonthly;

            if (cloneCountRoot != null)
                cloneCountRoot.SetActive(hasAccess);

            if (cloneCountText != null)
                cloneCountText.text = $"克隆声音剩余次数：{cloneAmount}次";
        }

        /// <summary>
        /// 生成音色前权限检查：无权限时弹购买面板并返回 false。
        /// </summary>
        private bool CheckCanGenerate()
        {
            int cloneAmount = AccountDataManager.Inst.BalanceInfo.AiCredit.cloneAmount;
            bool isMonthly = AICreditManager.Inst.isCreditMonthCardUser();
            if (cloneAmount <= 0 && !isMonthly)
            {
                OpenCloneShop();
                return false;
            }
            return true;
        }

        /// <summary>打开克隆次数购买面板。</summary>
        private void OpenCloneShop()
        {
            UIManager.Inst.OpenPanel(PanelId.CabinToneCloneTimePanel);
        }

        // ==================== 通用工具方法 ====================

        /// <summary>
        /// 初始化进度条为只读、归零状态。
        /// </summary>
        private static void InitProgressSlider(Slider slider)
        {
            if (slider == null)
                return;

            slider.interactable = false;
            slider.value = 0f;
        }

        /// <summary>
        /// 切换播放/暂停按钮的可见状态（两者互斥显示）。
        /// </summary>
        /// <param name="playBtn">播放按钮</param>
        /// <param name="pauseBtn">暂停按钮</param>
        /// <param name="isPlaying">true = 显示 pauseBtn；false = 显示 playBtn</param>
        private static void SetAudioButtonState(Button playBtn, Button pauseBtn, bool isPlaying)
        {
            if (playBtn != null)
            {
                playBtn.gameObject.SetActive(!isPlaying);
            }

            if (pauseBtn != null)
            {
                pauseBtn.gameObject.SetActive(isPlaying);
            }
        }

        /// <summary>
        /// 停止指定的进度条更新协程，并将进度条重置为起始位置。
        /// </summary>
        private void StopProgressCoroutine(ref Coroutine coroutine, Slider slider)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }

            if (slider != null)
            {
                slider.value = 0f;
            }
        }

        /// <summary>
        /// 根据音频总时长通过计时器推进进度条（适用于 Wwise URL 流式音频）。
        /// 暂停期间不推进计时，已累计时长保留。播放完毕后执行 onFinish 回调。
        /// </summary>
        /// <param name="totalDuration">音频总时长（秒）</param>
        /// <param name="slider">要更新的进度条</param>
        /// <param name="isPaused">返回当前是否处于暂停状态的委托</param>
        /// <param name="onFinish">自然播放结束时的收尾回调（归零进度条 + 恢复按钮）</param>
        private IEnumerator ProgressByTimeCoroutine(float totalDuration, Slider slider, Func<bool> isPaused, Action onFinish)
        {
            if (slider == null)
                yield break;

            if (totalDuration <= 0f)
                yield break;

            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                // 暂停时不推进计时，循环继续等待
                if (!isPaused())
                {
                    elapsed += Time.deltaTime;
                    slider.value = Mathf.Clamp01(elapsed / totalDuration);
                }

                yield return null;
            }

            // 自然播放结束：归零进度条并执行收尾逻辑
            slider.value = 0f;
            onFinish?.Invoke();
        }

        /// <summary>
        /// 从语言字典中取出当前语言对应的文本并写入 UI 组件。
        /// </summary>
        /// <param name="textComp">目标 Text 组件</param>
        /// <param name="dict">语言 key → 文本 value 的字典</param>
        private void SetTextFromLanguageDict(Text textComp, Dictionary<int, string> dict)
        {
            if (textComp == null)
                return;

            if (dict.TryGetValue(_currentLanguage, out var value))
            {
                textComp.text = value;
            }
        }

        /// <summary>
        /// 删除指定语言时的通用处理：若当前正在编辑该语言则切回中文，再取消勾选对应 Toggle。
        /// </summary>
        /// <param name="language">被删除的语言类型（1=英语，2=日语）</param>
        /// <param name="toggle">对应语言的勾选 Toggle</param>
        private void OnDeleteLanguageBtnClick(int language, Toggle toggle)
        {
            if (_currentLanguage == language)
            {
                SwitchToLanguage(0);
            }

            toggle.isOn = false;
        }

        // ==================== 录音计时器 ====================

        private IEnumerator RecordTimerCoroutine()
        {
            _recordSeconds = 0;
            RecordTimeText.text = "0s";
            while (true)
            {
                yield return new WaitForSeconds(1f);
                _recordSeconds++;
                RecordTimeText.text = $"{_recordSeconds}s";
            }
        }

        private void StopRecordTimer()
        {
            if (_recordTimerCoroutine != null)
            {
                StopCoroutine(_recordTimerCoroutine);
                _recordTimerCoroutine = null;
            }
        }

        /// <summary>
        /// 重置录制 UI 状态：停止计时协程、恢复开始按钮、隐藏停止按钮、清零计时文本。
        /// 在面板退出时由父面板调用，确保下次进入时 UI 处于初始状态。
        /// </summary>
        public void ResetRecordingUI()
        {
            StopRecordTimer();
            _recordSeconds = 0;
            CopyStateText.text = "等待录制......";
            CopyBtn_Start.gameObject.SetActive(true);
            CopyBtn_Stop.gameObject.SetActive(false);
            RecordTimeText.text = "0s";
        }

        // ==================== 预览进度条协程 ====================

        /// <summary>
        /// 每帧轮询 AudioSource 的播放位置，将进度实时映射到进度条（适用于本地录音）。
        /// 暂停时（_isPreviewPaused = true）挂起等待，不更新进度也不退出循环。
        /// 播放自然结束后将进度条归零并恢复 PreviewBtn 显示。
        /// </summary>
        private IEnumerator UpdatePreviewProgressByAudioSource()
        {
            if (PreviewProgressSlider == null)
                yield break;

            // isPlaying 在 Pause() 后返回 false，需同时检测暂停标志以保持协程存活
            while (_previewAudioSource != null && (_previewAudioSource.isPlaying || _isPreviewPaused))
            {
                if (!_isPreviewPaused && _previewAudioSource.clip != null && _previewAudioSource.clip.length > 0f)
                {
                    PreviewProgressSlider.value = _previewAudioSource.time / _previewAudioSource.clip.length;
                }

                yield return null;
            }

            // 自然播放结束：归零并恢复 PreviewBtn
            PreviewProgressSlider.value = 0f;
            _previewProgressCoroutine = null;
            SetPreviewButtonState(isPlaying: false);
        }

        /// <summary>Preview URL 音频进度协程（委托 ProgressByTimeCoroutine 实现）。</summary>
        private IEnumerator UpdatePreviewProgressByTime(float totalDuration)
            => ProgressByTimeCoroutine(totalDuration, PreviewProgressSlider,
                () => _isPreviewPaused,
                () => { _previewProgressCoroutine = null; SetPreviewButtonState(false); });

        /// <summary>ToPublish 试听音频进度协程（委托 ProgressByTimeCoroutine 实现）。</summary>
        private IEnumerator UpdateToPublishProgressByTime(float totalDuration)
            => ProgressByTimeCoroutine(totalDuration, ToPublishProgressSlider,
                () => _isToPublishPaused,
                () => { _toPublishProgressCoroutine = null; SetToPublishButtonState(false); });

        /// <summary>
        /// 下载预览音频并获取实际时长，成功后更新对应语言的 durationText 和 ToPublish_TimeText。
        /// 复用 AkSoundManager.GetAudioTypeFromExtension 识别音频格式。
        /// </summary>
        /// <param name="url">GetBatchPreviewByVoiceId 返回的预览音频 URL</param>
        /// <param name="language">对应语言类型（0=中文，1=英文，2=日文）</param>
        private IEnumerator FetchPreviewAudioDuration(string url, int language)
        {
            Uri uri;

            try
            {
                uri = new Uri(url);
            }
            catch
            {
                yield break;
            }

            string extension = Path.GetExtension(uri.LocalPath);
            var audioType = AkSoundManager.Inst.GetAudioTypeFromExtension(extension);
            var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                yield break;

            var clip = DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
                yield break;

            // 将秒数格式化为 mm:ss
            int totalSeconds = (int)clip.length;
            string durationText = string.Format("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);

            // 写入语言状态，切换语言再切回时 RestoreLanguageState 会正确恢复
            var langState = GetOrCreateLanguageState(language);
            langState.durationText = durationText;
            // 同时存储浮点时长，供 ToPublish 进度条协程使用
            langState.previewAudioDuration = clip.length;

            // 如果当前仍在展示该语言的 ToPublish 视图，立即刷新文本
            if (_currentLanguage == language && _currentStep == ToneCreationStep.ToPublish)
            {
                ToPublish_TimeText.text = durationText;
            }
        }
    }
}
