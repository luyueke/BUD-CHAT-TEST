using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;

public class NavigationBarTabs2 : TabView
{
    [SerializeField]
    public CButton BackButton;
    
    public void AddBackBtnClickListener(UnityAction btnCallback)
    {
        BackButton.onClick.AddListener(btnCallback);
    }
}
