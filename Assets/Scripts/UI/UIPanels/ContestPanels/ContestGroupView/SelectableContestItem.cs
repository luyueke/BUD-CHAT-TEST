using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectableContestItem : ActiveContestItem
{
    public Action<SelectableContestItem> OnSelect { get; set; }
    public bool IsSelect { get; private set; }
    [SerializeField] private GameObject selGo;

    private void Awake()
    {
        selGo.SetActive(IsSelect);
    }

    public void SetSelect(bool isOn)
    {
        IsSelect = isOn;
        selGo.SetActive(isOn);
    }

    protected override void OnClickInternal()
    {
        base.OnClickInternal();
        OnSelect?.Invoke(this);
    }
}
