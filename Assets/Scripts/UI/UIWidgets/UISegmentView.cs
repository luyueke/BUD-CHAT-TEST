using System;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;
using UnityEngine.UI;

public class UISegmentView : MonoBehaviour
{
    private Color _selectTextColor = SegmentColor.SelectTextColor();
    public Color selectTextColor
    {
        set
        {
            _selectTextColor = value;
            updateUI();
        }
        get
        {
            return _selectTextColor;
        }
    }

    private Color _normalTextColor = SegmentColor.NormalTextColor();
    public Color normalTextColor
    {
        set
        {
            _normalTextColor = value;
            updateUI();
        }
        get
        {
            return _normalTextColor;
        }
    }

    /// <summary>
    /// 当前选中的索引
    /// </summary>
    public int CurrentSelectIndex
    {
        get
        {
            return selectedIndex;
        }
    }
    private int selectedIndex = 0;

    /// <summary>
    ///  是否添加点击拦截, 防止多次点击
    /// </summary>
    public bool isThrottle = false;
    
    /// <summary>
    /// 点击是否显示动画
    /// </summary>
    public bool showAnimation = true;
    /// <summary>
    /// 动画时长
    /// </summary>
    public float animationDur = 0.25f;
    
    private Transform content;
    private RectTransform m_HandleRect;
    private Button defaultItem;
    private List<Button> items = new List<Button>();
    
    private Image handleView;
    
    private float tapInterval = 0.35f;

    private Action<int> IndexDidChangeAction;

    private void Init()
    {
        if (defaultItem == null)
        {
            defaultItem = transform.Find("DefaultItem").GetComponent<Button>();
        }

        if (m_HandleRect == null)
        {
            m_HandleRect = transform.Find("SlidingArea").GetComponent<RectTransform>();
        }
        
        if (content == null)
        {
            content = transform.Find("SlidingArea/ContentArea").transform;
        }
        
        if (handleView == null)
        {
            handleView = transform.Find("SlidingArea/Handle").GetComponent<Image>();
        }
        
        
    }
    private void Awake()
    {
        Debug.Log("[Segment] Awake");
        // #warning TODO: repalce find func
        //设置默认颜色
        Init();
    }

    private void Start()
    {
        updateUI();
    }

    public void SetSegementData(List<string> titles, int defaultIndex = 0, Action<int> indexChangeAction = null)
    {
        Debug.Log("[Segment] SetSegementData");
        Init();
        if (titles.Count == 0)
        {
            return;
        }
        if (defaultIndex < titles.Count && defaultIndex >= 0)
        {
            selectedIndex = defaultIndex;
        }
        else
        {
            selectedIndex = 0;
        }

        IndexDidChangeAction = indexChangeAction;

        if (items.Count > 0)
        {
            // TODO: 这个remove 会不会crash
            items.ForEach(x => {GameObject.Destroy(x); });
            items.Clear();
        }

        for (int i = 0; i < titles.Count; i++)
        {
            CreateItem(titles[i], i);
        }
        adjustHandleView();
    }
    
    private void OnEnable()
    {
        if (isThrottle)
        {
            ResetThrottleAction();
        }
    }
    

    private void CreateItem(string title, int index)
    {
        var newItem = GameObject.Instantiate(defaultItem, content);
        newItem.gameObject.SetActive(true);
        var label = newItem.transform.Find("Text").GetComponent<Text>();
        label.text = title;
        newItem.onClick.AddListener(() =>
        {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            OnTrigerItem(index);
        });
        items.Add(newItem);
    }

    private void adjustHandleView()
    {
        if (selectedIndex < items.Count && selectedIndex >= 0)
        {
            
            // var target = items[selectedIndex];
            var size = m_HandleRect.rect.size;
            var itemW = size.x / items.Count;
            float defaultX = itemW * selectedIndex + itemW / 2;
            float defaultY = -size.y / 2;
            
            handleView.rectTransform.sizeDelta = new Vector2(x: itemW, y: size.y);
            handleView.rectTransform.anchoredPosition = new Vector2(x: defaultX, y: defaultY);
        }
        else
        {
            handleView.rectTransform.sizeDelta = m_HandleRect.sizeDelta;
            handleView.rectTransform.anchoredPosition = gameObject.GetComponent<RectTransform>().anchoredPosition;
        }
    }

    private void updateUI(bool animation = false)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var btn = items[i];
            bool isSelect = selectedIndex == i;
            var label = btn.transform.Find("Text").GetComponent<Text>();
            label.color = isSelect ? _selectTextColor : _normalTextColor;
            if (isSelect)
            {
                handleView.rectTransform.anchoredPosition = btn.GetComponent<RectTransform>().anchoredPosition;
            }
        }
    }
    
    private void OnTrigerItem(int index)
    {
        if (index == selectedIndex)
        {
            return;
        }
        if (index >= items.Count)
        {
            return;
        }
        
        if (isThrottle || showAnimation)
        {
            items.ForEach(x => x.interactable = false);
            this.CancelInvoke();
            this.Invoke("ResetThrottleAction", tapInterval);
        }

        selectedIndex = index;
        updateUI(true);
        IndexDidChangeAction?.Invoke(index); 
    }

    public void SetSelectIndex(int index)
    {
        OnTrigerItem(index);
    }

    private void ResetThrottleAction()
    {
        items.ForEach(x => x.interactable = true);
    }
}

class SegmentColor
{
    public static Color NormalTextColor()
    {
        return new Color32(r:120, g: 120, b: 120, a: 255);
    }
    
    public static Color SelectTextColor()
    {
        return Color.black;
    }
}