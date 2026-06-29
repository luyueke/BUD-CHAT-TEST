using System;
using UnityEngine;
using UnityEngine.UI;

public class AdjustItem
{
    public EAdjustItemType mItemType;
    public string mTitle;
    public float mCurValue;
    public Action<float> mValueChanged;
    public GameObject mGameObject;
    public Scrollbar mScrollbar;
    public Text mTitleText;
    public Action<EAdjustItemType, float> mItemValueChanged;
    public AdjustItem(AdjustItemContext context)
    {
        mItemType = context.mItemType;
        mTitle = context.mTitle;
        mCurValue = context.mCurValue;
    }

    public void Init()
    {
        Find();
        mTitleText.text = mTitle;
        mScrollbar.onValueChanged.AddListener(OnSliderChanged);
        mScrollbar.value = mCurValue;
    }

    public void Find()
    {
        mScrollbar = mGameObject.transform.Find("Scrollbar").GetComponent<Scrollbar>();
        mTitleText = mGameObject.transform.Find("title/Text").GetComponent<Text>();
    }
    public void OnSliderChanged(float value)
    {
        mCurValue = value;
        mItemValueChanged?.Invoke(mItemType, (float)Math.Round(value, 2));
    }
    public void UpdateSilderValue()
    {
        mScrollbar.value = mCurValue;
    }
    public void SetValue(float value)
    {
        mCurValue = value;
        OnSliderChanged(value);
        mScrollbar.SetValueWithoutNotify(value); 
    }
    public void Reset(float value)
    {
        SetValue(value);
    }

    public void SetValueWithoutNotify(float value)
    {
        mCurValue = value;
        mScrollbar.SetValueWithoutNotify(value);
    }
}
