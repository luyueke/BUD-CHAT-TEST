using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine.UI;

namespace GameUI
{
    public class TimeLimitGiftPanel : BasePanel<TimeLimitGiftPanel>
    {
        public Button CloseBtn;

        public List<TimeLimitGiftPackage> limitPackageItems;

        public List<TimeLimitGiftTog> Togs;

        public Text Time;

        BudTimer BudTimer;

        long time;
        public override void OnCreate()
        {
            base.OnCreate();

            CloseBtn.onClick.AddListener(CloseSelf);
            foreach (var item in Togs) 
            {
                item.toggle.onValueChanged.AddListener((bo) => { OnTog(item, bo); });
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            foreach (var tog in Togs)
            {
                tog.gameObject.SetActive(false);
                tog.gift.gameObject.SetActive(false);
            }
            TimerManager.Inst.Stop(BudTimer);

            var ts = TimeLimitGiftSystem.Inst.GetPackageType();
            foreach (var item in ts)
            {
                foreach (var tog in Togs)
                {
                    if (tog.timeLimitGiftType == item.Item1)
                    {
                        tog.gameObject.SetActive(true);
                        tog.gameObject.transform.SetAsFirstSibling();
                    }
                }
            }

            var ls = IAPDataManager.Inst.productRes.popupPackageList;
            foreach (var item in limitPackageItems)
            {
                item.SetData(ls.Find(x => { return x.productInfo.productId.EndsWith(item.productId); }));
            }

            foreach (var tog in Togs)
            {
                if (tog.gameObject.activeSelf && tog.timeLimitGiftType == ts[ts.Count - 1].Item1)
                {
                    tog.toggle.isOn = true;
                }
            }
        }


        protected override void OnDestroy()
        {
            TimerManager.Inst.Stop(BudTimer);
            base.OnDestroy();
        }

        void SetTimer(TimeLimitGiftType t) 
        {
            TimerManager.Inst.Stop(BudTimer);
            var _time = TimeLimitGiftSystem.Inst.GetTriggerTime(t);
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
                Time.text = $"{TimeTools.SecondsToTimeString((int)time)} <color=#FFFFFF>后礼包消失</color>";
            }
            else
            {
                TimerManager.Inst.Stop(BudTimer);
            }
        }


        void OnTog(TimeLimitGiftTog tog,bool bo) 
        {
            if (bo)
            {
                SetTimer(tog.timeLimitGiftType);
                foreach (var item in Togs)
                {
                    item.gift.gameObject.SetActive(item == tog);
                }
            }
        }
    }
}