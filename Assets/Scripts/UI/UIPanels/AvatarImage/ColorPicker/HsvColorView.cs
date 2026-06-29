using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 有滑条的调色面板
/// @stanley
/// </summary>
public class HsvColorView : MonoBehaviour , IColorSetable
{
    public Transform adjustParent;
    public GameObject adjustItem;
    public Action<string> OnSelect;

    private AdjustItem hueItem;
    private AdjustItem brightItem; //bright - s
    private AdjustItem chromaItem; //chroma - v  

    private HsvTexPainter painter;
    private bool hasManualSet = false;

    private Func<string> getCurTargetColor;

    public void InitHsvView(Action<string> select)
    {
        OnSelect = select;
        GameObject hueGo = Instantiate(adjustItem, adjustParent);
        GameObject chromaGo = Instantiate(adjustItem, adjustParent);
        GameObject brightGo = Instantiate(adjustItem, adjustParent);

        AdjustItemFactory factory = new AdjustItemFactory();
        hueItem = CreateAdjustItem(factory, hueGo, EAdjustItemType.Hue);
        brightItem = CreateAdjustItem(factory, brightGo, EAdjustItemType.Bright);
        chromaItem = CreateAdjustItem(factory, chromaGo, EAdjustItemType.Chroma);       

        hueItem.mItemValueChanged = (t, v) => OnHueChange(v);     
        brightItem.mItemValueChanged = (t, v) => OnBrightChange(v);
        chromaItem.mItemValueChanged = (t, v) => OnChromaChange(v);

        painter = new HsvTexPainter();
        painter.BindImages(GetItemImage(hueItem), GetItemImage(brightItem), GetItemImage(chromaItem));
    }

    public bool GetHasManualSet()
    {
        return hasManualSet;
    }

    public void SetGetCurrentTarget(Func<string> getCurrent)
    {
        getCurTargetColor = getCurrent;
    }

    public void SetSelect(string hexColor)
    {
        if(ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            hueItem.SetValue(h);
            brightItem.SetValue(s);
            chromaItem.SetValue(v);           
            painter.Set(h, s, v);
        }
    }

    public void SelectWithoutNotify(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            hueItem.SetValueWithoutNotify(h);
            brightItem.SetValueWithoutNotify(s);
            chromaItem.SetValueWithoutNotify(v);
            painter.Set(h, s, v);
        }
    }


    public void OnHueChange(float value)
    {
        OnColorChange(value, brightItem.mCurValue, chromaItem.mCurValue);
    }

    public void OnBrightChange(float value)
    {
        OnColorChange(hueItem.mCurValue, value, chromaItem.mCurValue);
    }

    public void OnChromaChange(float value)
    {
        OnColorChange(hueItem.mCurValue, brightItem.mCurValue, value);
    }

    public string GetCurrentColor()
    {
        return GetColorString(hueItem.mCurValue, brightItem.mCurValue, chromaItem.mCurValue);
    }

    public void SetToCurrentTarget()
    {
        if (getCurTargetColor != null)
        {
            SelectWithoutNotify(getCurTargetColor.Invoke());
        }
    }

    private void OnColorChange(float h, float s, float v)
    {
        string color = GetColorString(h, s, v);
        OnSelect?.Invoke(color);
        painter.Set(h, s, v);
        hasManualSet = true;
    }

    private AdjustItem CreateAdjustItem(AdjustItemFactory factory, GameObject go, EAdjustItemType type)
    {
        AdjustItemContext context = AdjustItemContextFactory.Create(type);
        return factory.Create(context, go);
    }

    private string GetColorString(float h, float s, float v)
    {
        Color color = Color.HSVToRGB(h, s, v);
        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }

    private Image GetItemImage(AdjustItem item)
    {
        return item.mScrollbar.transform.Find("Mask/Slider").GetComponent<Image>();
    }
}
