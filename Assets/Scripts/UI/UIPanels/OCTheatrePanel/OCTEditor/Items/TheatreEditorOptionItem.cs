using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorOptionItem : MonoBehaviour
{
    [SerializeField] private SuperTextMesh text;
    [SerializeField] private GameObject OptionHint;
    [SerializeField] private Button OptionTextBtn;
    [SerializeField] private Button JumpSectionBtn;
    [SerializeField] private Text JumpSectionText;
    [SerializeField] private Text JumpIndexText;
    [SerializeField] private GameObject SelectedObj;
    [SerializeField] private GameObject HintObj;

    public System.Action<TheatreEditorOptionItem> onSelected;
    public System.Action<TheatreEditorOptionItem> onJumpClicked;

    public Pb.Theatre.POCTheatreOption OptionData { get; private set; }

    public void OnCreate()
    {
        OptionTextBtn?.onClick.RemoveAllListeners();
        OptionTextBtn?.onClick.AddListener(() => onSelected?.Invoke(this));
        JumpSectionBtn?.onClick.RemoveAllListeners();
        JumpSectionBtn?.onClick.AddListener(() => onJumpClicked?.Invoke(this));
    }

    public void SetData(Pb.Theatre.POCTheatreOption option, string jumpFirstText = null)
    {
        OptionData = option;
        if (text != null)
        {
            string display = string.IsNullOrEmpty(option.Text) ? "" : option.Text;
            if (display.Length > 8) display = display.Substring(0, 8) + "...";
            text.text = display;
        }
        OptionHint?.SetActive(string.IsNullOrEmpty(option.Text));

        bool hasJump = option.JumpIndex > 0;
        if (JumpSectionText != null)
        {
            if (hasJump)
            {
                string preview = string.IsNullOrEmpty(jumpFirstText) ? "未配置文本" : jumpFirstText;
                if (preview.Length > 8) preview = preview.Substring(0, 8) + "...";
                JumpSectionText.text = $"[{option.JumpIndex}].{preview}";
            }
            else
            {
                JumpSectionText.text = "";
            }
        }
        if (JumpIndexText != null) JumpIndexText.text = hasJump ? option.JumpIndex.ToString() : "？";
        HintObj?.SetActive(!hasJump);
    }
}
