using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{

// ReSharper disable once CheckNamespace
    public class ClothesColorPickerView : MonoBehaviour
    {
        private Color selectedColor;
        private bool isColorFold = false;
        private Button colorBtn;
        private GameObject colorOriginItem;
        private GameObject ColorParentBG;
        private ColorItem selectColorItem;
        private RectTransform colorBgRectTransform;
        private ScrollRect colorScrollRect;
        private Transform colorContent;
        private RectTransform Viewport;
        private List<ColorItem> colorItems = new List<ColorItem>();
        private int colorLength = 14;
        private float ColorFoldHeight => 124;
        private float ColorUnFoldHeight => 364;
        private int curIndex;
        private Action<Color> onColorSelect;

        private void Awake()
        {

            colorBtn = transform.Find("ColorButton").GetComponent<Button>();
            colorBtn.onClick.AddListener(OnColorFoldBtnClick);
            colorOriginItem = transform.Find("ColorBg/ColorGrid/Viewport/ColorGrid/ColorItem").gameObject;
            colorContent = colorOriginItem.transform.parent;
            colorBgRectTransform = transform.Find("ColorBg").GetComponent<RectTransform>();
            Viewport = transform.Find("ColorBg/ColorGrid/Viewport").GetComponent<RectTransform>();
            ColorParentBG = transform.Find("ColorBg/BG").gameObject;
            colorScrollRect = colorBgRectTransform.Find("ColorGrid").GetComponent<ScrollRect>();
            var colorDataConfigList = Es.DataTables.GetColorDataConfigList();
            int index = 0;
            foreach (var colorDataConfig in colorDataConfigList)
            {
                index++;
                ColorUtility.TryParseHtmlString("#" + colorDataConfig.Color, out var color);
                var item = Instantiate(colorOriginItem, colorOriginItem.transform.parent);
                var colorItem = new ColorItem(item, color, OnColorSelect);
                colorItem.SetSelect(false);
                if (selectColorItem == null)
                {
                    OnColorSelect(colorItem);
                    colorItem.SetSelect(true);
                }
                colorItems.Add(colorItem);
            }
            colorOriginItem.SetActive(false);
            colorScrollRect.vertical = false;
        }

        public void AddListener(Action<Color> onColorSelect)
        {
            this.onColorSelect = onColorSelect;
        }


        private void OnColorSelect(ColorItem colorItem)
        {
            curIndex = colorItems.FindIndex(x=>x == colorItem);
            selectColorItem?.SetSelect(false);
            selectColorItem = colorItem;
            SetSelectColor(colorItem.GetColor());
            onColorSelect?.Invoke(colorItem.GetColor());
            if (!isColorFold)
            {
                OnColorFoldBtnClick();
            }
        }


        public Color GetSelectColor()
        {
            return selectedColor;
        }

        public void SetSelectColor(Color color)
        {
            UGCPaintToolSettings.Current.UGCColor = color;
            selectedColor = color;
            foreach (var tmpItem in colorItems)
            {
                tmpItem.SetSelect(color == tmpItem.GetColor());
            }
        }
        


        private void OnColorFoldBtnClick()
        {
            isColorFold = !isColorFold;
            if (!isColorFold)
            {
                colorBtn.transform.localEulerAngles = new Vector3(0, 0, 180);
                colorScrollRect.vertical = true;
                Viewport.offsetMin = new Vector2(Viewport.offsetMin.x, -112);
                ColorParentBG.transform.localPosition = Vector3.zero;
                colorContent.transform.localPosition = new Vector3(0, 0, 0);
            }
            else
            {
                colorBtn.transform.localEulerAngles = Vector3.zero;
                colorScrollRect.vertical = false;
                Viewport.offsetMin = new Vector2(Viewport.offsetMin.x, 112);
                ColorParentBG.transform.localPosition = new Vector3(0, 250, 0);
                colorContent.transform.localPosition = new Vector3(0, (curIndex / colorLength)*102, 0);
            }
        }

        public class ColorItem
        {

            private readonly Color color;
            private readonly GameObject selectObj;
            private readonly GameObject rootObj;
            public ColorItem(GameObject obj, Color color, Action<ColorItem> onColorSelect)
            {
                rootObj = obj;
                this.color = color;
                GameObjectEx.FindComponentByName<Image>(obj, "Background").color = color;
                selectObj = obj.transform.Find("SelectObj").gameObject;
                obj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    onColorSelect?.Invoke(this);
                });
            }

            public void SetSelect(bool isSelect)
            {
                selectObj.SetActive(isSelect);
            }

            public Color GetColor()
            {
                return color;
            }

            public GameObject GetRoot()
            {
                return rootObj;
            }



        }


    }


}