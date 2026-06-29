using Game.Avatar;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Game.Avatar.HandPartAdapter;

namespace UI.UIPanels.FittingRoom
{
    public class AdjustView : MonoBehaviour
    {
        [SerializeField] Transform ItemContent;
        [SerializeField] Button ResetButton;
        [SerializeField] Button BackButton;
        [SerializeField] SliderItem sliderItem;
        [SerializeField] ColorSliderItem colorSliderItem;


        /// <summary>
        /// 可能为 null, 添加事件时注意判空
        /// </summary>
        [SerializeField] Button switchHandButton;

        private Action resetAction;

        private Dictionary<AdjustViewItemType, string> titleDict = new Dictionary<AdjustViewItemType, string>()
        {
            [AdjustViewItemType.Size] = "大小",
            [AdjustViewItemType.UpDown] = "上下位置",
            [AdjustViewItemType.LeftRight] = "左右位置",
            [AdjustViewItemType.FrontBack] = "前后位置",
            [AdjustViewItemType.Spacing] = "间距",
            [AdjustViewItemType.Vertical] = "垂直位置",
            [AdjustViewItemType.HorizontalStretch] = "水平拉伸",
            [AdjustViewItemType.VerticalStretch] = "垂直拉伸",
            [AdjustViewItemType.Rotation] = "旋转",
            [AdjustViewItemType.XRotation] = "X轴旋转",
            [AdjustViewItemType.YRotation] = "Y轴旋转",
            [AdjustViewItemType.ZRotation] = "Z轴旋转",
            [AdjustViewItemType.Hue] = "色相",
            [AdjustViewItemType.Chroma] = "饱和度",
            [AdjustViewItemType.Bright] = "亮度",
            [AdjustViewItemType.SwitchHand] = "切换手",
        };

        public string GetTitle(AdjustViewItemType type)
        {
            string result = "";
            if (titleDict.ContainsKey(type))
            {
                result = LocalizationManager.Inst.GetLocalizedText(titleDict[type]);
            }
            return result;
        }

        public void Awake()
        {
            if (ResetButton != null) {
                ResetButton.onClick.AddListener(() =>
                {
                    resetAction?.Invoke();
                });
            }

            if (BackButton != null) {
                BackButton.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                });
            }


        }

        public void SetAdjustItems(List<RoleDataAdjust> list)
        {
            UnuseAllItem();
            resetAction = null;
            for (int i = 0, C = list.Count; i < C; i++)
            {
                var data = list[i];

                if (data.AdjustType == AdjustViewItemType.SwitchHand)
                {
                    var switchHand = Instantiate(switchHandButton, ItemContent);
                    HandLRType switchType = (HandLRType)Mathf.RoundToInt(data.Getter().x);
                    switchHand.onClick.AddListener(() =>
                    {
                        if (switchType == HandLRType.Left) switchType = HandLRType.Right;
                        else if (switchType == HandLRType.Right) switchType = HandLRType.Left;
                        data.Setter?.Invoke(new Vector3((int)switchType, 0, 0));
                        data.Apply();
                    });
                    continue;
                }

                var item = Instantiate(sliderItem, ItemContent);
                item.SetName(GetTitle(data.AdjustType));
                item.SetValueWithoutNotify(GetSliderValue(data));
                item.SetCallback(v =>
                {
                    var v3 = GetValueBySlider(data, v);
                    data.Setter(v3);
                    data.Apply();
                });
                resetAction += () =>
                {
                    var defaultValue = data.Default();
                    data.Setter(defaultValue);
                    item.SetValueWithoutNotify(GetSliderValue(data));
                    data.Apply();
                };
            }
        }

        #region ColorView
        private ColorSliderItem Hue;
        private ColorSliderItem Chroma;
        private ColorSliderItem Bright;
        private Color[] hueColor = new Color[] { new Color(1, 0, 0), new Color(1, 1, 0), new Color(0, 1, 0), new Color(0, 1, 1), new Color(0, 0, 1), new Color(1, 0, 1), new Color(1, 0, 0) };
        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="colorAdjust"></param>
        public void SetAdjustColors(RoleColorAdjust colorAdjust)
        {
            UnuseAllItem();
            var color = colorAdjust.Getter();
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Hue = Instantiate(colorSliderItem, ItemContent);
            Chroma = Instantiate(colorSliderItem, ItemContent);
            Bright = Instantiate(colorSliderItem, ItemContent);

            Hue.SetName(GetTitle(AdjustViewItemType.Hue));
            Hue.SetCallback(_h =>
            {
                colorAdjust.Setter(Color.HSVToRGB(_h, Chroma.Value, Bright.Value));
                Chroma.SetColors(new Color[] { Color.HSVToRGB(_h, 0, Bright.Value), Color.HSVToRGB(_h, 0.5f, Bright.Value), Color.HSVToRGB(_h, 1, Bright.Value), });
                Bright.SetColors(new Color[] { Color.HSVToRGB(_h, Chroma.Value, 0), Color.HSVToRGB(_h, Chroma.Value, 0.5f), Color.HSVToRGB(_h, Chroma.Value, 1), });
                colorAdjust.Apply();
            });

            Chroma.SetName(GetTitle(AdjustViewItemType.Chroma));
            Chroma.SetCallback(_s =>
            {
                colorAdjust.Setter(Color.HSVToRGB(Hue.Value, _s, Bright.Value));
                Bright.SetColors(new Color[] { Color.HSVToRGB(Hue.Value, _s, 0), Color.HSVToRGB(Hue.Value, _s, 0.5f), Color.HSVToRGB(Hue.Value, _s, 1), });
                colorAdjust.Apply();
            });

            Bright.SetName(GetTitle(AdjustViewItemType.Bright));
            Bright.SetCallback(_v =>
            {
                colorAdjust.Setter(Color.HSVToRGB(Hue.Value, Chroma.Value, _v));
                Chroma.SetColors(new Color[] { Color.HSVToRGB(Hue.Value, 0, _v), Color.HSVToRGB(Hue.Value, 0.5f, _v), Color.HSVToRGB(Hue.Value, 1, _v), });
                colorAdjust.Apply();
            });

            ResetColor(h, s, v);

            resetAction = () =>
            {
                var color = colorAdjust.Default();
                Color.RGBToHSV(color, out float h, out float s, out float v);
                ResetColor(h, s, v);
                colorAdjust.Setter(color);
                colorAdjust.Apply();
            };
        }

        public void ResetAdjust() {
            resetAction?.Invoke();
        }

        public void ResetColor(float h, float s, float v)
        {
            Hue.SetValueWithoutNotify(h);
            Hue.SetColors(hueColor);
            Chroma.SetValueWithoutNotify(s);
            Chroma.SetColors(new Color[] { Color.HSVToRGB(h, 0, v), Color.HSVToRGB(h, 0.5f, s), Color.HSVToRGB(h, 1, s), });
            Bright.SetValueWithoutNotify(v);
            Bright.SetColors(new Color[] { Color.HSVToRGB(h, s, 0), Color.HSVToRGB(h, s, 0.5f), Color.HSVToRGB(h, s, 1), });
        }
        #endregion

        private void UnuseAllItem()
        {
            for (int i = ItemContent.childCount - 1; i >= 0; i--)
            {
                var item = ItemContent.GetChild(i);
                Destroy(item.gameObject);
            }
        }

        private Vec3 GetValueBySlider(RoleDataAdjust dataAdjust, float progress)
        {
            var limit = dataAdjust.Limit();
            Vec3 curVec = dataAdjust.Getter();
            AdjustAxis vAxis = dataAdjust.Axis();

            Vector3 max = limit[1];
            Vector3 min = limit[0];
            if (dataAdjust.AdjustType == AdjustViewItemType.FrontBack)
            {
                max = limit[0];
                min = limit[1];
            }
            var cur = curVec.Clone();
            switch (vAxis)
            {
                case AdjustAxis.X:
                    cur.x = Mathf.Lerp(min.x, max.x, progress);
                    break;
                case AdjustAxis.None:
                    cur = Vector3.Lerp(min, max, progress);
                    break;
                case AdjustAxis.Z:
                    cur.z = Mathf.Lerp(min.z, max.z, progress);
                    break;
                case AdjustAxis.Y:
                    cur.y = Mathf.Lerp(min.y, max.y, progress);
                    break;
            }

            cur.x = (float)Math.Round(cur.x, 4);
            cur.y = (float)Math.Round(cur.y, 4);
            cur.z = (float)Math.Round(cur.z, 4);
            return cur;
        }

        private float GetSliderValue(RoleDataAdjust dataAdjust)
        {
            var limit = dataAdjust.Limit();
            Vec3 curVec = dataAdjust.Getter();
            AdjustAxis vAxis = dataAdjust.Axis();

            if (limit == null || limit.Count != 2)
            {
                Debug.LogError("配置表内微调数据错误");
                return 0;
            }
            Vector3 max = limit[1];
            Vector3 min = limit[0];
            if (dataAdjust.AdjustType == AdjustViewItemType.FrontBack)
            {
                max = limit[0];
                min = limit[1];
            }

            Vector3 cur = curVec;
            switch (vAxis)
            {
                case AdjustAxis.X:
                    max.y = 0;
                    max.z = 0;
                    min.y = 0;
                    min.z = 0;
                    cur.y = 0;
                    cur.z = 0;
                    break;
                case AdjustAxis.Z:
                    max.y = 0;
                    max.x = 0;
                    min.y = 0;
                    min.x = 0;
                    cur.y = 0;
                    cur.x = 0;
                    break;
                case AdjustAxis.Y:
                    max.x = 0;
                    max.z = 0;
                    min.x = 0;
                    min.z = 0;
                    cur.x = 0;
                    cur.z = 0;
                    break;
            }
            float length = (max - min).magnitude;
            float curLength = (cur - min).magnitude;
            float progress = 0;
            if (length == 0)
            {
                LoggerUtils.LogError($"分母不能为 0, max: {max}, min: {min}, cur: {cur}  {dataAdjust.AdjustType}, A {vAxis}");
                progress = 1;
            }
            else
            {
                progress = (float)Math.Round(curLength / length, 2);
                progress = Mathf.Clamp01(progress);
            }
            return progress;
        }
    }

    public class RoleDataAdjust
    {
        public Func<Vector3> Getter = () => Vector3.zero;
        public Func<Vector3> Default = () => Vector3.zero;
        public Func<AdjustAxis> Axis = () => AdjustAxis.None;
        public Action<Vector3> Setter;
        public Func<List<Vector3>> Limit;
        public Action Apply;
        public AdjustViewItemType AdjustType;
    }

    public class RoleColorAdjust
    {
        public Func<Color> Getter = () => Color.white;
        public Func<Color> Default = () => Color.white;
        public Action<Color> Setter;
        public Action Apply;
    }

    public enum AdjustViewItemType
    {
        Size,                           // 大小
        UpDown,                         // 上下位置
        LeftRight,                      // 左右位置
        FrontBack,                      // 前后位置
        Spacing,                        // 间距
        Vertical,                       // 垂直位置
        HorizontalStretch,              // 水平拉伸
        VerticalStretch,                // 垂直拉伸
        Rotation,                       // 旋转
        XRotation,                      // X轴旋转
        YRotation,                      // Y轴旋转
        ZRotation,                      // Z轴旋转
        Hue,                            // 色相
        Chroma,                         // 饱和度
        Bright,                         // 亮度
        SwitchHand,                     // 交换左右手持
    }

    public enum AdjustAxis
    {
        None,
        X,
        Z,
        Y
    }
}
