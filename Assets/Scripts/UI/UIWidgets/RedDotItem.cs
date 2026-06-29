using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class RedDotItem : MonoBehaviour
{
    [SerializeField]private CText reddotText;
    public bool isShowNum;
    public void SetRedDotNum(int reddotNum)
    {
        if (reddotNum<=0)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        if (isShowNum)
        {
            string redotNumText = reddotNum > 99 ? "99+" : reddotNum.ToString();
            reddotText.text = redotNumText;
        }
        else
        {
            reddotText.text = "";
        }
        var layout = GetComponent<HorizontalLayoutGroup>();
        layout.enabled = false;
        layout.enabled = true;
    }
}
