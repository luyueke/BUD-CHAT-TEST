using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UI.BaseWidgets;

namespace AIGame.Base
{
    public class SettingContentBase : MonoBehaviour
    {
        protected MapInfo curMapInfo;
        protected EditType curEditType;

        public virtual void InitData(MapInfo mapInfo, EditType editType)
        {
            this.curMapInfo = mapInfo;
            this.curEditType = editType;
            
            InitUIComponent();
        }

        public virtual void InitUIComponent()
        {
            
        }

        public virtual void SyncEditData()
        {
            
        }

        public virtual void SaveData()
        {
            
        }
    }

    public class BasicSettingContent : SettingContentBase
    {
        // UI组件引用
        [Header("基础设置组件")] 
        public Toggle Tog_Limited;
        public Toggle Tog_NoLimited;
        public RemoteImageBehaviour Rm_Cover;
        public GameObject Go_CoverLoading;
        public CButton Btn_UploadCover;
        public CButton Btn_InputGameName;
        public Text Txt_MapName;
        public GameObject TimeLimitedContent;
        public CButton Btn_InputGameDur;
        public Text Txt_GameDur;
        public CButton Btn_InputHpLimit;
        public Text Txt_PlayerHp;
        public CButton Btn_InputTarget;
        public Text Txt_Target;
        public List<AIHospitalBgmSelectBtn> BgmSelectBtns;
        public Toggle Tog_ThemeColor1;
        public Toggle Tog_ThemeColor2;
        public Toggle Tog_ThemeColor3;
        
        //KeyboardData
        private KeyBoardInfo _nameKBInfo;
        private KeyBoardInfo _gameDurKBInfo;
        private KeyBoardInfo _playerHPKBInfo;
        private KeyBoardInfo _gameTargetKBInfo;

        private const int MAP_NAME_LIMIT = 25;
        
        // 定义三种主题颜色
        private const string THEME_COLOR_1 = "#404040";
        private const string THEME_COLOR_2 = "#FFFFFF";
        private const string THEME_COLOR_3 = "#FF7700";
        
        public override void InitUIComponent()
        {
            base.InitUIComponent();
            Tog_Limited.onValueChanged.AddListener(OnLimitTimeTogValueChange);
            // Tog_NoLimited.onValueChanged.AddListener(OnLimitTimeTogValueChange);
            
            Btn_UploadCover.onClick.AddListener(OnBtnUploadCoverClick);
            Btn_InputGameName.onClick.AddListener(OnBtnInputGameNameClick);
            Btn_InputGameDur.onClick.AddListener(OnBtnInputGameDurClick);
            Btn_InputHpLimit.onClick.AddListener(OnBtnInputHPClick);
            Btn_InputTarget.onClick.AddListener(OnInputTargetClick);
            
            // 添加主题颜色切换事件
            Tog_ThemeColor1.onValueChanged.AddListener(isOn => { if(isOn) OnThemeColorChanged(THEME_COLOR_1); });
            Tog_ThemeColor2.onValueChanged.AddListener(isOn => { if(isOn) OnThemeColorChanged(THEME_COLOR_2); });
            Tog_ThemeColor3.onValueChanged.AddListener(isOn => { if(isOn) OnThemeColorChanged(THEME_COLOR_3); });

            #region KeyBoardData
            _nameKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入名称",
                inputMode = 0,
                maxLength = MAP_NAME_LIMIT,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
            
            _gameDurKBInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入限时时长"),
                inputMode = 1,
                maxLength = 10,
                inputFlag = 0,
                textSecurity = 1,
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
            
            _playerHPKBInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入玩家血量"),
                inputMode = 1,
                maxLength = 10,
                inputFlag = 0,
                textSecurity = 1,
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
            
            _gameTargetKBInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入说服监管者个数"),
                inputMode = 1,
                maxLength = 2,
                inputFlag = 0,
                textSecurity = 1,
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
            #endregion
        }

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            SyncEditData();
        }

        public override void SyncEditData()
        {
            base.SyncEditData();
            Tog_Limited.isOn = curMapInfo.gameSetting.timeLimited == 1;
            OnLimitTimeTogValueChange(Tog_Limited.isOn);
            Rm_Cover.Load(curMapInfo.cover);
            Txt_MapName.text = curMapInfo.name;
            Txt_GameDur.text = curMapInfo.gameSetting.limitDuration.ToString();
            Txt_PlayerHp.text = curMapInfo.gameSetting.limitHp.ToString();
            Txt_Target.text = curMapInfo.gameSetting.aIGameConfig.winRegulatorAmount.ToString();
            
            //初始化BGM选择
            InitBgmContent();
            
            // 初始化主题颜色
            InitThemeColor();
        }

        private void InitBgmContent()
        {
            var curBgName = curMapInfo.gameSetting.bgName;
            var curBgMusicUrl = curMapInfo.gameSetting.bgMusicUrl;
            
            BgmSelectBtns.ForEach(x =>
            {
                x.SetSelectState(false);
                x.SetData(curBgName, curBgMusicUrl, OnSyncBgmUrl);
            });
            
            if (string.IsNullOrEmpty(curBgMusicUrl))//curBgMusicUrl.Contains(AIHospitalUtils.AIHospital_Offical_Bgm)
            {
                BgmSelectBtns[(int)AIHospitalBgmType.OfficalBgm].SetSelectState(true);
                BgmSelectBtns[(int)AIHospitalBgmType.UploadUgcBgm].gameObject.SetActive(true);
                BgmSelectBtns[(int)AIHospitalBgmType.UploadedUgcBgm].gameObject.SetActive(false);
            }
            else if (!string.IsNullOrEmpty(curBgName) && !string.IsNullOrEmpty(curBgMusicUrl))
            {
                BgmSelectBtns[(int)AIHospitalBgmType.UploadedUgcBgm].SetSelectState(true);
                BgmSelectBtns[(int)AIHospitalBgmType.UploadUgcBgm].gameObject.SetActive(false);
                BgmSelectBtns[(int)AIHospitalBgmType.UploadedUgcBgm].gameObject.SetActive(true);
            }
        }
        
        // 初始化主题颜色
        private void InitThemeColor()
        {
            // 确保aIGameConfig不为空
            if (curMapInfo.gameSetting.aIGameConfig == null)
            {
                curMapInfo.gameSetting.aIGameConfig = new AIGameConfig();
            }
            
            // 如果主题颜色为空，设置默认颜色为#404040
            if (string.IsNullOrEmpty(curMapInfo.gameSetting.aIGameConfig.themeColor))
            {
                curMapInfo.gameSetting.aIGameConfig.themeColor = THEME_COLOR_1;
            }
            
            // 根据当前主题颜色选中对应的Toggle
            string currentColor = curMapInfo.gameSetting.aIGameConfig.themeColor;
            
            Tog_ThemeColor1.isOn = currentColor == THEME_COLOR_1;
            Tog_ThemeColor2.isOn = currentColor == THEME_COLOR_2;
            Tog_ThemeColor3.isOn = currentColor == THEME_COLOR_3;
            
            // 如果没有匹配的颜色，默认选中第一个
            if (!Tog_ThemeColor1.isOn && !Tog_ThemeColor2.isOn && !Tog_ThemeColor3.isOn)
            {
                Tog_ThemeColor1.isOn = true;
                curMapInfo.gameSetting.aIGameConfig.themeColor = THEME_COLOR_1;
            }
        }
        
        // 主题颜色切换回调
        private void OnThemeColorChanged(string colorHex)
        {
            if (curMapInfo?.gameSetting?.aIGameConfig != null)
            {
                curMapInfo.gameSetting.aIGameConfig.themeColor = colorHex;
                LoggerUtils.Log($"Theme color changed to: {colorHex}");
            }
        }

        private void OnLimitTimeTogValueChange(bool isOn)
        {
            if (isOn)
            {
                
            }
            TimeLimitedContent.gameObject.SetActive(isOn);
            curMapInfo.gameSetting.timeLimited = isOn ? 1 : 0;
        }

        private void OnBtnUploadCoverClick()
        {
            OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
            {
                albumType = 1, //0竖屏 1横屏
                // isCrop = 1, //裁剪
                // cropAspectRatio = 1, //宽高比
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUrl);
            MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
        }
        
        private void OnNativeUrl(string msg)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
            if (string.IsNullOrEmpty(authData.localUrl))
            {
                LoggerUtils.LogError("authData.localUrl is null ", authData.localUrl);
                return;
            }
            
            UploadCustomCover(authData.localUrl);
        }
        
        private void UploadCustomCover(string filePath)
        {
            var uri = $"AIGame/AI_Hospital/UGC_MapCover/{AccountDataManager.Inst.Uid}/{Path.GetFileName(filePath)}";
            CosXmlUploadManager.UploadFile(uri, filePath, (url, err) =>
            {
                UploadImgCallback(url, err, filePath);
            });
        }
        
        private void UploadImgCallback(string url, string err, string filePath)
        {
            if (this==null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
                TipPanel.ShowToast("导入图片失败， 请再试一遍!");
            }
            else
            {

                LoggerUtils.Log("Upload Image Success url: " + url);
                var req = new Dictionary<string, string>()
                {
                    {"url", url}
                };
                Go_CoverLoading.SetActive(true);
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp =>
                    {
                        if (rsp != null && rsp.auditResult == (int) AuditResult.Passed)
                        {
                            Go_CoverLoading.SetActive(false);
                            curMapInfo.cover = url;
                            SyncEditData();
                        }
                        else
                        {
                            Go_CoverLoading.SetActive(false);
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, null);
            }
        }
        
        private void OnBtnInputGameNameClick()
        {
            _nameKBInfo.defaultText = curMapInfo.name;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
        }

        private void OnBtnInputGameDurClick()
        {
            _gameDurKBInfo.defaultText = curMapInfo.gameSetting.limitDuration.ToString();
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetGameDurFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_gameDurKBInfo));
        }

        private void OnBtnInputHPClick()
        {
            _playerHPKBInfo.defaultText = curMapInfo.gameSetting.limitHp.ToString();
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPlayerHpFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_playerHPKBInfo));
        }

        private void OnInputTargetClick()
        {
            _gameTargetKBInfo.defaultText = curMapInfo.gameSetting.aIGameConfig.winRegulatorAmount.ToString();
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetGameTargetFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_gameTargetKBInfo));
        }
        
        private void OnGetNameFormNative(string value) {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            curMapInfo.name = value;
            SyncEditData();
        }
        
        private void OnGetGameDurFormNative(string value)
        {
            int gameDur = 900;
            if (int.TryParse(value, out gameDur))
            {
                if (gameDur <= 0)
                {
                    TipPanel.ShowToast("请输入一个大于0的数");
                    return;
                }
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
                curMapInfo.gameSetting.limitDuration = gameDur;
                SyncEditData();
            }
        }
        
        private void OnGetPlayerHpFormNative(string value)
        {
            int playerHp = 3;
            if (int.TryParse(value, out playerHp))
            {
                if (playerHp < 0)
                {
                    TipPanel.ShowToast("请输入一个不小于0的数");
                    return;
                }
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
                curMapInfo.gameSetting.limitHp = playerHp;
                SyncEditData();
            }
        }
        
        private void OnGetGameTargetFormNative(string value)
        {
            int gameTarget = 1;
            if (int.TryParse(value, out gameTarget))
            {
                if (gameTarget <= 0)
                {
                    TipPanel.ShowToast("请输入一个大于0的数");
                    return;
                }
                if (gameTarget > 7)
                {
                    TipPanel.ShowToast("目标数量不能超过7");
                    return;
                }
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
                curMapInfo.gameSetting.aIGameConfig.winRegulatorAmount = gameTarget;
                SyncEditData();
            }
        }
        
        private void OnSyncBgmUrl(string bgName, string bgMusicUrl)
        {
            curMapInfo.gameSetting.bgName = bgName;
            curMapInfo.gameSetting.bgMusicUrl =bgMusicUrl;
            
            SyncEditData();
        }
    }
}