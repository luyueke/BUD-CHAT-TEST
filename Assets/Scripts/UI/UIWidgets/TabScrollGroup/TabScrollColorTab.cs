using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabScrollColorTab : TabScrollTab
{
    public Graphic graphic;
    public Color selectColor = Color.yellow;
    public Color diselectColor = Color.white;

    protected override void OnToggleUIChanged(bool isOn)
    {
        base.OnToggleUIChanged(isOn);
        UpdateColor(isOn);
    }
    public void UpdateColor(bool isOn)
    {
        if (graphic != null)
        {
            graphic.color = isOn ? selectColor : diselectColor;
        }
    }
}
