using System;
using UI.BaseWidgets;
using UnityEngine;

public class CommonDailyTaskButton : MonoBehaviour {
    [SerializeField]
    private GameObject Go_Reddot;
    [SerializeField]
    private CButton Btn_Click;
    [SerializeField]
    private GameObject Go_Select;
    public CommonTaskType CurTaskType;
    private Action<CommonTaskType> _onBtnClick;

    private void Awake()
    {
        Btn_Click.onClick.AddListener(OnBtnClick);
    }

    public void InitData(Action<CommonTaskType> act)
    {
        this._onBtnClick = act;
    }

    private void OnBtnClick()
    {
        this._onBtnClick?.Invoke(this.CurTaskType);
    }

    public void SetSelectState(bool isSelect)
    {
        Go_Select?.SetActive(isSelect);
    }

    public void SetReddotEnable(bool enable)
    {
        Go_Reddot.SetActive(enable);
    }

#if UNITY_EDITOR
    private void Reset() {
        Go_Reddot = GameObjectEx.FindChildByName(transform, "Reddot").gameObject;
        Btn_Click = GetComponent<CButton>();
        Go_Select = GameObjectEx.FindChildByName(transform, "Go_Select").gameObject;

    }
#endif

}
