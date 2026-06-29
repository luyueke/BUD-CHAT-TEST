using Game.Props.PropsComponents;

namespace UI.UIPanels.GameEdit
{
    public class TrapBoxViewAdapter : BasePropertyAdapter
    {
        private TrapBoxSubView trapBoxSubView;
        protected override void OnCreate()
        {
            trapBoxSubView = AddTabView<TrapBoxSubView>("");
        }
        protected override void OnSelectEntity()
        {
            var boxComp = selectEntity.GetComp<TrapBoxComponent>();
            if (boxComp != null)
            {
               
            }
        }

       
    }
}