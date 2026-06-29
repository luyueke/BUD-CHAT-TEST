using View.UI.PopupPanelSystem.Base.SubSystem;
using View.UI.PopupPanelSystem.Data;

namespace View.UI.PopupPanelSystem
{
    public class PopupPanelManager : GlobalInstance<PopupPanelManager>
    {
        public PopupDataSystem DataSystem;
        public PopupCreateSystem CreateSystem;
        public PopupSchedulerSystem SchedulerSystem; //连续展示的弹窗调度
        public PopupImagePreLoaderSystem ImagePreLoaderSystem; //图片预加载缓存
        

        public PopupPanelManager()
        {
            InitSystems();
        }

        public override void Release()
        {
            ReleaseSystems();
            base.Release();
        }

        #region Systems

        private void InitSystems()
        {
            DataSystem = new PopupDataSystem(this);
            CreateSystem = new PopupCreateSystem(this);
            SchedulerSystem = new PopupSchedulerSystem(this);
            ImagePreLoaderSystem = new PopupImagePreLoaderSystem(this);
        }

        private void ReleaseSystems()
        {
            DataSystem?.Release();
            CreateSystem?.Release();
            SchedulerSystem?.Release();
            ImagePreLoaderSystem?.Release();
        }

        #endregion

        #region Request data

        /// <summary>
        /// 冷启动请求弹窗数据
        /// </summary>
        public void RequestPopupDataOnColdStart()
        {
            if (SchedulerSystem.IsRunning)
            {
                LoggerUtils.Log("SchedulerSystem.IsRunning Skip");
                return;
            }

            DataSystem.RequestDataOnCodeStart(ParseDataAndStartScheduler);
        }

        /// <summary>
        /// 暖启动请求弹窗数据
        /// </summary>
        public void RequestPopupDataOnWarmStart()
        {
            if (SchedulerSystem.IsRunning)
            {
                LoggerUtils.Log("SchedulerSystem.IsRunning Skip");
                return;
            }

            DataSystem.RequestDataOnWarmStart(ParseDataAndStartScheduler);
        }

        private void ParseDataAndStartScheduler(PopupRspData popupData)
        {
            SchedulerSystem?.ClearAllSchedule();
            CreateSystem.ParsePopupDataAndCreate(popupData, StartScheduler);
        }

        #endregion

        #region Scheduler Start

        /// <summary>
        /// 启动弹窗轮播队列
        /// </summary>
        public void StartScheduler()
        {
            if (DataSystem.PopupData == null)
            {
                return;
            }

            //延迟一点展示，看到弹出的过程
            TimerManager.Inst.RunOnce(nameof(StartScheduler), 0.5f, () => { SchedulerSystem?.Start(); });
        }

        #endregion
    }
}