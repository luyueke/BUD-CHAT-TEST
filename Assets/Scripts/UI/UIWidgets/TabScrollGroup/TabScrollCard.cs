using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabScrollCard : MonoBehaviour
{
    public int groupId;

    public Vector2 Dimension { 
        get
        {
            RectTransform rTransform = transform as RectTransform;
            return rTransform.rect.size;
        }
    }

    protected virtual bool OverrideActiveCheck => false;
    protected virtual bool GetIsElementActive()
    {
        return true;
    }

    public bool IsElementActive => OverrideActiveCheck ? GetIsElementActive() : gameObject.activeSelf;
}
