using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EmoteSelectItem: MonoBehaviour
{
    public string EmoteId { get; private set; }
    [SerializeField] private CButton filledBtn;
    [SerializeField] private Text normalText;
    [SerializeField] private GameObject selectedIcon;
    [SerializeField] private Text selectedText;
    [SerializeField] private GameObject loading;

    
    private bool interactable;
    public bool Interactable
    {
        get => interactable;
        set
        {
            interactable = value;
            filledBtn.gameObject.SetActive(interactable);
        }
    }

    private bool selected = false;
    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            selectedIcon.gameObject.SetActive(selected);
        }
    }

    private bool selectedLoading;
    public bool SelectedLoading
    {
        get => selectedLoading;
        set
        {
            selectedLoading = value;
            loading.gameObject.SetActive(selectedLoading);
        }
    }
    

    public void SetData(string emoteId, string name)
    {
        EmoteId = emoteId;
        normalText.SetText(name);
        selectedText.SetText(name);
    }

    public void Show(bool isShow)
    {
        gameObject.SetActive(isShow);
    }

    public void AddOnClick(UnityAction<EmoteSelectItem> act)
    {
        filledBtn.onClick.AddListener(() => act?.Invoke(this));
    }
    

    public void OnCompleteLoading()
    {
        //TODO:异步加载
        loading.gameObject.SetActive(false);
    }
}
