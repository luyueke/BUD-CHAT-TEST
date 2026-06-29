using System;
using Com.TheFallenGames.OSA.Util.IO;
using System.IO;
using Game.COSXML;
using GameData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{

    /// <summary>
    /// 修改头像页面
    /// </summary>
    public class ChangeHeadImgPanel : BasePanel<ChangeHeadImgPanel>
    {
        [SerializeField] private CButton BackBtn;
        [SerializeField] private LoadingButton ChangePhotoBtn;
        [SerializeField] private RemoteImageBehaviour HeadImg;
        private AccountUserInfo _accountUserInfo;
        private Action onSetSuccess;
        private Action onSetFail;

        public override void OnCreate()
        {
            ChangePhotoBtn.onClick.AddListener(OnChangePhotoBtnClick);
            BackBtn.onClick.AddListener(OnBackBtnClick);

        }

        private void OnChangePhotoBtnClick()
        {
            ChangePhotoBtn.SetLoadingVisible(true);
            AlbumUtils.Inst.UploadHead((url) =>
            {
                AccountDataManager.Inst.SendSetHeadImgReqeust(url,OnChageHeadImgCallback);
            }, (error) =>
            {
                LoggerUtils.LogError(error);
                TipPanel.ShowToast(error);
                ChangePhotoBtn.SetLoadingVisible(false);
            });
        }


        private void OnChageHeadImgCallback(bool isSuccess)
        {
            if (isSuccess)
            {
                this.onSetSuccess?.Invoke();
                CloseSelf();
            }
            else
            {
                ChangePhotoBtn.SetLoadingVisible(false);
                this.onSetFail?.Invoke();
            }
        }

        private void OnBackBtnClick()
        {
            CloseSelf();
        }

        private void CloseSelf()
        {
            UIManager.Inst.ClosePanel(this);
        }

        public override void OnShow(params object[] args)
        {
            _accountUserInfo = args[0] as AccountUserInfo;

            if (_accountUserInfo == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_accountUserInfo.portraitUrl))
            {
                HeadImg.Load(_accountUserInfo.portraitUrl);
            }
            ChangePhotoBtn.SetLoadingVisible(false);
        }

        public void SetAction(Action onSuccess = null, Action onFail = null)
        {
            this.onSetSuccess = onSuccess;
            this.onSetFail = onFail;
        }
    }
}
