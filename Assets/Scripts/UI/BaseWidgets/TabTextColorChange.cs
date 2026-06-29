using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabTextColorChange : MonoBehaviour
{
    public Toggle toggle;
    void Awake()
    {
        toggle = transform.GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(ChangeTextColor);
    }

    private void ChangeTextColor(bool isOn)
    {
        
    }
   
}
