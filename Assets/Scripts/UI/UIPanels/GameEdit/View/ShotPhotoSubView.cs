using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UI.BaseWidgets;
using Newtonsoft.Json.Linq;
using static GameData.SavingData;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;

namespace UI.UIPanels.GameEdit
{
    public class ShotPhotoSubView : BasePropertyEditSubView
    {
        [Header("业务")]
        [SerializeField]private GameObject emptyGo;
        [SerializeField]private GameObject imgGo;
        [SerializeField]private GameObject loadingGo;
        [SerializeField]private RawImage imgRawImage;
        [SerializeField]private CButton addBtn;
        [SerializeField]private CButton replaceBtn;

        Vector2 originSizeDelta;
        ShotPhotoBehaviour behv;
        ShotPhotoComponent component;

        protected override void OnInit()
        {
            addBtn.onClick.AddListener(OnAddImageClick);
            replaceBtn.onClick.AddListener(OnAddImageClick);
            originSizeDelta = imgRawImage.rectTransform.sizeDelta;
        }

		public override void OnSelectEntity(SceneEntity entity)
		{
			base.OnSelectEntity(entity);
            behv = entity.GetBehaviour<ShotPhotoBehaviour>();
            component = entity.GetComp<ShotPhotoComponent>();
            behv.AddImageLoadListener(OnLoadImage);
            behv.Get(component.PhotoUrl);
		}

        private void OnDestroy()
        {
            behv?.RemoveImageLoadListener(OnLoadImage); 
        }

        void OnAddImageClick()
        {
            LoggerUtils.Log($"ShotPhotoSubView.OnAddImageClick");

            // Test
            // var remoteUrl = "https://cdn.joinbudapp.com/TestFolder/UgcImage/_1644358089.jpg";
            // component.PhotoUrl = remoteUrl;
            // behv.Load(remoteUrl);
            
            AlbumUtils.Inst.UploadAlbum(OnGetMediaFromNative,OnGetMediaFromNativeFail);
        }

        void OnGetMediaFromNative(string remoteUrl)
        {
            if(string.IsNullOrEmpty(remoteUrl))
            {
                return;
            }
      
            component.PhotoUrl = remoteUrl;
            // 加载图片
            behv.Load(remoteUrl);
        }

        void OnLoadImage(ShotPhotoLoadState state, Texture texture)
        {
            if (state == ShotPhotoLoadState.Loading){
                ShowLoading();
            } else if (state == ShotPhotoLoadState.Success) {
                ShowImage(texture);
            } else if (state == ShotPhotoLoadState.Fail || state == ShotPhotoLoadState.Empty) {
                ShowEmpty();
            }
        }

        void OnGetMediaFromNativeFail(string msg)
        {
            LoggerUtils.Log($"ShotPhotoSubView.OnGetMediaFromNativeFail {msg}");
            ShowEmpty();
        }

        void ShowLoading()
        {
            emptyGo.SetActive(false);
            imgGo.SetActive(false);
            loadingGo.SetActive(true);
        }

        void ShowImage(Texture texture)
        {
            emptyGo.SetActive(false);
            imgGo.SetActive(true);
            loadingGo.SetActive(false);
            imgRawImage.texture = texture;
            // 刷新一下图片大小
            var aspectRatio = texture.width / texture.height;
            var nSizeDelta = originSizeDelta;
            nSizeDelta.x = originSizeDelta.y * aspectRatio;
            imgRawImage.rectTransform.sizeDelta = nSizeDelta;
        }

        void ShowEmpty()
        {
            emptyGo.SetActive(true);
            imgGo.SetActive(false);
            loadingGo.SetActive(false);
        }
    }
}