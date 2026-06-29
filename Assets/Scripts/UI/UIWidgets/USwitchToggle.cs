using UnityEngine;
using UnityEngine.UI;

public class USwitchToggle : Toggle
{
    private GameObject onNode;
    private GameObject offNode;

    public void Init() {
        if(onNode == null) onNode = GameObjectEx.FindChildByName(transform, "On").gameObject;
        if(offNode == null) offNode = GameObjectEx.FindChildByName(transform, "Off").gameObject;
        onValueChanged.AddListener(OnValueChanged);
        OnValueChanged(isOn);
    }

    private void OnValueChanged(bool isOn) {
        onNode.SetActive(isOn);
        offNode.SetActive(!isOn);
    }
}
