using System;
using UnityEngine;
using UnityEngine.UI;
using Basic.Extensions;


[RequireComponent(typeof(Toggle))]
public class UToggleHelper : MonoBehaviour {

    [SerializeField]
    private Toggle toggle;

    [SerializeField]
    private GameObject checkObj;

    protected void Awake() {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

    }
    protected void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    private void OnToggleValueChanged(bool value)
    {
        if (checkObj != null)
        {
            checkObj.SetActive(value);
        }
    }

    public void SetIsOnWithoutNotify(bool value) {

        if (toggle)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
        var helpers = toggle.group.GetComponentsInChildren<UToggleHelper>();
        foreach (var helper in helpers)
        {
            helper.Refresh();
        }
    }

    public void SetOn(bool isOn) {
        if (toggle)
        {
            toggle.isOn = isOn;
        }
        this.SetFrameCallBack(1, ()=>{
            if (toggle != null)
            {
                var helpers = toggle.group.GetComponentsInChildren<UToggleHelper>();
                foreach (var helper in helpers)
                {
                    helper.Refresh();
                }
            }

        });


    }

    private void OnEnable() {
        Refresh();
    }

    public void Refresh() {
        if (checkObj != null && toggle != null)
        {
            checkObj.SetActive(toggle.isOn);
        }
    }

    private void Reset() {
        toggle = GetComponent<Toggle>();
        var checkTrans = GameObjectEx.FindChildByName(transform, "Checkmark");
        if (checkTrans != null)
        {
            checkObj = checkTrans.gameObject;
        }
        if (checkObj != null)
        {
            checkObj.SetActive(toggle.isOn);
        }
    }

}