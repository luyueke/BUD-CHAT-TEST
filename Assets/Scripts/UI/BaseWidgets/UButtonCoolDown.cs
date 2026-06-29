using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UButtonCoolDown:ButtonEventListener
{
    [Header("是否开启重复点击拦截")]
    public bool HasCoolDown = false;
    [Header("按钮点击冷却时间")]
    public float CoolDownTime = 2f;
    //处于冷却状态
    private bool _isUnderCoolDown = false;
    
    private void Start() 
    {
        AddClickEventHandler((go, eventData) => { ButtonClickEffect(go, eventData); });
        AddPointerDownHandler((go, eventData) => { ButtonDownEffect(go, eventData); });
        AddPointerUpHandler((go, eventData) => { ButtonUpEffect(go, eventData); });
    }

    public virtual void ButtonClickEffect(GameObject go, BaseEventData eventData)
    {
        if(!CheckCanClick(go))
            return;

        if (go != null && go.activeInHierarchy)
        {
            _isUnderCoolDown = true;
            // 使按钮不可交互
            SetButtonClickAble(false);
            // 开始按钮冷却倒计时
            StartCoroutine(Cooldown());
        }
    }

    public virtual void ButtonDownEffect(GameObject go, BaseEventData eventData)
    {
        if(!CheckCanClick(go))
            return;
        
    }

    public virtual void ButtonUpEffect(GameObject go, BaseEventData eventData)
    {
        if(!CheckCanClick(go))
            return;
        
    }

    private bool CheckCanClick(GameObject go)
    {
        bool canClick = true;
        
        if (go == null) 
            canClick = false;
        
        if(HasCoolDown = false)
            canClick = false;
        
        if(_isUnderCoolDown)
            canClick = false;

        return canClick;
    }
    
    private IEnumerator Cooldown()
    {
        // 等待冷却时间
        yield return new WaitForSeconds(CoolDownTime); 
            
        // 使按钮可以交互
        SetButtonClickAble(true);

        _isUnderCoolDown = false;
    }

    private void SetButtonClickAble(bool state)
    {
        var cBtn = this.GetComponent<CButton>();
        if (cBtn != null)
        {
            cBtn.SetClickAble(state);
        }
        else
        {
            var button = this.GetComponent<Button>();
            if (button != null)
                button.interactable = state;
        }
    }
}

