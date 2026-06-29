using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubbleThemeItem : MonoBehaviour
{
    [SerializeField] private CButton itemBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private GameObject selectBg;

    private int curBubbleId = 0;
    private Action<int> itemClicListener;

    void Awake()
    {
        itemBtn.onClick.AddListener(OnItemClick);
    }

    public void Init(int bubbleId, Action<int> callback)
    {
        curBubbleId = bubbleId;
        itemClicListener = callback;
        var sp = UserUIWidgetManager.Inst.GetChoosePanelBubbleBg(bubbleId, this.gameObject);
        iconImg.sprite = sp;
    }

    private void OnItemClick()
    {
        itemClicListener?.Invoke(curBubbleId);
        SetSelect(true);
    }

    public void SetSelect(bool isSelect)
    {
        selectBg.SetActive(isSelect);
    }
}
