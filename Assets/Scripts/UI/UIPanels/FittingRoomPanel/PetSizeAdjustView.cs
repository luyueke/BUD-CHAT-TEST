using System;
using System.Collections;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class PetSizeAdjustView : MonoBehaviour
    {
        [SerializeField] private SliderItem sliderItem;

        [SerializeField] private Button resetBtn;

        // Start is called before the first frame update
        public void SetItemData(RoleDataAdjust data)
        {
            sliderItem.SetValueWithoutNotify(GetSliderValue(data));
            sliderItem.SetCallback(v =>
            {
                var v3 = GetValueBySlider(data, v);
                data.Setter(v3);
                data.Apply();
            });

            resetBtn.onClick.AddListener(() =>
            {
                var defaultValue = data.Default();
                data.Setter(defaultValue);
                sliderItem.SetValueWithoutNotify(GetSliderValue(data));
                data.Apply();
            });
        }

        private Vec3 GetValueBySlider(RoleDataAdjust dataAdjust, float progress)
        {
            var limit = dataAdjust.Limit();
            Vec3 curVec = dataAdjust.Getter();
            AdjustAxis vAxis = dataAdjust.Axis();

            Vector3 max = limit[1];
            Vector3 min = limit[0];
            var cur = Vector3.Lerp(min, max, progress);
            return cur;
        }

        private float GetSliderValue(RoleDataAdjust dataAdjust)
        {
            var limit = dataAdjust.Limit();
            Vec3 curVec = dataAdjust.Getter();

            Vector3 max = limit[1];
            Vector3 min = limit[0];

            float length = (max - min).magnitude;
            float curLength = (curVec - min).magnitude;
            float progress = 0;
            if (length == 0)
            {
                LoggerUtils.LogError($"宠物Size分母不能为 0, max: {max}, min: {min}, cur: {curVec} ");
                progress = 1;
            }
            else
            {
                progress = (float) Math.Round(curLength / length, 2);
                progress = Mathf.Clamp01(progress);
            }

            return progress;
        }
    }
}