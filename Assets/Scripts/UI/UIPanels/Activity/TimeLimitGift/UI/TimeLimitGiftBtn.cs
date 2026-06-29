using Message;
using System.Collections;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class TimeLimitGiftBtn : MonoBehaviour
    {
        public CButton Btn;

        public Text Time;

        BudTimer BudTimer;

        long time;
        private void Awake()
        {
            TimeLimitGiftSystem.Inst.limitGiftBtn = this;
            Btn.onClick.AddListener(OnBtn);

            MessageHelper.AddListener(MessageName.TcpTimeUpdate, OnShow);
        }

        void OnShow() 
        {
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            TimerManager.Inst.Stop(BudTimer);

            var _time = TimeLimitGiftSystem.Inst.GetTriggerTime();

            if (_time > 0 && TcpTimeSystem.Inst.IsInit())
            {
                if (IAPDataManager.Inst.productRes == null)
                {
                    IAPDataManager.Inst.GetProductInfo((t) => { OnShow(); });
                    gameObject.SetActive(false);
                    return;
                }
                TimeLimitGiftSystem.Inst.RefreshBtn(null);
                time = _time + 12 * 60 * 60 - TcpTimeSystem.Inst.ServerTime;
                BudTimer = TimerManager.Inst.Run("TimeLimitGiftBtn",0,1,OnTimer);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            TimerManager.Inst.Stop(BudTimer);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.TcpTimeUpdate, OnShow);
        }

        void OnTimer() {
            time -= 1;
            if (time > 0)
            {
                Time.text = TimeTools.SecondsToTimeString((int)time);
            }
            else
            {
                TimerManager.Inst.Stop(BudTimer);
                gameObject.SetActive(false);
            }
        }

        void OnBtn() {
            TimeLimitGiftSystem.Inst.OpenPanel();
        }
    }
}