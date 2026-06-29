using System.Collections.Generic;
using FSM;
using View.UI.PopupPanelSystem.Base.Core;
using View.UI.PopupPanelSystem.Base.Utils;

namespace View.UI.PopupPanelSystem.Base.SubSystem
{
    public class PopupSchedulerSystem : BasePopupSystem
    {
        private List<BasePopup> schedules;
        private StateMachine machine;
        private int current;

        public bool IsRunning = false;

        public PopupSchedulerSystem(PopupPanelManager context) : base(context)
        {
            schedules = new List<BasePopup>();
            machine = new StateMachine();
            IsRunning = false;
        }

        public override void Release()
        {
            IsRunning = false;
            current = 0;
            schedules?.Clear();
        }

        public void AddSchedule(BasePopup schedule)
        {
            schedules.Add(schedule);
        }

        public void ClearAllSchedule()
        {
            if (IsRunning) return;
            schedules?.Clear();
        }

        public void Start()
        {
            if (IsRunning)
            {
                LoggerUtils.Log("PopupScheduler start failed ! Scheduler is running!");
                return;
            }

            if (!PopupDataHelper.IsCanRunPopup())
            {
                LoggerUtils.Log("PopupScheduler start failed ! Not in game hall!");
                return;
            }
            
            IsRunning = true;
            current = 0;
            Play(current);
        }

        public void PlayNext()
        {
            current++;
            if (current >= schedules.Count)
            {
                Finish();
            }
            else
            {
                Play(current);
            }
        }

        public void Finish()
        {   
            
            
            machine.ChangeState(null);
            IsRunning = false;
            
        }

        private void Play(int index)
        {
            if (DataUtil.TryGetFromList(schedules, index, out BasePopup next))
            {
                if (next)
                {
                    machine.ChangeState(next);
                }
            }
        }
    }
}