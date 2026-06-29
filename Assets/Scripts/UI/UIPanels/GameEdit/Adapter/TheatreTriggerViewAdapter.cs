using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class TheatreTriggerViewAdapter : BasePropertyAdapter
    {
        TheatreTriggerEditSubView subView;

        protected override void OnCreate()
        {
            subView = AddTabView<TheatreTriggerEditSubView>("设置");
        }

        protected override void OnSelectEntity()
        {
            
        }

    }
}