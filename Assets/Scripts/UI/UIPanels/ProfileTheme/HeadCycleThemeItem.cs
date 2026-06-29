using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class HeadCycleThemeItem : MonoBehaviour
{
    [SerializeField] private CButton itemBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private GameObject selectBg;
    [SerializeField] private GameObject timeNode;
    [SerializeField] private Text timeText;
    private int curCycleId = 0;
    private Action<int> itemClicListener;

    void Awake()
    {
        itemBtn.onClick.AddListener(OnItemClick);
    }

    public void Init(int headCycleId,string leftTime, Action<int> callback)
    {
        curCycleId = headCycleId;
        itemClicListener = callback;
        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(headCycleId, this.gameObject);
        iconImg.sprite = headCycleData.Sp_PreviewCycle;
        if (headCycleId == 38)
        {
            iconImg.transform.localScale = Vector3.one * 1.4f;
        }
        if (headCycleId == 43)
        {
            iconImg.transform.localScale = Vector3.one * 1.3f;
        }
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
        itemClicListener?.Invoke(curCycleId);
        SetSelect(true);
    }

    public void SetSelect(bool isSelect)
    {
        selectBg.SetActive(isSelect);
    }
}
