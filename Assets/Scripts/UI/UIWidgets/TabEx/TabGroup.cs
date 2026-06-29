using UnityEngine;
using System;

public class TabGroup : MonoBehaviour
{
    public int selectedIndex { get; private set; }
    [SerializeField] TabButton[] _buttons = null;
    Action<int> _onChanged;

    void Awake()
    {
        for (int i = 0; i < buttonCount; ++i)
        {
            if (_buttons[i] != null)
            {
                _buttons[i].Initialize(this, i);
            }
        }
        selectedIndex = -1;
    }

    public void Select(int index)
    {
        int length = buttonCount;
        if (index < 0 || index >= length)
        {
            index = -1;
        }
        if (selectedIndex != index)
        {
            selectedIndex = index;
            for (int i = 0; i < length; ++i)
            {
                _buttons[i].OnSelected(index == i);
            }
            _onChanged?.Invoke(index);
        }
    }

    public void OnChanged(Action<int> func)
    {
        _onChanged = func;
    }

    public TabButton GetButton(int index)
    {
        if(index >= 0 && index < buttonCount)
        {
            return _buttons[index];
        }
        return null;
    }

    private void OnDestroy()
    {
        _onChanged = null;
    }

    public int buttonCount
    {
        get { return _buttons == null ? 0 : _buttons.Length; }
    }
}
