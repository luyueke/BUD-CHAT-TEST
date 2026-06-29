using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.BaseInfo;
using Network.Http;
using Network;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using GameData.Base;
using System.IO;

namespace AIGame.Base
{
    public enum ELocation
    {
        ConsultationRoom = 0,
        Sickroom,
        WaitingRoom,
        Corridor,
    }
    public class AIGamePicUpLoadBtn : MonoBehaviour
    {
        // Start is called before the first frame update
        [Header("归属区域")]
        [SerializeField] private ELocation _location;
        [Header("第几个图")]
        [SerializeField] private int _index;

        [SerializeField] private RemoteImageBehaviour Rm_Cover;
        [SerializeField] private GameObject Go_CoverLoading;
        [SerializeField] private CButton Btn_UploadCover;
        [SerializeField] private GameObject Txt_UpLoad;
        [SerializeField] private CButton Btn_cancelCover;
        [SerializeField] private Animation upLoadAni;

        private MapInfo _curMapInfo;
        private HospitalPhotoData _curData;

        public void InitData(MapInfo mapInfo)
        {
            Btn_UploadCover.onClick.AddListener(OnBtnUploadCoverClick);
            Btn_cancelCover.onClick.AddListener(OnCancelBtnClick);
            if (mapInfo == null)
            {
                LoggerUtils.LogError("MapInfo is null");
                return;
            }

            if (mapInfo.gameSetting?.aIGameConfig?.hospitalPhotos == null)
            {
                LoggerUtils.LogError("hospitalPhotos is null");
                return;
            }

            var locationPhotos = mapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_location];
            if (locationPhotos == null)
            {
                locationPhotos = new HospitalPhotoData { urls = new List<string>() };
                mapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_location] = locationPhotos;
            }

            while (locationPhotos.urls.Count <= _index)
            {
                locationPhotos.urls.Add(string.Empty);
            }

            _curMapInfo = mapInfo;
        }

        public void InitData(ELocation location, int index) 
        {
            _location = location;
            _index = index;
            SyncEditData();
        }

        private void SyncEditData()
        {
            if (_curMapInfo?.gameSetting?.aIGameConfig?.hospitalPhotos == null) return;
            
            var photos = _curMapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_location];
            if (photos?.urls != null && _index < photos.urls.Count)
            {
                _curData = photos;
                bool validUrl = !string.IsNullOrEmpty(photos.urls[_index]);
                if (validUrl)
                    Rm_Cover.Load(photos.urls[_index]);
                else
                    Rm_Cover.ResetRawImage();
                Btn_UploadCover.gameObject.SetActive(!validUrl);
                Txt_UpLoad.SetActive(!validUrl);
                Btn_cancelCover.gameObject.SetActive(validUrl);
            }
        }

        private void OnCancelBtnClick()
        {
            if (_curData != null)
            {
                _curData.urls[_index] = string.Empty;
            }
            Rm_Cover.ResetRawImage();
            Btn_UploadCover.gameObject.SetActive(true);
            Txt_UpLoad.SetActive(true);
            Btn_cancelCover.gameObject.SetActive(false);
        }


        private void OnBtnUploadCoverClick()
        {
            Btn_UploadCover.gameObject.SetActive(false);
            Txt_UpLoad.SetActive(false);
#if UNITY_EDITOR
            //todo 在unity编辑器下，直接使用本地图片上传测试
            var path = $"Assets/Arts/UITexture/Cover/Map/{(_index%2==0?"100012": "100011")}.png";
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
            if (authData == null || string.IsNullOrEmpty(authData.localUrl))
            {
                LoggerUtils.LogError("authData.localUrl is null ", authData?.localUrl);
                OnCancelBtnClick();
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
                Btn_UploadCover.gameObject.SetActive(true);
                return;
            }
            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
                TipPanel.ShowToast("导入图片失败， 请再试一遍!");
                Btn_UploadCover.gameObject.SetActive(true);
            }
            else
            {

                LoggerUtils.Log("Upload Image Success url: " + url);
                var req = new Dictionary<string, string>()
                {
                    {"url", url}
                };
                Go_CoverLoading.SetActive(true);
                upLoadAni?.Play();
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp =>
                    {
                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                        {
                            Go_CoverLoading.SetActive(false);
                            _curMapInfo.gameSetting.aIGameConfig.hospitalPhotos[(int)_location].urls[_index] = url;
                            SyncEditData();
                        }
                        else
                        {
                            OnFailAction();
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, failRsp => { OnFailAction(); });
            }
        }

        private void OnFailAction()
        {
            Btn_UploadCover.gameObject.SetActive(true);
            Go_CoverLoading.SetActive(false);
        }

        private void OnDestroy()
        {
            // 清理回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            
            // 清理引用
            _curMapInfo = null;
            Btn_UploadCover.onClick.RemoveListener(OnBtnUploadCoverClick);
        }
    }
}