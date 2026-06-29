using System;
using Game.Audio;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace View.UI.PopupPanelSystem.Base
{
    public class BasePopupPanel<T> : BasePanel<T> where T : BasePopupPanel<T>
    {
        [HideInInspector] public Button CloseBtn;
        [HideInInspector] public Button GoBtn;
        [HideInInspector] public Button BgMask;

        public Action OnClose { get; set; }
        public Action OnFinish { get; set; }

        public override void OnCreate()
        {
            base.OnCreate();
            CloseBtn = GameObjectEx.FindChildByName(this.transform, "CloseBtn").GetComponent<Button>();
            GoBtn = GameObjectEx.FindChildByName(this.transform, "GoBtn")?.GetComponent<Button>();
            BgMask = GameObjectEx.FindChildByName(transform, "BgMask")?.GetComponent<Button>();
            CloseBtn.onClick.AddListener(OnCloseBtnClick);
            GoBtn?.onClick.AddListener(OnGoBtnClick);
            if (BgMask != null)
            {
                BgMask.onClick.AddListener(OnCloseBtnClick);
            }
        }

        protected virtual void HandleGoto()
        {
        }

        protected virtual void HandleClose()
        {
        }

        protected void OnCloseBtnClick()
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_Cancel_C2);
            HandleClose();
            CloseSelf();
            OnClose?.Invoke();
        }

        protected virtual void OnGoBtnClick()
        {
            LoggerUtils.Log("BaseHallForcePopy OnGoBtnClick");
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ConfirmButton_A2);
            HandleGoto();
            CloseSelf();
            OnClose?.Invoke();
        }
    }
}