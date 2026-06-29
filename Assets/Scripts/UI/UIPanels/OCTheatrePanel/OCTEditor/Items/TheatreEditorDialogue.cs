using System;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorDialogue : MonoBehaviour
{
    [SerializeField] private SuperTextMesh text;
    [SerializeField] private GameObject hint;
    [SerializeField] private Button selectBtn;
    [SerializeField] private GameObject selectedObj;

    public Action<TheatreEditorDialogue> onSelected;

    public void OnCreate()
    {
        selectBtn?.onClick.AddListener(() => onSelected?.Invoke(this));
        RefreshHint();
    }

    public SuperTextMesh GetText()
    {
        return text;
    }

    public void SetText(string t)
    {
        if (text != null) text.text = t;
        RefreshHint();
    }

    private void RefreshHint()
    {
        bool isEmpty = text == null || string.IsNullOrEmpty(text.text);
        hint?.SetActive(isEmpty);
        if (text != null) text.gameObject.SetActive(!isEmpty);
    }

    public void SetTextColor(Color color)
    {
        if (text != null) text.color = color;
    }
}
