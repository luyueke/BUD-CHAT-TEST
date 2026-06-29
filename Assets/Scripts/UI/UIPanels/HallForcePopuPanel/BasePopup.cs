using FSM;
using System;
using UnityEngine;
using View.UI.PopupPanelSystem.Data;

namespace View.UI.PopupPanelSystem.Base.Core
{
    public abstract class BasePopup : State
    {
        public PopupState CurState;
        public Action CloseCallBack;
        protected PopupPanelManager Context;

        protected BasePopup(PopupPanelManager context)
        {
            Context = context;
            CurState = GetState();
        }

        protected BasePopup()
        {

        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (IsCanPop())
            {
                OnPlayPopup();
            }
            else
            {
                PlayNext();
            }
        }

        protected virtual void OnPlayPopup()
        {
        }

        protected virtual void PlayNext()
        {
            Context.SchedulerSystem.PlayNext();
        }

        #region Panel Listeners

        public virtual void OnClose()
        {
            SaveState((int)PopupState.Closed + string.Empty);
            CurState = PopupState.Closed;
            CloseCallBack?.Invoke();
            PlayNext();
        }

        public virtual void OnFinish()
        {
            SaveState((int)PopupState.Finish + string.Empty);
            CurState = PopupState.Finish;
            CloseCallBack?.Invoke();
            PlayNext();
        }

        #endregion

        #region State

        protected abstract string CreateKey();

        protected virtual void SaveState(string value)
        {
            var key = CreateKey();
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            
            if (string.IsNullOrEmpty(value))
            {
                // Log.E("SaveState error , value is null");
                return;
            }

            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        protected virtual PopupState GetState()
        {
            var key = CreateKey();
            if (string.IsNullOrEmpty(key))
            {
                return PopupState.Show;
            }
            
            var str = PlayerPrefs.GetString(key, string.Empty);
            if (int.TryParse(str, out var intV))
            {
                return (PopupState)intV;
            }

            return PopupState.Show;
        }

        /// <summary>
        /// 默认未完成都要弹，直接关闭下次仍然弹出
        /// </summary>
        public virtual bool IsCanPop()
        {
            return (int)GetState() < (int)PopupState.Finish;
        }

        #endregion
    }
}