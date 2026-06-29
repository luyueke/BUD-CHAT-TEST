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
    public class AIParkUgcEditBasic : SettingContentBase
    {
        // UI组件引用
        [Header("基础设置组件")]
        public RemoteImageBehaviour Rm_Cover;
        public GameObject Def_Cover;
        public GameObject Go_CoverLoading;
        public CButton Btn_UploadCover;
        public CButton Btn_InputGameName;
        public InputField Txt_MapName;
        public List<AIParkBgmSelectBtn> BgmSelectBtns;

        //KeyboardData
        private KeyBoardInfo _nameKBInfo;

        private const int MAP_NAME_LIMIT = 25;
        public override void InitUIComponent()
        {
            base.InitUIComponent();

            Btn_UploadCover.onClick.AddListener(OnBtnUploadCoverClick);
            Btn_InputGameName.onClick.AddListener(OnBtnInputGameNameClick);

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
        }

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            SyncEditData();
        }

        public override void SyncEditData()
        {
            base.SyncEditData();
            if (string.IsNullOrEmpty(curMapInfo.cover))
            {
                Rm_Cover.gameObject.SetActive(false);
                Def_Cover.gameObject.SetActive(true);
            }
            else
            {
                Rm_Cover.gameObject.SetActive(true);
                Def_Cover.gameObject.SetActive(false);
                Rm_Cover.Load(curMapInfo.cover);
            }
       
            Txt_MapName.text = curMapInfo.name;

            //初始化BGM选择
            InitBgmContent();
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

            if (string.IsNullOrEmpty(curBgMusicUrl) && string.IsNullOrEmpty(curBgName))//curBgMusicUrl.Contains(AIHospitalUtils.AIHospital_Offical_Bgm)
            {
                foreach (var item in BgmSelectBtns)
                {
                    switch (item._curType)
                    {
                        case AIHospitalBgmType.OfficalBgm:
                        case AIHospitalBgmType.UploadUgcBgm:
                            item.gameObject.SetActive(true);
                            break;
                        case AIHospitalBgmType.UploadedUgcBgm:
                            item.gameObject.SetActive(false);
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(curBgName) && string.IsNullOrEmpty(curBgMusicUrl))
            {
                foreach (var item in BgmSelectBtns)
                {
                    switch (item._curType)
                    {
                        case AIHospitalBgmType.OfficalBgm:
                        case AIHospitalBgmType.UploadUgcBgm:
                            item.gameObject.SetActive(true);
                            item.SetSelectState(curBgName == item.OfficalName);
                            break;
                        case AIHospitalBgmType.UploadedUgcBgm:
                            item.gameObject.SetActive(false);
                            item.SetSelectState(false);
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                foreach (var item in BgmSelectBtns)
                {
                    item.gameObject.SetActive(true);
                    switch (item._curType)
                    {
                        case AIHospitalBgmType.OfficalBgm:
                        case AIHospitalBgmType.UploadUgcBgm:
                            item.SetSelectState(false);
                            break;
                        case AIHospitalBgmType.UploadedUgcBgm:
                            item.SetSelectState(true);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void OnBtnUploadCoverClick()
        {
#if UNITY_EDITOR
            //todo 在unity编辑器下，直接使用本地图片上传测试
            var path = $"Assets/Arts/UITexture/Cover/Map/{"10008"}.png";
            UploadCustomCover(path);
            return;
#endif
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
            if (this == null)
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
                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
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

        private void OnGetNameFormNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            curMapInfo.name = value;
            SyncEditData();
        }

        private void OnSyncBgmUrl(string bgName, string bgMusicUrl, AIParkBgmSelectBtn selectBtn)
        {
            curMapInfo.gameSetting.bgName = bgName;
            curMapInfo.gameSetting.bgMusicUrl = bgMusicUrl;

            SyncEditData();
        }
    }
}