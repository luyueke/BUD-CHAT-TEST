using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载到 Dropdown 的 Item Template 根节点上，监听 Toggle 的选中状态变化，
/// 同步修改 Item Label 文字颜色，实现选中/未选中两种不同文字颜色。
/// </summary>
public class DropdownItemTextColor : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;          // Item 上的 Toggle 组件
    [SerializeField] private Text _label;             // Item Label 文字组件
    [SerializeField] private Color _normalColor;      // 未选中时的文字颜色
    [SerializeField] private Color _selectedColor;    // 选中时的文字颜色

    private void Awake()
    {
        if (_toggle == null)
        {
            _toggle = GetComponent<Toggle>();
        }

        if (_label == null)
        {
            _label = GetComponentInChildren<Text>();
        }

        if (_toggle != null)
        {
            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    private void Start()
    {
        // 初始化时同步一次颜色，确保默认选中项显示正确
        if (_toggle != null && _label != null)
        {
            _label.color = _toggle.isOn ? _selectedColor : _normalColor;
        }
    }

    /// <summary>
    /// Toggle 选中状态变更时同步文字颜色。
    /// </summary>
    /// <param name="isOn">是否选中</param>
    private void OnToggleValueChanged(bool isOn)
    {
        if (_label != null)
        {
            _label.color = isOn ? _selectedColor : _normalColor;
        }
    }

    private void OnDestroy()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }
}
