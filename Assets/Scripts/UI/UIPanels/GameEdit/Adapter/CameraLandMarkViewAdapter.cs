using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class CameraLandMarkViewAdapter : BasePropertyAdapter
    {
        CameraLandMarkEditSubView subView;

        protected override void OnCreate()
        {
            subView = AddTabView<CameraLandMarkEditSubView>("设置");
        }

        protected override void OnSelectEntity()
        {
            
        }

    }
}
