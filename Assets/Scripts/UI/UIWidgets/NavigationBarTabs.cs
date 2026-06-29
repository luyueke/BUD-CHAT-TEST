using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;

public class NavigationBarTabs : TabView
{
    [SerializeField]
    public CButton BackButton;
    [SerializeField]
    public CButton PublishBundleButton;

    public void AddBackBtnClickListener(UnityAction btnCallback)
    {
        BackButton.onClick.AddListener(btnCallback);
    }
}
