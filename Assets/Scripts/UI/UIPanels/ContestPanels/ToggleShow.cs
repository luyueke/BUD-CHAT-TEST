
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ToggleShow : MonoBehaviour
{
    public GameObject OnNode;
    public GameObject OffNode;

    private void Awake()
    {
        GetComponent<Toggle>().onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(bool isOn)
    {
        if (OnNode != null) OnNode.SetActive(isOn);
        if (OffNode != null) OffNode.SetActive(!isOn);
    }

    public void SetOn(bool bo) {
        OnValueChanged(bo);
    }
}
