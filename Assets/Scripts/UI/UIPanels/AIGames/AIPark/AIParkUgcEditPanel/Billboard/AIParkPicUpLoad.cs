using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public class AIParkPicUpLoad : MonoBehaviour
    {

        [SerializeField] private RemoteImageBehaviour Rm_Cover;
        [SerializeField] private GameObject Go_CoverLoading;
        [SerializeField] private CButton Btn_UploadCover;
        [SerializeField] private GameObject Txt_UpLoad;
        [SerializeField] private CButton Btn_ChooseCover;
        [SerializeField] private CButton Btn_cancelCover;
        [SerializeField] private Animation upLoadAni;

        [SerializeField] private GameObject MapMask;
        [SerializeField] private GameObject MapBg;

        [HideInInspector]public string curUrl;
        public Action<string, int> selectAction;
        public Action<string, int> delAction;
        public Action<string, int> clickAction;
        private int idx;
        private void Awake()
        {
            Btn_UploadCover.onClick.AddListener(OnBtnUploadCoverClick);
            Btn_cancelCover.onClick.AddListener(OnCancelBtnClick);
            Btn_ChooseCover?.onClick.AddListener(OnChooseCoverClick);
        }

        public void InitData(Action<string, int> _action,int _idx, Action<string, int> _delAction, Action<string, int> _clickAction) {
            selectAction = _action;
            idx = _idx;
            delAction = _delAction;
            clickAction = _clickAction;
        }

        public void SetData(string url)
        {
            curUrl = url;
            var validUrl = !string.IsNullOrEmpty(curUrl);
            if (validUrl)
                Rm_Cover.Load(curUrl);
            else
                Rm_Cover.ResetRawImage();
            Btn_ChooseCover?.gameObject.SetActive(validUrl);
            Btn_UploadCover.gameObject.SetActive(!validUrl);
            Txt_UpLoad.SetActive(!validUrl);
            Btn_cancelCover.gameObject.SetActive(validUrl);

            MapMask?.gameObject.SetActive(validUrl);
            MapBg?.gameObject.SetActive(validUrl);
        }

        public void OnCancelBtnClick()
        {
            var tem = curUrl;
            curUrl = string.Empty;
            SetData(curUrl);
            delAction?.Invoke(tem, idx);
        }

        public void OnChooseCoverClick() {
            clickAction?.Invoke(curUrl, idx);
        }

        private void OnBtnUploadCoverClick()
        {
            Btn_UploadCover.gameObject.SetActive(false);
            Txt_UpLoad.SetActive(false);
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
                            SetData(url);
                            selectAction?.Invoke(url,idx);
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