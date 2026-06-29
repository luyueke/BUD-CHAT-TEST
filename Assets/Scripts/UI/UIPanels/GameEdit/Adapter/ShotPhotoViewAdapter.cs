using Es;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class ShotPhotoViewAdapter : BasePropertyAdapter
    {
        protected override void OnCreate()
		{
            this.AddTabView<ShotPhotoSubView>("");
        }

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
        }
    }
}