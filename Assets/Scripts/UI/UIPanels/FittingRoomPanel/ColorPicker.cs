using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ColorPicker : MonoBehaviour
    {
        [SerializeField] ToggleGroup colorContent;
        [SerializeField] ColorItem colorItem;
        [SerializeField] private Button customButton;
        [SerializeField] private ColorItem customItem;

        private List<Color> colorList;
        private Queue<ColorItem> colorItemPool = new();
        private Dictionary<Color, ColorItem> colorDict = new();
        private ColorItem curSelectedItem;

        private Action<Color> onValueChanged;
        private Action onCustomButtonClick;

        public Color CustomColor => customItem.Color;

        public void Awake()
        {
            customButton.onClick.AddListener(OnCustomClick);
        }

        public void SetColors(bool customColor, Color selected, List<Color> list)
        {
            colorList = list;
            customButton.gameObject.SetActive(customColor);
            customItem.gameObject.SetActive(false);
            customItem.SetToggleGroup(colorContent);
            customItem.SetSelectedCallBack(OnSelecedColor);
            UnuseAllItem();
            for (int i = 0, C = list.Count; i < C; i++)
            {
                var color = list[i];
                ColorItem item;
                if (colorDict.ContainsKey(color)) continue;
                if (colorItemPool.Count > 0)
                {
                    item = colorItemPool.Dequeue();
                }
                else
                {
                    item = Instantiate(customItem, colorContent.transform);
                }

                item.gameObject.SetActive(true);
                item.transform.SetSiblingIndex(2 + i);
                item.SetColor(color);
                item.SetToggleGroup(colorContent);
                item.SetSelectedCallBack(OnSelecedColor);
                colorDict.Add(color, item);
            }
            OnSelecedColor(selected);
        }

        public void OnSelecedColor(Color color)
        {
            var custom = !colorList.Contains(color);
            if (custom)
            {
                customItem.gameObject.SetActive(true);
                customItem.SetColor(color);
                SetSelectedItem(customItem);
            }
            else
            {
                SetSelectedItem(colorDict[color]);
            }

            onValueChanged?.Invoke(color);
        }

        private void UnuseAllItem()
        {
            foreach (var kv in colorDict)
            {
                kv.Value.gameObject.SetActive(false);
                kv.Value.SetToggleGroup(null);
                colorItemPool.Enqueue(kv.Value);
            }

            colorDict.Clear();
        }

        private void SetSelectedItem(ColorItem item)
        {
            curSelectedItem?.SetIsOn(false);
            curSelectedItem = item;
            curSelectedItem?.SetIsOn(true);
        }

        public void SetCallback(Action<Color> action)
        {
            onValueChanged = action;
        }

        private void OnCustomClick()
        {
            onCustomButtonClick?.Invoke();
        }

        public void SetCustomButtonCallback(Action action)
        {
            onCustomButtonClick = action;
        }
    }
}
