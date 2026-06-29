using UnityEngine;
using UnityEngine.UI;
public class CabinBtnToggle : MonoBehaviour
{
    public Button button;
    CabinBtnToggleParent _cabinBtnToggleParent;

    GameObject _selectObj;
    int _index;
    public void Init(int index,CabinBtnToggleParent cabinBtnToggleParent)
    {
        _cabinBtnToggleParent = cabinBtnToggleParent;
        button.onClick.AddListener(OnClick);
        _selectObj = transform.Find("selectImg").gameObject;
        _index = index;
    }
    private void OnClick()
    {
        _cabinBtnToggleParent.OnClick(this,_index);
    }

    public void Select()
    {
        _selectObj.SetActive(true);
    }
    public void UnSelect()
    {
        _selectObj.SetActive(false);
    }
}
