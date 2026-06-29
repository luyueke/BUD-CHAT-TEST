using System;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorTriggerTimeOptionItem : MonoBehaviour
{
    [SerializeField] private Button Btn;
    [SerializeField] private Text Number;
    [SerializeField] private Text Content;
    [SerializeField] private GameObject NextLine;

    public void Init(int index, string contentText, bool isLast, Action<int> onSelected)
    {
        if (Number != null) Number.text = index.ToString();
        if (Content != null) Content.text = contentText;
        NextLine?.SetActive(!isLast);

        Btn?.onClick.RemoveAllListeners();
        int capturedIndex = index;
        Btn?.onClick.AddListener(() => onSelected?.Invoke(capturedIndex));
    }
}
