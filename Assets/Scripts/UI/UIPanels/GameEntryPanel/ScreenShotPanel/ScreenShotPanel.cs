using System;
using System.Collections;
using DG.Tweening;
using GameData;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class ScreenShotPanel : BasePanel<ScreenShotPanel>
    {
        public Image BlackImage;

        public RawImage tempRawImage;

        public Action CallBack;
        public override void OnCreate()
        {
            base.OnCreate();
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.saveMediaToLocal, OnSaveSuccess);
            MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.saveMediaToLocal, OnSaveFail);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            StartCoroutine(ShotAnimation());
        }

        private void OnSaveSuccess(string info)
        {
            TipPanel.ShowToast("照片已保存");
        }

        private void OnSaveFail(string info)
        {
            TipPanel.ShowToast("照片保存失败，请重试");
        }

        private IEnumerator ShotAnimation()
        {
            Game.Audio.AkSoundManager.Inst.PostEvent("Play_UI_Screenshot", gameObject);

            BlackImage.DOFade(1, 0.4f).SetEase(Ease.InExpo).onComplete = () =>
            {
                BlackImage.DOFade(0, 0.4f).SetEase(Ease.OutExpo);
            };
            yield return new WaitForEndOfFrame();

            Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenShot.Apply();
            byte[] imageBytes = screenShot.EncodeToPNG();

            tempRawImage.texture = screenShot;
            tempRawImage.CrossFadeAlpha(1, 0, false);

            yield return new WaitForSeconds(1.3f);
            var lastTexture = tempRawImage.texture;
            if (lastTexture)
            {
                Destroy(lastTexture);
            }

            string userId = AccountDataManager.Inst.Uid;
            userId = string.IsNullOrEmpty(userId) ? "shotTemplate" : userId;
            string filePath = LocalDataUtils.Inst.SaveTempImgRes(userId, imageBytes);
            SaveMediaParams data = new SaveMediaParams()
            {
                mediaType = 1,
                mediaUrl = filePath
            };

            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));

            CloseSelf();
            CallBack?.Invoke();
        }

        protected override void OnDestroy()
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.saveMediaToLocal);
            MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.saveMediaToLocal);
            base.OnDestroy();
        }
    }
}