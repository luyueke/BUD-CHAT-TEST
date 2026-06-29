using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TitleBgThemeItem : MonoBehaviour
{
    [SerializeField] private CButton itemBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private GameObject selectBg;
    [SerializeField] private GameObject timeNode;
    [SerializeField] private Text timeText;
    private int titleId = 0;
    private Action<int> itemClicListener;

    void Awake()
    {
        itemBtn.onClick.AddListener(OnItemClick);
    }

    public void Init(int titleId,string leftTime, Action<int> callback)
    {
        this.titleId = titleId;
        itemClicListener = callback;
        var sprite = UserUIWidgetManager.Inst.GetTitleBg(titleId, this.gameObject);
        iconImg.sprite = sprite;

        if (string.IsNullOrEmpty(leftTime))
        {
            timeNode.SetActive(false);
        }
        else
        {
            timeNode.SetActive(true);
            timeText.text = leftTime;
        }
        
    }

    private void OnItemClick()
    {
        itemClicListener?.Invoke(titleId);
        SetSelect(true);
    }

    public void SetSelect(bool isSelect)
    {
        selectBg.SetActive(isSelect);
    }
}
