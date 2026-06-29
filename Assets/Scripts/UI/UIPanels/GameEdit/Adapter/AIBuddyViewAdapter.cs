using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class AIBuddyViewAdapter : BasePropertyAdapter
    {
        AIBuddyEditSubView subView;
        AIBuddyBoxEditSubView boxSubView;

        protected override void OnCreate()
        {
            subView = AddTabView<AIBuddyEditSubView>("伙伴设置");
            boxSubView = AddTabView<AIBuddyBoxEditSubView>("盒子");
        }

        protected override void OnSelectEntity()
        {
            
        }

    }
}
