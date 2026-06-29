using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class NavigationBar : MonoBehaviour
{
    [SerializeField]
    public CButton BackButton;
    [SerializeField]
    public CText TitleText;
    void Start()
    {

    }

    public void AddBackBtnClickListener(UnityAction btnCallback)
    {
        BackButton.onClick.AddListener(btnCallback);
    }

    public void SetTitle(string title)
    {
        TitleText.SetLocalText(title);
    }
}
