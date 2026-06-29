using System;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorOptionEditItem : MonoBehaviour
{
    [SerializeField] private Button optionBtn;
    [SerializeField] private Text optionText;
    [SerializeField] private Button editBtn;
    [SerializeField] private GameObject optionObj;
    [SerializeField] private Button option_order_goUp;
    [SerializeField] private Button option_delete;
    [SerializeField] private Text jumpInfoText;

    private bool isOptionShowing;

    public void Init(POCTheatreOption option, int index, Action onEdit, Action onMoveUp, Action onDelete)
    {
        if (optionText != null)
            optionText.text = string.IsNullOrEmpty(option.Text) ? "点击编辑选项内容" : option.Text;
        if (jumpInfoText != null)
            jumpInfoText.text = option.JumpIndex > 0 ? $"-> {option.JumpIndex}" : "-> END";

        optionObj?.SetActive(false);
        isOptionShowing = false;

        optionBtn?.onClick.RemoveAllListeners();
        optionBtn?.onClick.AddListener(() =>
        {
            isOptionShowing = !isOptionShowing;
            optionObj?.SetActive(isOptionShowing);
        });

        editBtn?.onClick.RemoveAllListeners();
        editBtn?.onClick.AddListener(() => { optionObj?.SetActive(false); onEdit?.Invoke(); });

        option_order_goUp?.onClick.RemoveAllListeners();
        option_order_goUp?.onClick.AddListener(() => { optionObj?.SetActive(false); onMoveUp?.Invoke(); });

        option_delete?.onClick.RemoveAllListeners();
        option_delete?.onClick.AddListener(() => { optionObj?.SetActive(false); onDelete?.Invoke(); });
    }
}
