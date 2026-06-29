// @Author: YangJie
// @Description:
// @Date:  2023/08/10
// @Modify:

using System;
using UnityEngine;

namespace UI.UIPanels.GameEdit.Light
{
    public class LightBasicSubView: BasePropertyEditSubView
    {

        [SerializeField]
        private GameObject lightSliderObj;
        
        protected override void OnInit()
        {
        
            lightSliderObj.gameObject.SetActive(false);
        }


        public DirLightSlider AddSlider(string title, int maxValue, Action<float> onValueChange,  Func<float, string> valueSet = null)
        {
            var slider = new DirLightSlider(Instantiate(lightSliderObj, lightSliderObj.transform.parent).transform, maxValue, valueSet);
            slider.SetValueChange(onValueChange);
            slider.SetTitle(title);
            return slider;
        }




    }
}