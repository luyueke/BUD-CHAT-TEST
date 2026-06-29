
using System;
using DG.Tweening;
using Game.Utils;
using UnityEngine;

/**
* @ Author: Jun Zhou
* @ Create Time: 2023-09-07 17:12:51
* @ Modified by: Jun Zhou
* @ Modified time: 2023-09-07 17:16:30
* @ Description: 远程Icon加载组件
*/
namespace UI.UIWidgets
{
    public class GameIconLoadItem : GameIconSelectItem
    {
        [Header("远程Icon")]
        [SerializeField]private GameRemoteImageBehaviour remoteIcon;
        [SerializeField]private Transform LoadingImg;

        Tween _loadingTween;

        private bool m_isLoading;
        public bool IsLoading
        {
            get { return m_isLoading; }
        }

        public void InitPool(IPool pool)
        {
            remoteIcon.InitializeWithPool(pool);
        }

        protected override void OnNotifyDestroy()
        {
            base.OnNotifyDestroy();
            _loadingTween?.Kill();
        }

        protected override void OnNotifyAwake()
        {
            base.OnNotifyAwake();
            HideLoading();
        }

        public void SetIcon(string url,Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(url))
            {
                LoggerUtils.LogError($"GameIconLoadItem.LoadIcon error.(url is empty)");
                return;
            }
            ShowLoading(); 
            remoteIcon.Load(url, true, (fromCache,success)=>
            {
                OnLoadComplete(fromCache,success);
                onComplete?.Invoke(success);
            });
        }

        void OnLoadComplete(bool fromCache, bool success)
        {
            var texture = remoteIcon.GetTexture();
            if (success && texture != null)
            {
                var sprite = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), Vector2.zero);
                SetIcon(sprite);
                Icon.gameObject.SetActive(true);
                SetLoadingVisible(false);
            }
            else
            {
                Icon.gameObject.SetActive(false);
                SetLoadingVisible(false);
            }
        }

        public void SetLoadingVisible(bool value)
        {
            m_isLoading = value;
            LoadingImg.gameObject.SetActive(value);
            _loadingTween?.Kill();
            if (value == true)
            {
                _loadingTween = LoadingImg.DOLocalRotate(new Vector3(0, 0, -720), 2f).SetLoops(-1);
            }
        }

        public void ShowLoading()
        {
            Icon.gameObject.SetActive(false);
            SetLoadingVisible(true);
        }

        public void HideLoading()
        {
            Icon.gameObject.SetActive(true);
            SetLoadingVisible(false);
        }
    }
}