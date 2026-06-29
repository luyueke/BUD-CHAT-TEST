using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class TimeLimitGiftTog : MonoBehaviour
    {
        public Toggle toggle;

        public GameObject gift;

        public TimeLimitGiftType timeLimitGiftType;

        public Text Time;


        BudTimer BudTimer;

        long time;

        private void OnEnable()
        {
            if (timeLimitGiftType != TimeLimitGiftType.None) {
                SetTimer();
            }
        }

        void SetTimer() {
            TimerManager.Inst.Stop(BudTimer);

            var _time = TimeLimitGiftSystem.Inst.GetTriggerTime(timeLimitGiftType);
            if (_time > 0)
            {
                time = _time + 12 * 60 * 60 - TcpTimeSystem.Inst.ServerTime;
                BudTimer = TimerManager.Inst.Run("TimeLimitGiftPanel", 0, 1, OnTimer);
            }
            else
            {
                Time.text = "";
            }
        }

        void OnTimer()
        {
            time -= 1;
            if (time > 0)
            {
                Time.text = $"{TimeTools.SecondsToTimeString((int)time)}";
            }
            else
            {
                Time.text = "";
                TimerManager.Inst.Stop(BudTimer);
            }
        }

        void OnDestroy()
        {
            TimerManager.Inst.Stop(BudTimer);
        }
    }
}