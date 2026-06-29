using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemBgColor : MonoBehaviour
{
    public Image img;
    public Color defaultColor = new Color(0.855f, 0.815f, 1f, 1f);

    public void SetColor(string colorStr)
    {
        if (!string.IsNullOrEmpty(colorStr))
        {
            img.color = DataUtil.DeSerializeColorCheckHash(colorStr);
            return;
        }
        img.color = defaultColor;
    }

    public void SetColor(Color color)
    {
        img.color = color;
    }
}
