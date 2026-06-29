using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIShowComputerPanel : BasePanel<AIShowComputerPanel>
{
    public GameObject[] View;
    
    public GameObject[] Pages;

    public Button NextBtn;
    
    public Button LeftBtn;
    public Button RightBtn;
    public Button CloseBtn;

    public Action OnClose;
    private int currentPage = 0;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(ClosePanel);
        LeftBtn.onClick.AddListener(OnPreClick);
        RightBtn.onClick.AddListener(OnNextClick);
        NextBtn.onClick.AddListener(OnNextViewClick);
        Pages[currentPage].SetActive(true);
    }


    private void OnNextViewClick()
    {
        View[0].SetActive(false);
        View[1].SetActive(true);
        ChangePage();
    }

    private void ClosePanel()
    {
        OnClose?.Invoke();
        CloseSelf();
    }

    public void OnPreClick()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ChangePage();
        }
    }


    public void OnNextClick()
    {
        if (currentPage < Pages.Length - 1)
        {
            currentPage++;
            ChangePage();
        }
    }

    public void ChangePage()
    {
        LeftBtn.gameObject.SetActive(currentPage > 0);
        RightBtn.gameObject.SetActive(currentPage < Pages.Length - 1);
        for (var i = 0; i < Pages.Length; i++)
        {
            Pages[i].SetActive(false);
        }
        Pages[currentPage].SetActive(true);
    }

}
