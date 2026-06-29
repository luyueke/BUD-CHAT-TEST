using Game.Props.PropsComponents;

namespace UI.UIPanels.GameEdit
{
    public class SensorBoxViewAdapter : BasePropertyAdapter
    {
        private SensorBoxSubView sensorBoxSubView;
        protected override void OnCreate()
        {
            sensorBoxSubView = AddTabView<SensorBoxSubView>("");
            sensorBoxSubView.AddBoxTimesChangeListener(OnBoxTimesValueChange);
        }
        protected override void OnSelectEntity()
        {
            var boxComp = selectEntity.GetComp<SensorBoxComponent>();
            if (boxComp != null)
            {
                sensorBoxSubView.SetDefaultBoxTimes(boxComp.BoxTimes);
            }
        }

        private void OnBoxTimesValueChange(int boxTimes)
        {
            var boxComp = selectEntity.GetComp<SensorBoxComponent>();
            if (boxComp != null)
            {
                boxComp.BoxTimes = boxTimes;
                LoggerUtils.Log("#OnBoxTimesValueChange:"+boxComp.BoxTimes);
            }
        }
    }
}