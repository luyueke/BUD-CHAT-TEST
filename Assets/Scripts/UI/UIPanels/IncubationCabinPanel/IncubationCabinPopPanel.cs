using System;
using System.Collections.Generic;
using UI.Base;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    public enum PopType
    {
        BigWin1,
        BigWin2,
        BigWin3,
        BigWin4,
        SmallWin1,
    }

    public class IncubationCabinPopPanel : BasePanel<IncubationCabinPopPanel>
    {
        [SerializeField] internal GameObject BigWinGo;
        [SerializeField] internal GameObject SmallWinGo;

        [SerializeField] internal GameObject BigWin_win1Go;
        [SerializeField] internal GameObject BigWin_win2Go;
        [SerializeField] internal GameObject BigWin_win3Go;
        [SerializeField] internal GameObject BigWin_win4Go;

        [SerializeField] internal Text txt_bigWinTitle;
        [SerializeField] internal Button btn_bigWinClose;
        [SerializeField] internal Button big_win1_btn1;
        [SerializeField] internal Button big_win1_btn2;
        [SerializeField] internal Button big_win2_backBtn;
        [SerializeField] internal Button big_win2_closeBtn;
        [SerializeField] internal Text big_win2_title;
        [SerializeField] internal Text big_win2_TextLimit;
        [SerializeField] internal TextInputView big_win2_inputView;
        [SerializeField] internal Button big_win2_confirmBtn;
        [SerializeField] internal ScrollRect big_win3_scrollRect;
        [SerializeField] internal GameObject big_win3_itemPrefab;
        [SerializeField] internal Button big_win3_confirmBtn;
        [SerializeField] internal Text big_win4_contentTxt;
        [SerializeField] internal Button big_win4_confirmBtn;
        [SerializeField] internal GameObject big_win4_confirm_state1;
        [SerializeField] internal GameObject big_win4_confirm_state2;

        [Header("BigWin2 Language")]
        [SerializeField] internal Toggle big_win2_chineseTog;
        [SerializeField] internal Toggle big_win2_englishTog;
        [SerializeField] internal Toggle big_win2_japanTog;

        [SerializeField] internal GameObject small_win1Go;
        [SerializeField] internal Button small_win1_btn1;
        [SerializeField] internal Button small_win1_btn2;

        private CabinCharacterBaseInfo characterInfo;
        private string tokenID;
        public Action<string, string> onDoubaoAsrResultCb; //url,text
        public Action<CabinDoubaoBatchPreviewData> onDoubaoBatchPreviewResultCb;

        public Action<RecordVoiceResult, string> onRecordVoiceResultWhenClosePanel;



        RecordVoiceResult recordVoiceResult;

        PopType _popType;

        bool isRecording = false;

        // 当前 win2 语言选择：0=中文，1=英文，2=日文；默认中文
        private int _selectedLanguageType = 0;

        // 音频上传后按日期+序号自动生成展示文件名（如 20260527_0.mp3），每天序号从 0 重置
        private static string _uploadFileNameDate = "";
        private static int _uploadFileNameIndex = 0;

        /// <summary>最近一次上传成功的音频展示文件名（格式：yyyyMMdd_序号.mp3），供父级面板读取展示</summary>
        public string LastUploadedAudioFileName { get; private set; }


        public override void OnShow(params object[] args)
        {
            if (args != null)
            {
                if (args.Length == 2)
                {
                    this.characterInfo = (CabinCharacterBaseInfo)args[0];
                    tokenID = (string)args[1];
                }
            }
            if (this.characterInfo == null)
            {
                return;
            }
            InitUI();
        }

        void InitUI()
        {
            BigWinGo.SetActive(false);
            SmallWinGo.SetActive(false);
            btn_bigWinClose.onClick.AddListener(OnBigWinCloseBtnClick);
        }

        public void SetData(PopType popType, string win2Title = null)
        {
            _popType = popType;
            BigWinGo.SetActive(false);
            BigWin_win1Go.SetActive(false);
            BigWin_win2Go.SetActive(false);
            BigWin_win3Go.SetActive(false);
            BigWin_win4Go.SetActive(false);

            SmallWinGo.SetActive(false);
            switch (_popType)
            {
                case PopType.BigWin1:
                    BigWinGo.SetActive(true);
                    BigWin_win1Go.SetActive(true);
                    big_win1_btn1.onClick.AddListener(OnBigWin1Btn1Click);
                    big_win1_btn2.onClick.AddListener(OnBigWin1Btn2Click);
                    break;
                case PopType.BigWin2:
                    BigWinGo.SetActive(true);
                    BigWin_win2Go.SetActive(true);
                    if (!string.IsNullOrEmpty(win2Title))
                    {
                        big_win2_title.text = win2Title;
                    }
                    big_win2_backBtn.onClick.AddListener(CloseSelf);
                    big_win2_closeBtn.onClick.AddListener(CloseSelf);
                    big_win2_confirmBtn.onClick.AddListener(OnBigWin2ConfirmBtnClick);
                    InitLanguageToggles();
                    break;
                case PopType.BigWin3:
                    BigWinGo.SetActive(true);
                    BigWin_win3Go.SetActive(true);
                    txt_bigWinTitle.text = ""; //TODO: 动作包 - 非循环动画
                    big_win3_confirmBtn.onClick.AddListener(OnBigWin3ConfirmBtnClick);
                    break;
                case PopType.BigWin4:
                    BigWinGo.SetActive(true);
                    BigWin_win4Go.SetActive(true);
                    txt_bigWinTitle.text = "录制声音";
                    big_win4_confirmBtn.onClick.AddListener(OnBigWin4ConfirmBtnClick);
                    big_win4_confirm_state1.SetActive(true);
                    big_win4_confirm_state2.SetActive(false);
                    break;
                case PopType.SmallWin1:
                    SmallWinGo.SetActive(true);
                    small_win1Go.SetActive(true);
                    txt_bigWinTitle.text = "录制声音";
                    small_win1_btn1.onClick.AddListener(OnSmallWin1Btn1Click);
                    small_win1_btn2.onClick.AddListener(OnSmallWin1Btn2Click);
                    break;
                default:
                    break;
            }
        }
        void OnBigWinCloseBtnClick()
        {
            CloseSelf();
            onRecordVoiceResultWhenClosePanel?.Invoke(recordVoiceResult, CabinToneNetManager.Inst.recordVoiceFilePath);
        }

        void OnBigWin1Btn1Click()
        {
            SetData(PopType.BigWin2);
        }

        /// <summary>
        /// 上传本地音频并进行语音转录，根据能否提取文字决定是否走文字审核：
        ///   1. 能提取文字 → 走文字审核，审核未通过则添加失败
        ///   2. 不能提取文字 → 跳过审核，直接通过
        /// 上传成功后自动生成类似剪映的展示文件名：yyyyMMdd_序号.mp3
        /// </summary>
        void OnBigWin1Btn2Click()
        {
            bool isCancel = false;
            Action onCancel = () => { isCancel = true; };
            UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
            AlbumUtils.Inst.UploadMusic(300, (remoteUrl) =>
            {
     

                if (isCancel)
                {
                    return;
                }

                if (string.IsNullOrEmpty(remoteUrl))
                {
                    return;
                }

                // 按当天日期+自增序号生成展示文件名（0 起始，每天重置）
                string today = System.DateTime.Now.ToString("yyyyMMdd");
                if (_uploadFileNameDate != today)
                {
                    _uploadFileNameDate = today;
                    _uploadFileNameIndex = 0;
                }
                LastUploadedAudioFileName = $"{today}_{_uploadFileNameIndex}.mp3";
                _uploadFileNameIndex++;

                // 尝试语音转文字
                CabinNetManager.Inst.AsrCabinCharacterTone(remoteUrl, (isSucc, text) =>
                {
             
                    if(isSucc)
                    {
                        onDoubaoAsrResultCb?.Invoke(remoteUrl, LastUploadedAudioFileName);
                    }
                    UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
                    CloseSelf();
                });
            }, err => { UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel); }, null);
        }
        void OnBigWin2BackBtnClick()
        {
            SetData(PopType.BigWin1);
        }

        /// <summary>
        /// 根据当前音色的 languageList 初始化语言 Toggle 组。
        /// 有对应语言数据的 Toggle 可交互，无数据的置灰不可选；默认选中中文。
        /// </summary>
        void InitLanguageToggles()
        {
            var toneInfo = CabinToneNetManager.Inst.GetToneInfo(tokenID);
            var languageList = toneInfo?.languageList;

            bool hasChinese = languageList != null && languageList.Exists(l => l.type == 0);
            bool hasEnglish = languageList != null && languageList.Exists(l => l.type == 1);
            bool hasJapanese = languageList != null && languageList.Exists(l => l.type == 2);

            // 有对应语言数据才可交互，否则置灰
            big_win2_chineseTog.interactable = hasChinese;
            big_win2_englishTog.interactable = hasEnglish;
            big_win2_japanTog.interactable = hasJapanese;

            // 默认选中中文；若中文不可用，按英文→日文顺序选第一个可用语言
            if (hasChinese)
            {
                _selectedLanguageType = 0;
            }
            else if (hasEnglish)
            {
                _selectedLanguageType = 1;
            }
            else if (hasJapanese)
            {
                _selectedLanguageType = 2;
            }

            // 用 SetIsOnWithoutNotify 设置初始视觉状态，避免触发回调
            big_win2_chineseTog.SetIsOnWithoutNotify(_selectedLanguageType == 0);
            big_win2_englishTog.SetIsOnWithoutNotify(_selectedLanguageType == 1);
            big_win2_japanTog.SetIsOnWithoutNotify(_selectedLanguageType == 2);

            // 注册切换回调，更新 _selectedLanguageType
            big_win2_chineseTog.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    _selectedLanguageType = 0;
                }
            });
            big_win2_englishTog.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    _selectedLanguageType = 1;
                }
            });
            big_win2_japanTog.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    _selectedLanguageType = 2;
                }
            });
        }

        void OnBigWin2ConfirmBtnClick()
        {
            if (characterInfo == null)
                return;

            if (string.IsNullOrEmpty(tokenID))
            {
                TipPanel.ShowToast("请先设置音色");
                return;
            }

            int inputLen = big_win2_inputView.Input?.Length ?? 0;

            if (inputLen < 3 || inputLen > 25)
            {
                TipPanel.ShowToast("字数不符合要求，需在3-25字以内");
                return;
            }

            List<string> texts = new List<string>() { big_win2_inputView.Input };
            // 传入当前选中语言，服务端按对应 voiceId 生成语音包
            CabinNetManager.Inst.GetCabinCharacterToneBatchPreview(tokenID, texts, (isSucc, list) =>
            {
                if (!isSucc)
                {
                    TipPanel.ShowToast("生成语音包失败，请重新输入");
                    return;
                }
                onDoubaoBatchPreviewResultCb?.Invoke(list);
                CloseSelf();
            }, _selectedLanguageType);
        }
        void OnBigWin3ConfirmBtnClick()
        {
        }

        void OnBigWin4ConfirmBtnClick()
        {
            if (!isRecording)
            {
                big_win4_confirm_state1.SetActive(false);
                big_win4_confirm_state2.SetActive(true);
                CabinToneNetManager.Inst.BeginRecordVoice((_recordVoiceResult) =>
                {
                    recordVoiceResult = _recordVoiceResult;
                    if (recordVoiceResult == RecordVoiceResult.Success)
                    {
                        isRecording = true;
                        big_win4_confirm_state1.SetActive(isRecording);
                        big_win4_confirm_state2.SetActive(!isRecording);
                    }
                    else
                    {
                        if (recordVoiceResult == RecordVoiceResult.TimeOut || recordVoiceResult == RecordVoiceResult.Fail)
                        {
                            TipPanel.ShowToast("录制音频不符合要求，请重新录制");
                        }
                        isRecording = false;
                        big_win4_confirm_state1.SetActive(isRecording);
                        big_win4_confirm_state2.SetActive(!isRecording);
                    }

                });
            }
            else
            {
                CabinToneNetManager.Inst.StopRecordVoice();
                CloseSelf();
            }
            isRecording = !isRecording;
            big_win4_confirm_state1.SetActive(isRecording);
            big_win4_confirm_state2.SetActive(!isRecording);
        }

        void OnSmallWin1Btn1Click()
        {
        }
        void OnSmallWin1Btn2Click()
        {
        }
    }
}
