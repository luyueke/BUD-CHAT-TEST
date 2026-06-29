using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class MusicScorePartItem : MonoBehaviour
{
    public GameObject select;
    public CButton deleteBtn;
    public CButton selectBtn;
    public Text partName;
    private int partId;

    public int PartId
    {
        get { return partId; }
    }
    public void Init(Action<MusicScorePartItem> deleteClick,Action<MusicScorePartItem> selectClick)
    {
        deleteBtn.onClick.AddListener(()=>
        {
            deleteClick?.Invoke(this);
        });
        selectBtn.onClick.AddListener(()=>
        {
            if (select.activeSelf)
            {
               return; 
            }
            selectClick?.Invoke(this);
        });
        SetSelected(false);
    }
    public void SetInfo(int pId)
    {
        partId = pId;
        partName.text = ((partId+1) * 2 - 1) + "-" + ((partId+1) * 2); 
    }
    public void SetSelected(bool isSelect)
    {
        select.SetActive(isSelect);
    }
    public void SetDelete(bool isShow)
    {
        deleteBtn.gameObject.SetActive(isShow);
    }
}
