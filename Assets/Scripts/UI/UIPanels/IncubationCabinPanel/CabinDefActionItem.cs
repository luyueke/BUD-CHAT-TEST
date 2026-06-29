using Es;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CabinDefActionItem : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Button btn;
    [SerializeField] private Image emoUIImage;
    [SerializeField] private Text emoUIText;
    [SerializeField] private Text coverText;
    [SerializeField] private GameObject selectedMark;

    private CabinDefActionConfig _config;
    private Action<CabinDefActionConfig, CabinDefActionItem> _onSelect;

    public void Init(CabinDefActionConfig config, Action<CabinDefActionConfig, CabinDefActionItem> onSelect, string text)
    {
        _onSelect = onSelect;
        btn.onClick.AddListener(OnClick);

        _config = config;
        if (_config == null)
        {
            return;
        }
        var emoUIConfig = DataTables.GetEmoUIConfig(_config.EmoUIID);
        if (emoUIConfig == null)
        {
            return;
        }
        nameText.text = text;
        emoUIImage.sprite = PgcUtils.GetIconSpriteByPgcId(_config.EmoUIID, emoUIImage.gameObject);
        emoUIText.text = emoUIConfig.name;
        coverText.text = config.Cover;

        SetSelected(false);

    }

    private void OnClick()
    {
        _onSelect?.Invoke(_config, this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
        {
            selectedMark.SetActive(selected);
        }
    }
}
