/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-24 15:07:03
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-26 15:51:58
 * @ Description: 道具颜色面板的子界面
 */
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using Es;
using UnityEngine.UI;
using System;
using UI.UIWidgets;
using GameData;
using Basic.UndoRedo;
using UndoSystem;

namespace UI.UIPanels.GameEdit
{
    public class GameColorEditSubView : BasePropertyEditSubView
    {
        [Header("业务UI")]
        [SerializeField]private GameIconSelectItem colorItemTmpl; // 颜色Item模版
        [SerializeField]private Transform colorCollectListTF; // 收藏列表
        [SerializeField]private Transform colorListTF;
        [SerializeField]private Transform colorNormalListTF;
        [SerializeField]private Transform colorContentTF;
        [SerializeField]private CButton collectBtn; // 收藏按钮
        [SerializeField]private CButton deleteBtn; // 删除按钮

        [Header("颜色滑块")]
        [SerializeField]private Slider colorHSlider;
        [SerializeField]private Slider colorSSlider;
        [SerializeField]private Slider colorVSlider;

        bool isEnableUnRedo = true; // 是否开启UnRedo

        ColorSliderGradient sColorSliderGradient;
        ColorSliderGradient vColorSliderGradient;

        const int COLLECTCOLOR_MAX_COUNT = 8;
        Color? curColor = null; // 当前颜色
        int curColorSelectIndex = -1; // 当前颜色库的选择Index
        int curCollectColorSelectIndex = -1; //当前颜色收藏库的选择Index
        bool isSaveToLocal = false;
        List<ColorDataConfig> colorDataList;
        List<GameIconSelectItem> items = new List<GameIconSelectItem>();
        List<string> colorCollectDataList;
        List<GameIconSelectItem> collectItems = new List<GameIconSelectItem>();
        Action<Color> onColorChangeAction;

        void OnDisable()
        {
            if (isSaveToLocal)
            {
                LocalDataUtils.Inst.NotifySaveLocal();
                isSaveToLocal = false;
            }
        }

		protected override void OnInit()
		{
            colorDataList = DataTables.GetColorDataConfigList();

            InitColorList(); // 颜色列表
            InitCollectColorList(); // 颜色收藏列表
            InitColorSlider(); // 颜色滑条
		}

		protected override void OnExpand(bool bIsExpand)
		{
			base.OnExpand(bIsExpand);

            foreach (var item in items)
            {
                if (bIsExpand)
                {
                    item.transform.SetParent(colorListTF);
                } else {
                    item.transform.SetParent(colorNormalListTF);
                }
            }
		}

        public void AddColorChangeListener(Action<Color> action)
        {
            onColorChangeAction += action;
        }

        /// <summary>
        /// 设置为嵌入模式
        /// </summary>
        public void SetEmbedStyle()
        {
            SetIsExpand(true);
            var expandUIRT = expandUI.GetComponent<RectTransform>();
            var size = expandUIRT.sizeDelta;
            size.x = 1500;
            expandUIRT.sizeDelta = size;
            expandUI.GetComponent<Image>().enabled = false;
            colorContentTF.GetComponent<Image>().enabled = false;
        }

        /// <summary>
        /// 设置颜色
        /// 注意： 带通知的颜色设定，这个方法会触发Undo/Redo
        /// </summary>
        public void SetColor(Color color)
        {
            curColor = color;
            bool isInLibrary = false;
            var colorHtmlString = ColorUtility.ToHtmlStringRGBA(color);
            // 查找收藏库
            if (colorCollectDataList == null)
                colorCollectDataList = LocalDataUtils.Inst.GetCustomizeColorInfo();
            for (int i = 0; i < colorCollectDataList.Count; i++)
            {
                if (colorHtmlString == colorCollectDataList[i])
                {
                    collectItems[i].SetSelect(true);
                    isInLibrary = true;
                    break;
                }
            }
            // 选择颜色库
            if (!isInLibrary)
            {
                for (int i = 0; i < colorDataList.Count; i++)
                {
                    if (colorHtmlString == colorDataList[i].Color)
                    {
                        items[i].SetSelect(true);
                        isInLibrary = true;
                        break;
                    }
                }
            }
            if (!isInLibrary)
            {
                SetHSVColor(color);
            }
        }

        /// <summary>
        /// 设置颜色
        /// 不带通知回调的
        /// </summary>
        public void SetColorWithNoNotify(Color color)
        {
            curColor = color;
            CancelCurrentColorItemSelect();
            if (color == Color.clear) {
                return;
            }

            bool isInLibrary = false;
            var colorHtmlString = ColorUtility.ToHtmlStringRGBA(color);
            // 查找收藏库
            if (colorCollectDataList == null)
                colorCollectDataList = LocalDataUtils.Inst.GetCustomizeColorInfo();
            for (int i = 0; i < colorCollectDataList.Count; i++)
            {
                if (colorHtmlString == colorCollectDataList[i])
                {
                    curCollectColorSelectIndex = i;
                    collectItems[i].SetSelectWithNoNotify(true);
                    isInLibrary = true;
                    break;
                }
            }
            // 选择颜色库
            if (!isInLibrary)
            {
                for (int i = 0; i < colorDataList.Count; i++)
                {
                    if (colorHtmlString == colorDataList[i].Color)
                    {
                        curColorSelectIndex = i;
                        items[i].SetSelectWithNoNotify(true);
                        isInLibrary = true;
                        break;
                    }
                }
            }

            SetHSVColor(color,false);
        }

        void SetHSVColor(Color color,bool hasCallback = true)
        {
            // HSV调色板
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            if (hasCallback)
            {
                colorHSlider.value = h;
                colorSSlider.value = s;
                colorVSlider.value = v;
            } else {
                colorHSlider.SetValueWithoutNotify(h);
                colorSSlider.SetValueWithoutNotify(s);
                colorVSlider.SetValueWithoutNotify(v);
                ChangeColorSliderGradient();
            }
        }

        Color GetHSVColor()
        {
            return Color.HSVToRGB(colorHSlider.value, colorSSlider.value, colorVSlider.value);
        }

        void InitColorList()
        {
            colorItemTmpl.gameObject.SetActive(false);
            for (int i = 0; i < colorDataList.Count; i++)
            {
                var index = i;
                var colorData = colorDataList[i];
                var colorItem = Instantiate(colorItemTmpl, isExpand ? colorListTF : colorNormalListTF);
                colorItem.AddOnSelectListener(() => OnColorSelect(colorItem, index));
                colorItem.SetSelectWithNoNotify(i == curColorSelectIndex);
                if (ColorUtility.TryParseHtmlString("#" + colorData.Color, out var c))
                {
                    colorItem.SetIconColor(c);
                    colorItem.SetSelectIconColor(c);
                }
                colorItem.gameObject.SetActive(true);
                items.Add(colorItem);
            }
        }

        /// <summary>
        /// 初始化颜色收藏列表
        /// </summary>
        void InitCollectColorList()
        {
            colorCollectDataList = LocalDataUtils.Inst.GetCustomizeColorInfo();
            collectBtn.onClick.AddListener(OnColorCollectBtn);
            deleteBtn.onClick.AddListener(OnColorDeleteBtn);

            for (int i = 0; i < COLLECTCOLOR_MAX_COUNT; i++)
            {
                var index = i;
                var colorItem = Instantiate(colorItemTmpl, colorCollectListTF);
                colorItem.SetSelectWithNoNotify(false);
                colorItem.GetIconGo().SetActive(false);
                colorItem.gameObject.SetActive(true);
                collectItems.Add(colorItem);
                colorItem.AddOnSelectListener(() => OnCollectColorSelect(colorItem, index));
            }

            RefreshColorCollectList();
        }

        void InitColorSlider()
        {
            sColorSliderGradient = colorSSlider.GetComponentInChildren<ColorSliderGradient>(true);
            vColorSliderGradient = colorVSlider.GetComponentInChildren<ColorSliderGradient>(true);

            colorHSlider.onValueChanged.AddListener(OnColorSliderChange);
            colorSSlider.onValueChanged.AddListener(OnColorSliderChange);
            colorVSlider.onValueChanged.AddListener(OnColorSliderChange);
        }

        void RefreshColorCollectList()
        {
            for (int i = 0; i < collectItems.Count; i++)
            {
                var colorItem = collectItems[i];
                if (colorCollectDataList.Count > i && ColorUtility.TryParseHtmlString("#" + colorCollectDataList[i], out var c))
                {
                    colorItem.GetIconGo().SetActive(true);
                    colorItem.SetIconColor(c);
                    colorItem.SetSelectIconColor(c);
                } else {
                    colorItem.GetIconGo().SetActive(false);
                }
            }
        }

        void ChangeColorSliderGradient()
        {
            // 获取明度和饱和度
            var sLeftCR = Color.gray;
            var sRightCR = Color.HSVToRGB(colorHSlider.value, 1, colorVSlider.value);
            var vLeftCR = Color.black;
            var vRightCR = Color.HSVToRGB(colorHSlider.value, colorSSlider.value, 1);

            sColorSliderGradient.SetColor(sLeftCR, sRightCR);
            vColorSliderGradient.SetColor(vLeftCR, vRightCR);
        }

        void OnColorSliderChange(float v)
        {
            ChangeColorSliderGradient();
            NotifyColorChangeWithRecord(GetHSVColor());
        }

        void CancelCurrentColorItemSelect()
        {
            // 取消颜色库的选择
            if (curColorSelectIndex >= 0)
            {
                items[curColorSelectIndex].SetSelectWithNoNotify(false);
                curColorSelectIndex = -1;
            }
            // 取消颜色收藏库的选择
            if (curCollectColorSelectIndex >= 0)
            {
                collectItems[curCollectColorSelectIndex].SetSelectWithNoNotify(false);
                curCollectColorSelectIndex = -1;
            }
        }

        void OnColorSelect(GameIconSelectItem selectItem, int index)
        {
            if (curColorSelectIndex == index) return;
            CancelCurrentColorItemSelect();

            curColorSelectIndex = index;
            if (ColorUtility.TryParseHtmlString("#" + colorDataList[index].Color, out var c))
            {
                SetHSVColor(c, false);
                NotifyColorChangeWithRecord(c);
            }
        }

        void OnCollectColorSelect(GameIconSelectItem selectItem, int index)
        {
            if (curCollectColorSelectIndex == index) return;
            CancelCurrentColorItemSelect();

            curCollectColorSelectIndex = index;
            if (ColorUtility.TryParseHtmlString("#" + colorCollectDataList[index], out var c))
            {
                SetHSVColor(c, false);
                NotifyColorChangeWithRecord(c);
            }
        }

        void OnColorCollectBtn()
        {
            if (colorCollectDataList.Count >= COLLECTCOLOR_MAX_COUNT)
            {
                // 提示
                TipPanel.ShowToast("超出收藏限制。");
                return;
            }

            var color = GetHSVColor();
            colorCollectDataList.Add(ColorUtility.ToHtmlStringRGBA(color));
            RefreshColorCollectList();
            isSaveToLocal = true;
            LocalDataUtils.Inst.SetCustomizeColorInfo(colorCollectDataList);
        }

        void OnColorDeleteBtn()
        {
            if (curCollectColorSelectIndex>=0)
            {
                colorCollectDataList.RemoveAt(curCollectColorSelectIndex);
                CancelCurrentColorItemSelect();
                RefreshColorCollectList();
                isSaveToLocal = true;
                LocalDataUtils.Inst.SetCustomizeColorInfo(colorCollectDataList);
            }
        }

        void NotifyColorChangeWithRecord(Color nColor)
        {
            if (isEnableUnRedo && curColor != null && curColor != Color.clear)
            {
                var beginData = CreateUndoData(curColor);
                NotifyColorChange(nColor);
                var endData = CreateUndoData(curColor);
                AddRecord(beginData, endData);
            } else {
                NotifyColorChange(nColor);
            }
        }

        void NotifyColorChange(Color nColor)
        {
            onColorChangeAction?.Invoke(nColor);
            curColor = nColor;
        }

        #region Undo/Redo

        public void EnableUnRedo(bool isEnable)
        {
            isEnableUnRedo = isEnable;
        }

        public void OnUndoSelect(ColorCommonSelectUndoData data)
        {
            SetColorWithNoNotify(data.color);
            NotifyColorChange(data.color);
        }

        ColorCommonSelectUndoData CreateUndoData(Color? color)
        {
            ColorCommonSelectUndoData data = new ColorCommonSelectUndoData();
            if (color.HasValue)
            {
                data.color = color.Value;
            } else {
                data.color = Color.clear;
            }
            data.targetEntity = selectEntity;
            data.adapterType = GetAdapter()?.GetType();
            return data;
        }

        void AddRecord(ColorCommonSelectUndoData beginData, ColorCommonSelectUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.ColorCommonSelectUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        #endregion
    }
}
