using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SwitchLine : MonoBehaviour
{

    public Button switchHandBtn;
    public Action switchHandAction;

    public void InitUI(Action switchAction)
    {
        switchHandBtn.onClick.AddListener(OnClickSwitchHand);
        this.switchHandAction = switchAction;
    }
    
    public void OnClickSwitchHand()
    {
        switchHandAction?.Invoke();
    }


}