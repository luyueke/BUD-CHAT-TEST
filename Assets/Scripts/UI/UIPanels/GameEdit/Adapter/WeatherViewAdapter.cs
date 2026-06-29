namespace UI.UIPanels.GameEdit
{
    public class WeatherViewAdapter : BasePropertyAdapter
    {
        protected override void OnCreate()
		{
            this.AddTabView<WeatherSubView>("");
        }

        protected override void OnSelectEntity()
        {
        }
    }
}