using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] Button _button;
    protected TabGroup _tabGroup;
    protected int _index;
    protected bool _isSelected = false;

    internal virtual void Initialize(TabGroup tabGroup, int index)
    {
        _tabGroup = tabGroup;
        _index = index;
        _button.onClick.AddListener(OnClick);
    }

    internal virtual void OnSelected(bool value)
    {
        if(_isSelected != value)
        {
            _isSelected = value;
            _button.interactable = !value;
        }
    }

    protected virtual void OnClick()
    {
        _tabGroup.Select(_index);
    }

    public Button GetButton()
    {
        return _button;
    }
}
