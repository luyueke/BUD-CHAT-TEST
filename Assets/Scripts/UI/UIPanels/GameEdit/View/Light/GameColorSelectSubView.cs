// @Author: YangJie
// @Description:
// @Date:  2023/08/09
// @Modify:

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit.Light
{

    public class ColorSelectItem
    {

        private Image colorImage;
        private Image selectImage;
        private Color color;
        
        public ColorSelectItem(GameObject itemObj, Color color, Action<ColorSelectItem> onClick)
        {
            colorImage = itemObj.GetComponent<Image>();
            selectImage = GameObjectEx.FindComponentByName<Image>(itemObj, "Select");
            selectImage.gameObject.SetActive(false);
            this.color = color;
            selectImage.color = color;
            colorImage.color = color;
            itemObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                onClick?.Invoke(this);
            });
        }

        public void SetSelect(bool isSelect)
        {
            colorImage.enabled = !isSelect;
            selectImage.gameObject.SetActive(isSelect);
        }

        public Color GetColor()
        {
            return color;
        }

    }


    public class GameColorSelectSubView : MonoBehaviour
    {
        
        [SerializeField]
        private GameObject colorItemPrefab;
        
        private readonly List<ColorSelectItem>  colorItems = new List<ColorSelectItem>();
        
        private Action<Color, int> onColorSelect;
        
        
        
        public void SetColors(Color[] colors, Color defaultColor, Action<Color, int> onSelect)
        {
            onColorSelect = onSelect;
            foreach (var color in colors)
            {
                var item = new ColorSelectItem(Instantiate(colorItemPrefab, colorItemPrefab.transform.parent), color, OnClickColorItem);
                colorItemPrefab.SetActive(true);
                colorItems.Add(item);
                item.SetSelect(color == defaultColor);
            }
            colorItemPrefab.SetActive(false);
        }
        
        private void OnClickColorItem(ColorSelectItem item)
        {

            for (var i = 0; i < colorItems.Count; i++)
            {
                var colorItem = colorItems[i];
                if (colorItem == item)
                {
                    colorItem.SetSelect(true);
                    onColorSelect?.Invoke(colorItem.GetColor(), i);
                }
                else
                {
                    colorItem.SetSelect(false);
                }
            }
        }


    }
}