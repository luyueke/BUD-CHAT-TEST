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
    public class AIParkUgcEditPicUpLoad : MonoBehaviour
    {

        [SerializeField] private RemoteImageBehaviour Rm_Cover;
        [SerializeField] private GameObject Go_CoverLoading;
        [SerializeField] private CButton Btn_UploadCover;
        [SerializeField] private GameObject Txt_UpLoad;
        [SerializeField] private CButton Btn_cancelCover;
        [SerializeField] private Animation upLoadAni;

        private MapInfo curMapInfo;

        private string curUrl;
        private int index;
        public void InitData(MapInfo _mapInfo,int _index)
        {
            Btn_UploadCover.onClick.AddListener(OnBtnUploadCoverClick);
            Btn_cancelCover.onClick.AddListener(OnCancelBtnClick);

            index = _index;
            curMapInfo = _mapInfo;

            SyncEditData();
        }

        private void SyncEditData()
        {
            var urls = curMapInfo.gameSetting.AICommonGameConfig.stage.backgroundUrls;
            if (urls != null && index < urls.Count)
            {
                bool validUrl = !string.IsNullOrEmpty(urls[index]);
                if (validUrl)
                    Rm_Cover.Load(urls[index]);
                else
                    Rm_Cover.ResetRawImage();
                Btn_UploadCover.gameObject.SetActive(!validUrl);
                Txt_UpLoad.SetActive(!validUrl);
                Btn_cancelCover.gameObject.SetActive(validUrl);
            }
        }

        private void OnCancelBtnClick()
        {
            if (!string.IsNullOrEmpty(curUrl))
            {
                var urls = curMapInfo.gameSetting.AICommonGameConfig.stage.backgroundUrls;
                urls[index] = "";
            }
            curUrl = string.Empty;
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
            var path = $"Assets/Arts/UITexture/Cover/Map/{(index % 2 == 0 ? "10008" : "10007")}.png";
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
                            curUrl = url;
                            var urls = curMapInfo.gameSetting.AICommonGameConfig.stage.backgroundUrls;
                            urls[index] = url;
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
        }
    }
}