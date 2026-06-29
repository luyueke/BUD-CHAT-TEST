using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BUD.MailBox
{
    public class MailBoxBaseView : MonoBehaviour
    {
        private bool isFirstShow = true;
        //第一次打开
        protected virtual void Init()
        {
      
        }
        protected virtual void OnViewShow()
        {
      
        }
        protected virtual void OnViewHide()
        {
      
        }
        public void Show()
        {
            if (!IsShow())
            {
                gameObject.SetActive(true);
                if (isFirstShow)
                {
                    isFirstShow = false;
                    Init();
                }
                OnViewShow();
            }
        }
   
        public void Hide()
        {
            if (IsShow())
            {
                OnViewHide();
                gameObject.SetActive(false);
            } 
        }
        public bool IsShow()
        {
            return gameObject.activeSelf;
        }
    }

}
