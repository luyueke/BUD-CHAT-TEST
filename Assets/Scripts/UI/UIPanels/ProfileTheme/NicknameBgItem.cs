using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class NicknameBgItem : MonoBehaviour
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
        var config = UserUIWidgetManager.Inst.GetNicknameData(bubbleId);
        iconImg.sprite = Loader.Load<Sprite>(config.Icon,gameObject);
        iconImg.SetNativeSize();
        if (curBubbleId == 0)
        {
            ColorUtility.TryParseHtmlString("#DEDEDE", out Color c);
            iconImg.color = c;
        }
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
