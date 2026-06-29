using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class UGCBaseStateView : MonoBehaviour {
        [SerializeField] protected CButton backBtn;

        [SerializeField] protected CButton nextBtn;


        public UGCPublishState state;

        protected Action nextCallback;
        protected Action backCallback;
        protected UGCBaseEditData editData;

        public virtual void Awake() {
            if (backBtn != null) {
                backBtn.onClick.AddListener(OnBackBtnClick);
            }

            if (nextBtn != null) {
                nextBtn.onClick.AddListener(OnNextBtnClick);
            }


        }

        public virtual void Show() {
            gameObject.SetActive(true);
            SyncEditData();
        }

        public virtual void Hide() {
            gameObject.SetActive(false);
        }


        protected virtual void OnBackBtnClick() {
            backCallback?.Invoke();
        }

        protected virtual void OnNextBtnClick() {
            nextCallback?.Invoke();
        }

        public virtual void SetEditData(UGCBaseEditData data) {
            editData = data;
        }

        public void SetBack(Action callback) {
            backCallback = callback;
        }

        public void SetNext(Action callback) {
            nextCallback = callback;
        }

        public void SetNextEnabled(bool value) {
            nextBtn.SetClickAble(value);
        }

        protected virtual void SyncEditData() {
        }
    }
}
