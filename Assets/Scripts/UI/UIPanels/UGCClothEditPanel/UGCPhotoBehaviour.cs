using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.COSXML;
using Game.Base;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public enum SavePhotoType
    {
        CheckInPhoto = 0,
        SystemPhoto
    }

    public enum TexLoadState
    {
        None,
        Loading,
        Error,
        Success
    }

    public class AlbumResData
    {
        /// <summary>
        ///  本地图片绝对路径
        /// </summary>
        public string localUrl;

        public int mediaType; //  0 视频  1 图片
    }

    public class UGCPhotoBehaviour : ElementBaseBehaviour
    {
        public PhotoData data;
        public RawImage self;
        private Coroutine loadCor;
        public SavePhotoType photoType;
        public Animation loader;
        private Transform mirParent;
        public UGCPhotoBehaviour photoDynamicMir;
        public Transform failImage;
        public UGCPhotoSelectPanel selectPhotoPanel;
        public BoxCollider selfCollider;
        public Texture defaultImage;
        public bool isLoadPhoto;
        public Action<UGCPhotoBehaviour> OnSelectAction;
        public Action OnCopyAction;
        public TexLoadState loadState = TexLoadState.None;
        public Action OnDestoryBehaviour;
        private UGCRemoteLoader ugcRawRemote;

        public override void OnCreate(Transform mirrorParent)
        {
            base.OnCreate(mirrorParent);
            self = this.GetComponent<RawImage>();
            rectTrans = self.rectTransform;
            loader = this.GetComponentInChildren<Animation>();
            loader.gameObject.SetActive(false);
            failImage = this.transform.Find("loadFail");
            failImage.gameObject.SetActive(false);

            defaultImage = self.texture;

            rectTrans.localPosition = Vector2.zero;
            rectTrans.localScale = Vector3.one;
            rectTrans.sizeDelta = new Vector2(minSize, minSize / scalingRatio);
            rectTrans.localEulerAngles = Vector3.zero;

            gameObject.SetActive(true);

            selfCollider = gameObject.AddComponent<BoxCollider>();
            selfCollider.size = new Vector3(rectTrans.sizeDelta.x, rectTrans.sizeDelta.y, 0);


            photoDynamicMir = CreatMirrorPhoto(self, mirrorParent);
            photoDynamicMir.self.enabled = false;
            mirParent = mirrorParent;

            ugcRawRemote = this.GetComponent<UGCRemoteLoader>();
            this.gameObject.SetActive(true);
            data = new PhotoData();
            SetSelfData();
            OnTransformChange();
            AddClickEvent();
        }

        public void SetPool(IPool pool)
        {
            ugcRawRemote.InitializeWithPool(pool);
        }

        public string GetOffetPosition()
        {
            return FormatUtils.Vector3ToString(rectTrans.localPosition + copyOffset);
        }

        public override void SetActive(bool isActive)
        {
            base.SetActive(isActive);
            this.gameObject.SetActive(isActive);
            photoDynamicMir.gameObject.SetActive(isActive);
            photoDynamicMir.self.enabled = self.mainTexture != null && self.mainTexture != defaultImage;
        }

        public UGCPhotoBehaviour CreatMirrorPhoto(RawImage photoGo, Transform par)
        {
            var mirrorPhoto = GameObject.Instantiate(photoGo, par);
            mirrorPhoto.rectTransform.localPosition = photoGo.rectTransform.localPosition;
            mirrorPhoto.rectTransform.localScale = photoGo.rectTransform.localScale;
            mirrorPhoto.rectTransform.sizeDelta = photoGo.rectTransform.sizeDelta;
            mirrorPhoto.rectTransform.localEulerAngles = photoGo.rectTransform.localEulerAngles;
            var behav = mirrorPhoto.GetComponent<UGCPhotoBehaviour>();
            behav.loader.gameObject.SetActive(false);
            return behav;
        }



        public override void SetData(UGCImportData pData)
        {
            data = pData as PhotoData;
            rectTrans.localPosition = FormatUtils.StringToVector3(data.pos);
            rectTrans.sizeDelta = FormatUtils.StringToVector2(data.sizeDelta);
            rectTrans.localEulerAngles = FormatUtils.StringToVector3(data.rot);
            rectTrans.localScale = Vector3.one;
            hierarchy = data.hierarchy;
            OpenLoader();
            LoadPhoto(false);
            OnTransformChange();
        }

        public override void OnCopy()
        {
            base.OnCopy();
            OnCopyAction?.Invoke();
        }

        public override void SetSelfData()
        {
            data.pos = FormatUtils.Vector3ToString(transform.localPosition);
            data.sizeDelta = FormatUtils.Vector2ToString(self.rectTransform.sizeDelta);
            data.rot = FormatUtils.Vector3ToString(transform.localEulerAngles);
            data.hierarchy = hierarchy;
        }

        public override void Select()
        {
            base.Select();

            if (!isLoadPhoto)
            {
                OnSelectAction?.Invoke(this);
            }
        }


        #region 加载图片

        public void LoadPhoto(bool isInit = false)
        {
            if (string.IsNullOrEmpty(data.photoUrl))
            {
                return;
            }
            failImage.gameObject.SetActive(false);
            //TODO:初始化需要同步，其他操作异步
            if (isInit)
            {
                var tex = Loader.LoadRemoteImage(data.photoUrl, this.gameObject);
                if (tex)
                {
                    loadState = TexLoadState.Success;
                    isLoadPhoto = true;
                    self.texture = tex;
                    SetMirTexture(tex);
                    CloseLoader();
                }
                else
                {
                    loadState = TexLoadState.Error;
                    isLoadPhoto = false;
                    CloseLoader();
                    failImage.gameObject.SetActive(true);
                }
            }
            else
            {
                var wrapper = Loader.LoadRemoteImageAsync(data.photoUrl);
                wrapper.completed += success =>
                {
                    if (success)
                    {
                        var tex = wrapper.RetainAsset(this.gameObject);
                        loadState = TexLoadState.Success;
                        isLoadPhoto = true;
                        self.texture = tex;
                        SetMirTexture(tex);
                        CloseLoader();
                    }
                    else
                    {
                        loadState = TexLoadState.Error;
                        isLoadPhoto = false;
                        CloseLoader();
                        failImage.gameObject.SetActive(true);
                    }
                };
            }
        }

        public override void OnClick()
        {
            base.OnClick();
            UGCImportPhotoManager.Inst.CurrentSelectBehaviour = this;
        }

        private void SetMirTexture(Texture tex)
        {
            rectTrans.sizeDelta = AutoSize(tex);
            selfCollider.size = rectTrans.sizeDelta;
            if (photoDynamicMir != null)
            {
                photoDynamicMir.self.texture = tex;
                photoDynamicMir.self.enabled = true;
            }

            OnTransformChange();
            SetSelfData();
            TransformInteractorController.Inst.InterActor.RefreshTransfrom(rectTrans);
        }

        private Vector2 AutoSize(Texture tex)
        {
            float newRatio = (float) tex.height / tex.width;
            float newH = rectTrans.sizeDelta.x * newRatio;
            return new Vector2(rectTrans.sizeDelta.x, newH);
        }

        public void OpenLoader()
        {
            loader.gameObject.SetActive(true);
            loader.Play();
            loadState = TexLoadState.Loading;
        }

        private void CloseLoader()
        {
            loader.Stop();
            loader.gameObject.SetActive(false);
        }

        #endregion


        public override void OnTransformChange()
        {
            if (photoDynamicMir)
            {
                Copy(self, photoDynamicMir.self);
            }
        }

        private void Copy(RawImage org, RawImage mir)
        {
            mir.rectTransform.localPosition = org.rectTransform.localPosition;
            mir.rectTransform.localScale = org.rectTransform.localScale;
            mir.rectTransform.sizeDelta = org.rectTransform.sizeDelta;
            mir.rectTransform.localEulerAngles = org.rectTransform.localEulerAngles;
            mir.texture = org.texture;
        }

        public override GameObject GetDynamicMir()
        {
            if (photoDynamicMir)
            {
                return photoDynamicMir.gameObject;
            }

            return null;
        }

        public override void OnDes()
        {
            base.OnDes();
            OnDestoryBehaviour?.Invoke();
        }



        public override GameObject ExcuteMirObj()
        {
            if (photoDynamicMir)
            {
                return photoDynamicMir.gameObject;
            }

            return null;
        }

        public override void SetMirSiblingIndex()
        {
            base.SetMirSiblingIndex();
            if (photoDynamicMir != null)
            {
                photoDynamicMir.rectTrans.SetSiblingIndex(rectTrans.GetSiblingIndex());
            }
        }

        public override void CreatUndoData(ElementUndoData data)
        {
            base.CreatUndoData(data);
            data.tex = self.texture;
            data.url = this.data.photoUrl;
        }

        public override void RedoInfo(int partIndex)
        {
            UGCImportPhotoManager.Inst.AddElement(partIndex, this);
            UGCImportPhotoManager.Inst.CurrentSelectBehaviour = this;
        }

        public override void UndoInfo(int partIndex)
        {
            UGCImportPhotoManager.Inst.RemoveElement(partIndex, this);
        }

        public void OpenPhotoPanel()
        {
            // selectPhotoPanel.gameObject.SetActive(true);
            // selectPhotoPanel.ShowPanel(behaviour);
            //
            OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
            {
                albumType = 1, //0竖屏 1横屏
                isCrop = 0, //裁剪
                cropAspectRatio = 1, //宽高比
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUri);
            MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
#if UNITY_EDITOR
            var filePath = Path.Combine(Application.streamingAssetsPath, "signIn_bg.png");
            AlbumResData resData = new AlbumResData();
            resData.localUrl = filePath;
            OnNativeUri(JsonConvert.SerializeObject(resData));
#endif

        }



        private void OnNativeUri(string msg)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
            AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
            if (string.IsNullOrEmpty(authData.localUrl))
            {
                LoggerUtils.LogError("authData.localUrl is null ", authData.localUrl);
                return;
            }

            UploadImg(authData.localUrl);
        }

        private void UploadImg(string filePath)
        {
            OpenLoader();
            var uri = $"AvatarPartTemplate/{AccountDataManager.Inst.Uid}/{Path.GetFileName(filePath)}";
            CosXmlUploadManager.UploadFile(uri, filePath, (url, err) => { UploadImgCallback(url, err, filePath); });
        }

        private void UploadImgCallback(string url, string err, string filePath)
        {
            // if (File.Exists(filePath))
            // {
            //     File.Delete(filePath);
            // }
            if (!string.IsNullOrEmpty(err))
            {
                CloseLoader();
                LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
                TipPanel.ShowToast("导入图片失败， 请再试一遍!");
            }
            else
            {

                LoggerUtils.Log("Upload Image Success url: " + url);
                var req = new Dictionary<string, string>() {
                    { "url", url }
                };
                NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                    HttpMethod.POST, req, rsp => {
                        if (rsp != null && rsp.auditResult == (int) AuditResult.Passed)
                        {
                            data.photoUrl = url;
                            LoadPhoto();
                        }
                        else
                        {
                            TipPanel.ShowToast("图片审核未通过，请重新上传!");
                        }
                    }, null);

            }

        }


        public void OnDestroy()
        {
            // UGCImportPhotoManager.Inst.RemoveElement(this);
        }
    }
}
