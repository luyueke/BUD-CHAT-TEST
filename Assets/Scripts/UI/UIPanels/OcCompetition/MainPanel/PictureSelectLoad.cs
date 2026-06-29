using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.BaseWidgets;
using UnityEngine;

namespace GameUI
{
    public class PictureSelectLoad : MonoBehaviour
    {
        [SerializeField] private RemoteImageBehaviour Rm_Cover;
        [SerializeField] private CButton Btn_UploadCover;
        [SerializeField] private Animation upLoadAni;

        [HideInInspector] public string curUrl;
        private void Awake()
        {
            Btn_UploadCover.onClick.AddListener(OnBtnUploadCoverClick);
        }

        private void OnEnable()
        {
            Btn_UploadCover.gameObject.SetActive(true);
        }

        public void SetData(string url)
        {
            curUrl = url;
            var validUrl = !string.IsNullOrEmpty(curUrl);
            if (validUrl)
                Rm_Cover.Load(curUrl);
            else
                Rm_Cover.ResetRawImage();
            Btn_UploadCover.gameObject.SetActive(true);
        }

        private void OnBtnUploadCoverClick()
        {
            Btn_UploadCover.gameObject.SetActive(false);
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
            if (authData == null || string.IsNullOrEmpty(authData.localUrl))
            {
                LoggerUtils.Log("authData.localUrl is null ", authData?.localUrl);
                OnFailAction();
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
                OnFailAction();
            }
            else
            {
                LoggerUtils.Log("Upload Image Success url: " + url);
                var req = new Dictionary<string, string>()
                {
                    {"url", url}
                };
                upLoadAni?.Play();
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp =>
                    {
                        OnFailAction();
                        if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                        {
                            SetData(url);
                        }
                        else
                        {
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, failRsp => { OnFailAction(); });
            }
        }

        private void OnFailAction()
        {
            Btn_UploadCover.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            // 清理回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
        }

    }
}