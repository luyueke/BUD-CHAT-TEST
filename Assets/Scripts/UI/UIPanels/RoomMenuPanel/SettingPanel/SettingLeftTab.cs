using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingLeftTab : MonoBehaviour
{
    private Text content;
    private Transform selected;
    private Transform line;
    public Action<bool> StatusChange;
    public Action OnClick;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            OnClick?.Invoke();
        });
        content = transform.Find("Text").GetComponent<Text>();
        //content.color = colorUnselected;
        selected = transform.Find("Selected");
        selected.gameObject.SetActive(false);
        line = transform.Find("Image");
    }

    public void Init(string text, bool showLine)
    {
        content.SetLocalText(text);
        line.gameObject.SetActive(showLine);
    }

    public void SetSelected(bool selected)
    {
        if (IsSelected() == selected)
        {
            return;
        }
        this.selected.gameObject.SetActive(selected);
        //unselected.gameObject.SetActive(!selected);
        //content.color = selected ? colorSelected : colorUnselected;
        StatusChange(selected);
    }

    public bool IsSelected()
    {
        return selected.gameObject.activeSelf;
    }
}