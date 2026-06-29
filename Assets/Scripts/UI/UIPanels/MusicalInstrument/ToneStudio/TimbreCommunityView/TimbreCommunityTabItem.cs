using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TimbreCommunityTabItem : MonoBehaviour
{
    [SerializeField] private Text _text;
    [SerializeField] private GameObject redDot;
    [SerializeField] private GameObject BackObj;

    private void Awake()
    {
        transform.GetComponent<CButton>()?.onClick.AddListener(OnClickTab);
    }
    
    private void OnClickTab()
    {
        if (string.IsNullOrEmpty(cId))
        {
            return;
        }
        
        clickAction?.Invoke(cId);
    }

    private string cId;
    private Action<string> clickAction;
    public void SetData(string id, Action<string> clickAction)
    {
        cId = id;
        _text.SetLocalText(id);
        this.clickAction = clickAction;
    }

    public void UpdateSelected(bool isSelect)
    {
        BackObj.SetActive(isSelect);
    }

    public void UpdateRedDot(bool showRed)
    {
        redDot.SetActive(showRed);
    }

}
