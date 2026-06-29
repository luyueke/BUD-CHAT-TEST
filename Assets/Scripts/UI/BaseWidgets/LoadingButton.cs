using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using DG.Tweening;

namespace UI.BaseWidgets
{
    [AddComponentMenu("BudUI/LoadingButton", 31)]
    public class LoadingButton : CButton
    {
        private Tween _loadingTween;
        private GameObject LoadingNode;
        private Transform LoadingImg;
        private Transform image;
        private bool m_isLoading;
        public bool IsLoading
        {
            get { return m_isLoading; }
        }


        protected override void Awake() {
            base.Awake();
            LoadingNode = this.transform.Find("LoadingNode")?.gameObject;
            LoadingImg = LoadingNode?.transform.Find("LoadingImg");
            image = this.transform.Find("Image");
        }


        protected override void Start()
        {
            base.Start();

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _loadingTween?.Kill();
        }

        public void SetLoadingVisible(bool value)
        {
            interactable = !value;
            m_isLoading = value;
            LoadingNode?.SetActive(value);
            _loadingTween?.Kill();
            if (value == true && LoadingImg!= null)
            {
                _loadingTween = LoadingImg.DOLocalRotate(new Vector3(0, 0, -720), 2f).SetLoops(-1);
            }

            if (ButtonText != null)
            {
                ButtonText.gameObject.SetActive(!value);
            }

            if (image != null)
            {
                image.gameObject.SetActive(!value);
            }
        }

        public void ShowLoading()
        {
            SetLoadingVisible(true);
        }

        public void HideLoading()
        {
            SetLoadingVisible(false);
        }
    }
}
