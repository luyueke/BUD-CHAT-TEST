using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PartnerDetailTagItem : MonoBehaviour
{
    [SerializeField] private CButton ItemBtn;
    [SerializeField] private Text TagName;
    [SerializeField] private GameObject Selected;

    public CabinCharacterPackInfo PackInfo { get; private set; }
    private Action<PartnerDetailTagItem> _onSelect;

    void Awake()
    {
        ItemBtn.onClick.AddListener(OnClick);
    }

    public void SetData(string name, CabinCharacterPackInfo packInfo, Action<PartnerDetailTagItem> onSelect)
    {
        TagName.text = name;
        PackInfo = packInfo;
        _onSelect = onSelect;
        SetSelected(false);
    }

    public void SetSelected(bool selected) => Selected.SetActive(selected);

    private void OnClick() => _onSelect?.Invoke(this);
}
