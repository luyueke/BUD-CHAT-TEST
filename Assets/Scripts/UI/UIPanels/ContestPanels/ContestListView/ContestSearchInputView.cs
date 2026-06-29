
using System;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UIElements;

public class ContestSearchInputView : MonoBehaviour
{
    [SerializeField] private TextInputView _inputView;
    [SerializeField] private CButton clearBtn;
    
    public Action ClearAction;
    public Action<string> SearchAction;
    
    private void Awake()
    {
        clearBtn.onClick.AddListener(OnClear);
        _inputView.SetOnInput(onInputValueChange);
    }

    public void SetPlaceholder(string placeholder)
    {
        _inputView.SetPlaceholder(placeholder);
    }

    private void OnClear()
    {
        _inputView.SetInputWithoutNotify("");
        clearBtn.gameObject.SetActive(false);
        ClearAction?.Invoke();
    }

    private void onInputValueChange(string content)
    {
        clearBtn.gameObject.SetActive(content.Length > 0);
        SearchAction?.Invoke(content);
    }
}
