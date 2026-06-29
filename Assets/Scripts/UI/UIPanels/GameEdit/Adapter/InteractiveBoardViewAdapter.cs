using Game.Props.PropsComponents;

namespace UI.UIPanels.GameEdit
{
    public class InteractiveBoardViewAdapter : BasePropertyAdapter
    {
        private InteractiveBoardSubView subView;
        protected override void OnCreate()
        {
            subView = AddTabView<InteractiveBoardSubView>("设置");
        }
        protected override void OnSelectEntity()
        {
            var boxComp = selectEntity.GetComp<InteractiveBoardComponent>();
        }

       
    }
}