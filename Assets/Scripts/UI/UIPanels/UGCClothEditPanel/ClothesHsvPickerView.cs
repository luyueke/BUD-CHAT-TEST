using System;
using System.Collections;
using System.Collections.Generic;
using Basic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UGCEditor
{

// ReSharper disable once CheckNamespace
    public class ClothesHsvPickerView : MonoBehaviour
    {
        //
        //
        // private Image selectedColorImage;
        // private HSVColorBar hueBar;
        // private HSVColorBar brightBar;
        // private HSVColorBar chromaBar;
        //
        // private Color selectColor = Color.red;
        // private List<Color> colorList = new List<Color>();
        // private UGCColorToggleItem colorToggleItemPrefab;
        // private UGCColorToggleItem curColorToggleItem;
        // private Action<Color> onColorSelect;
        // private Action onHideClick;
        // private int MaxUgcColorCount = 30;//UGC颜色最大数量
        // private static Color reservedColor = new Color(0.9f, 0.9f, 0.9f);//预留位颜色
        // private List<UGCColorToggleItem> ugcColorItems = new List<UGCColorToggleItem>();
        // public void Init()
        // {
        //     selectedColorImage = transform.Find("SelectedColor/Image").GetComponent<Image>();
        //     var hueSprite = Sprite.Create(new Texture2D(7, 1), new Rect(Vector2.zero, new Vector2(7, 1)), Vector2.one * 0.5f);
        //     hueBar = new HSVColorBar(transform.Find("BarContainers/HueBar").gameObject, hueSprite, 0, OnHueChange);
        //
        //     Color.RGBToHSV(selectColor, out var h, out var s, out var v);
        //     Color[] hueColor = new Color[]
        //     {
        //         new Color(1, 0.5f, 0), //橙
        //         new Color(1, 1, 0), //黄
        //         new Color(0, 1, 0), //绿
        //         new Color(0, 1, 1), //青
        //         new Color(0, 0, 1), //蓝
        //         new Color(1, 0, 1), //紫
        //         new Color(1, 0, 0)
        //     }; //红
        //     hueBar.UpdateColor(hueColor);
        //     var brightSprite = Sprite.Create(new Texture2D(2, 1), new Rect(new Vector2(0.5f, 0), Vector2.one), Vector2.one * 0.5f);
        //     brightBar = new HSVColorBar(transform.Find("BarContainers/BrightBar").gameObject, brightSprite, 0, OnBrightChange);
        //     Color[] brightColor = new[]
        //     {
        //         Color.HSVToRGB(h, s, 0),
        //         Color.HSVToRGB(h, s, 1),
        //     };
        //     brightBar.UpdateColor(brightColor);
        //
        //     var chromaSprite = Sprite.Create(new Texture2D(2, 1), new Rect(new Vector2(0.5f, 0), Vector2.one), Vector2.one * 0.5f);
        //     chromaBar = new HSVColorBar(transform.Find("BarContainers/ChromaBar").gameObject, chromaSprite, 0, OnChromaChange);
        //     Color[] chromaColor = new[]
        //     {
        //         Color.HSVToRGB(h, 0, v),
        //         Color.HSVToRGB(h, 1, v),
        //     };
        //     chromaBar.UpdateColor(chromaColor);
        //
        //     colorToggleItemPrefab = GameObjectEx.FindChildByName(transform, "SavedContainer/Layout/SaveColorItem").GetComponent<UGCColorToggleItem>();
        //
        //     var deleteBtn = GameObjectEx.FindComponentByName<Button>(transform, "DeleteBtn");
        //     deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        //     var saveBtn = GameObjectEx.FindComponentByName<Button>(transform, "SaveBtn");
        //     saveBtn.onClick.AddListener(OnSaveBtnClick);
        //     
        //     colorToggleItemPrefab.gameObject.SetActive(false);
        //
        //     var maskBtn = GameObjectEx.FindComponentByName<Button>(transform, "Mask");
        //     maskBtn.onClick.AddListener(() =>
        //     {
        //         gameObject.SetActive(false);
        //         onHideClick?.Invoke();
        //     });
        //     
        //     InitColorItems();
        //     UpdateUgcColors();
        // }
        //
        // /// <summary>
        // /// 初始化颜色预留位
        // /// </summary>
        // public void InitColorItems()
        // {
        //     for (int i = 0; i < MaxUgcColorCount; i++)
        //     {
        //         var item = GameObject.Instantiate(colorToggleItemPrefab, colorToggleItemPrefab.transform.parent);
        //         item.gameObject.SetActive(true);
        //         item.ColorCheckImage.SetActive(false);
        //         item.SetColor(i,reservedColor, null);
        //         ugcColorItems.Add(item);
        //     }
        // }
        //
        // private void OnSaveBtnClick()
        // {
        //     if (colorList.Count >= MaxUgcColorCount)
        //     {
        //         TipPanel.ShowToast("数量达到上限");
        //         return;
        //     }
        //     var index = colorList.Count;
        //     if (curColorToggleItem != null)
        //     {
        //         curColorToggleItem.ColorCheckImage.SetActive(false);
        //     }
        //     curColorToggleItem = ugcColorItems[index];
        //     curColorToggleItem.SetColor(index,selectColor, SetSelectColor);
        //     curColorToggleItem.OnToggleClick();
        //     colorList.Add(selectColor);
        //     if (colorList.Count == (MaxUgcColorCount / 2) + 1)
        //     {
        //         SetColorItemShow(true);
        //     }
        // }
        //
        // /// <summary>
        // /// 设置色盘展示（一行/两行）
        // /// </summary>
        // private void SetColorItemShow(bool isShow)
        // {
        //     for (int i = MaxUgcColorCount / 2; i < ugcColorItems.Count; i++)
        //     {
        //         ugcColorItems[i].gameObject.SetActive(isShow);
        //     }
        // }
        //
        // private void OnDeleteBtnClick()
        // {
        //     if (curColorToggleItem != null)
        //     {
        //         colorList.Remove(curColorToggleItem.color);
        //         UpdateUgcColors();
        //         curColorToggleItem = null;
        //     }
        // }
        //
        // /// <summary>
        // /// 更新色盘颜色展示
        // /// </summary>
        // private void UpdateUgcColors()
        // {
        //     for (int i = 0; i < ugcColorItems.Count; i++)
        //     {
        //         if (i < colorList.Count)
        //         {
        //             ugcColorItems[i].SetColor(i,colorList[i], SetSelectColor);
        //         }
        //         else
        //         {
        //             ugcColorItems[i].SetColor(i,reservedColor, null);
        //         }
        //     }
        //     if (colorList.Count > MaxUgcColorCount / 2)
        //     {
        //         SetColorItemShow(true);
        //     }
        //     else
        //     {
        //         SetColorItemShow(false);
        //     }
        // }
        //
        // public void AddListener(Action<Color> callBack)
        // {
        //     onColorSelect = callBack;
        // }
        //
        // public void AddHideListener(Action callBack)
        // {
        //     onHideClick = callBack;
        // }
        //
        //
        // public void SetSelectColor(Color color)
        // {
        //     var index = colorList.FindIndex(x=>x == color);
        //     if (index < ugcColorItems.Count && index >= 0)
        //     {
        //         if (curColorToggleItem != null)
        //         {
        //             curColorToggleItem.ColorCheckImage.SetActive(false);
        //         }
        //         curColorToggleItem = ugcColorItems[index];
        //         selectColor = color;
        //         selectedColorImage.color = color;
        //         Color.RGBToHSV(color, out var h, out var s, out var v);
        //         hueBar.SetValue(h);
        //         brightBar.SetValue(v);
        //         chromaBar.SetValue(s);
        //         Color[] brightColor = new[]
        //         {
        //             Color.HSVToRGB(h, s, 0),
        //             Color.HSVToRGB(h, s, 1),
        //         };
        //         brightBar.UpdateColor(brightColor);
        //         Color[] chromaColor = new[]
        //         {
        //             Color.HSVToRGB(h, 0, v),
        //             Color.HSVToRGB(h, 1, v),
        //         };
        //         chromaBar.UpdateColor(chromaColor);
        //         onColorSelect?.Invoke(selectColor);
        //     }
        // }
        //
        // private void OnHueChange(float value)
        // {
        //     Color.RGBToHSV(selectColor, out var h, out var s, out var v);
        //     selectColor = Color.HSVToRGB(value, s, v);
        //     selectedColorImage.color = selectColor;
        //     Color[] brightColor = new[]
        //     {
        //         Color.HSVToRGB(value, s, 0),
        //         Color.HSVToRGB(value, s, 1),
        //     };
        //     brightBar.UpdateColor(brightColor);
        //     Color[] chromaColor = new[]
        //     {
        //         Color.HSVToRGB(value, 0, v),
        //         Color.HSVToRGB(value, 1, v),
        //     };
        //     chromaBar.UpdateColor(chromaColor);
        // }
        //
        // private void OnChromaChange(float value)
        // {
        //     Color.RGBToHSV(selectColor, out var h, out var s, out var v);
        //     selectColor = Color.HSVToRGB(h, value, v);
        //     selectedColorImage.color = selectColor;
        //     Color[] brightColor = new[]
        //     {
        //         Color.HSVToRGB(h, value, 0),
        //         Color.HSVToRGB(h, value, 1),
        //     };
        //     brightBar.UpdateColor(brightColor);
        // }
        // private void OnBrightChange(float value)
        // {
        //     Color.RGBToHSV(selectColor, out var h, out var s, out var v);
        //     selectColor = Color.HSVToRGB(h, s, value);
        //     selectedColorImage.color = selectColor;
        //     Color[] chromaColor = new[]
        //     {
        //         Color.HSVToRGB(h, 0, value),
        //         Color.HSVToRGB(h, 1, value),
        //     };
        //     chromaBar.UpdateColor(chromaColor);
        // }
        //
        //
        //
        // public class HSVColorBar
        // {
        //     private readonly Slider slider;
        //     private readonly Texture2D texture;
        //     private readonly EventTrigger selectTrigger;
        //     public HSVColorBar(GameObject go, Sprite sprite, float value, Action<float> onValueChange)
        //     {
        //         slider = GameObjectEx.FindComponentByName<Slider>(go, "Slider");
        //         selectTrigger = GameObjectEx.FindComponentByName<EventTrigger>(go, "Slider/EventTrigger");
        //         var bgImage = GameObjectEx.FindComponentByName<Image>(go, "Image/BackGround");
        //         bgImage.sprite = sprite;
        //
        //
        //         var entry = new EventTrigger.Entry
        //         {
        //             eventID = EventTriggerType.PointerClick,
        //             callback = new EventTrigger.TriggerEvent()
        //         };
        //         entry.callback.AddListener(OnPointerClicked);
        //
        //         selectTrigger.triggers.Add(entry);
        //         texture = sprite.texture;
        //         slider.value = value;
        //         slider.onValueChanged.AddListener((float newValue) =>
        //         {
        //             onValueChange?.Invoke(newValue);
        //         });
        //     }
        //
        //     public void SetValue(float value)
        //     {
        //         slider.value = value;
        //     }
        //
        //     public void UpdateColor(Color[] colors)
        //     {
        //         texture.SetPixels(colors);
        //         texture.Apply();
        //     }
        //
        //     public void OnPointerClicked(BaseEventData eventData)
        //     {
        //         
        //         var mousePosition = eventData.currentInputModule.input.mousePosition;
        //         var rectTransform = selectTrigger.GetComponent<RectTransform>();
        //         RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, GlobalCameraManager.Inst.UICamera,
        //             out var uiLocalPos);
        //
        //
        //         var xPos = rectTransform.sizeDelta.x * rectTransform.pivot.x + uiLocalPos.x;
        //
        //         var value = xPos / rectTransform.sizeDelta.x;
        //         SetValue(value);
        //     }
        //
        // }

    }

}