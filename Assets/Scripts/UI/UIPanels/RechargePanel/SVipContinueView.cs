using System;
using UI.BaseWidgets;
using UnityEngine;

public class SVipContinueView : MonoBehaviour
{
    [SerializeField] private CButton renewalBtn;

    private SubscribeStatusResponse _subscribeStatusResponse;
    private Action _action;

    public void SetData(SubscribeStatusResponse subscribeStatusResponse, Action action)
    {
        this._subscribeStatusResponse = subscribeStatusResponse;
        this._action = action;
    }

    private void Start()
    {
        renewalBtn.onClick.AddListener(() =>
        {
            if ( _action == null)
            {
                return;
            }
            _action.Invoke();
        });
    }

}