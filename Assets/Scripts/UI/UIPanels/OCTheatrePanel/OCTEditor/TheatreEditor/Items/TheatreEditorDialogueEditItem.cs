using System;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorDialogueEditItem : MonoBehaviour
{
    [SerializeField] private Button editContentBtn;
    [SerializeField] private Text contentText;
    [SerializeField] private Button optionBtn;
    [SerializeField] private GameObject optionObj;
    [SerializeField] private Button option_order_goUp;
    [SerializeField] private Button option_delete;
    [SerializeField] private Text indexText;

    private bool isOptionShowing;

    public void Init(POCTheatreDialogue dialogue, int index, Action onEditContent, Action onMoveUp, Action onDelete)
    {
        if (contentText != null)
            contentText.text = string.IsNullOrEmpty(dialogue.Text) ? "点击编辑对话内容" : dialogue.Text;
        if (indexText != null)
            indexText.text = (index + 1).ToString();

        optionObj?.SetActive(false);
        isOptionShowing = false;

        editContentBtn?.onClick.RemoveAllListeners();
        editContentBtn?.onClick.AddListener(() => onEditContent?.Invoke());

        optionBtn?.onClick.RemoveAllListeners();
        optionBtn?.onClick.AddListener(() =>
        {
            isOptionShowing = !isOptionShowing;
            optionObj?.SetActive(isOptionShowing);
        });

        option_order_goUp?.onClick.RemoveAllListeners();
        option_order_goUp?.onClick.AddListener(() =>
        {
            optionObj?.SetActive(false);
            onMoveUp?.Invoke();
        });

        option_delete?.onClick.RemoveAllListeners();
        option_delete?.onClick.AddListener(() =>
        {
            optionObj?.SetActive(false);
            onDelete?.Invoke();
        });
    }
}
