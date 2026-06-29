using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabScrollGoTab : TabScrollTab
{
    public GameObject target;

    protected override void OnToggleUIChanged(bool isOn)
    {
        base.OnToggleUIChanged(isOn);
        if (target)
        {
            target.SetActive(isOn);
        }  
    }
}
