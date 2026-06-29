using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorOptions : MonoBehaviour
{
    [SerializeField] private SuperTextMesh text;
    [SerializeField] private Button selectBtn;
    [SerializeField] private TheatreEditorOptionItem optionPrefab;
    [SerializeField] private Transform optionRoot;
    [SerializeField] private GameObject OptionTextHint;
    [SerializeField] private GameObject OnSelected;

    public System.Action<TheatreEditorOptions> onSelected;
    // (optionIndex, currentText) — fired when individual option text button is clicked
    public System.Action<int, string> onOptionTextEditRequested;
    // (jumpIndex) — fired when individual option jump button is clicked
    public System.Action<int> onOptionJumpRequested;

    private readonly List<TheatreEditorOptionItem> optionItems = new();

    public void OnCreate()
    {
        OnSelected?.SetActive(false);
        OptionTextHint?.SetActive(true);
        if (text != null) text.text = "";
        selectBtn?.onClick.RemoveAllListeners();
        selectBtn?.onClick.AddListener(() => onSelected?.Invoke(this));
    }

    public void RefreshFromData(POCTheatreDialogue optionDialogue, System.Func<int, string> jumpTextResolver = null)
    {
        foreach (var item in optionItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        optionItems.Clear();

        if (optionDialogue == null || optionPrefab == null || optionRoot == null) return;

        if (text != null)
            text.text = string.IsNullOrEmpty(optionDialogue.Text) ? "" : optionDialogue.Text;

        OptionTextHint?.SetActive(optionDialogue.Options.Count == 0);

        int idx = 0;
        foreach (var option in optionDialogue.Options)
        {
            var obj = Instantiate(optionPrefab.gameObject, optionRoot);
            var item = obj.GetComponent<TheatreEditorOptionItem>();
            item.OnCreate();
            string jumpText = (option.JumpIndex > 0 && jumpTextResolver != null)
                ? jumpTextResolver.Invoke(option.JumpIndex)
                : null;
            item.SetData(option, jumpText);

            int capturedIdx = idx;
            var capturedOption = option;
            item.onSelected = _ => onOptionTextEditRequested?.Invoke(capturedIdx, capturedOption.Text);
            item.onJumpClicked = _ => onOptionJumpRequested?.Invoke(capturedOption.JumpIndex);

            optionItems.Add(item);
            obj.SetActive(true);
            idx++;
        }
    }
}
