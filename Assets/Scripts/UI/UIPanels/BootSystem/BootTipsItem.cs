using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class BootTipsItem : MonoBehaviour
{
    public RectTransform iconsPerent;
    public GameObject iconObj;
    public Text txt;
    TipsItemData _data;

    public void SetData(TipsItemData data)
    {
        _data = data;
        if (_data.icon != null)
        {
            txt.gameObject.SetActive(true);
            foreach (var id in _data.icon)
            {
                GameObject clonedTextObject = GameObject.Instantiate(iconObj);
                clonedTextObject.SetActive(true);
                clonedTextObject.transform.SetParent(iconsPerent, false);
                clonedTextObject.GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(id, clonedTextObject);
            }
        }
        else
        {
            iconsPerent.gameObject.SetActive(false);
        }
        if (txt.text != null)
        {
            txt.text = _data.text;
            txt.gameObject.SetActive(true);
        }
        else
        {
            txt.gameObject.SetActive(false);
        }
        
    }
}
