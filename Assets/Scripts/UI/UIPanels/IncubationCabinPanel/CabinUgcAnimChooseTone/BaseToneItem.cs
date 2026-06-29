using UnityEngine;
using UnityEngine.UI;

public abstract class BaseToneItem : MonoBehaviour
{
    public Text Txt_Title;
    public Button Btn_Select;
    public GameObject Go_Selected;
    public GameObject Go_Normal;
    public GameObject Go_TryPlay;

    protected virtual void Awake()
    {
        Btn_Select.onClick.AddListener(OnSelectBtnClick);
        Go_Normal.SetActive(true);
        Go_TryPlay.SetActive(false);
    }

    protected abstract void OnSelectBtnClick();

    public virtual void SetSelectState(bool isSelect)
    {
        Go_Selected.SetActive(isSelect);
        Go_Normal.SetActive(!isSelect);
        Go_TryPlay.SetActive(isSelect);
    }
}
