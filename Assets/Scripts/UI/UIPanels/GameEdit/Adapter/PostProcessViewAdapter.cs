namespace UI.UIPanels.GameEdit
{
    public class PostProcessViewAdapter : BasePropertyAdapter
    {
        protected override void OnCreate()
		{
            this.AddTabView<PostProcessSubView>("");
        }

        protected override void OnSelectEntity()
        {
        }
    }
}