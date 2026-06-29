using Game.Props.PropsComponents;

namespace UI.UIPanels.GameEdit
{
    public class CollectStarAdapter : BasePropertyAdapter
    {
        private CollectStarSubView subView;
        protected override void OnCreate()
        {
            subView = AddTabView<CollectStarSubView>(string.Empty);
            subView.OnStarNameChanged = OnStarNameChanged;
        }
        protected override void OnSelectEntity()
        {
            var starCmp = selectEntity.GetComp<CollectStarComponent>();
            if (starCmp != null)
            {
                subView.SetText(starCmp.StarName);
            }
        }

        private void OnStarNameChanged(string name)
        {
            var starCmp = selectEntity.GetComp<CollectStarComponent>();
            if (starCmp != null)
            {
                starCmp.StarName = name;
            }
        }
    }
}