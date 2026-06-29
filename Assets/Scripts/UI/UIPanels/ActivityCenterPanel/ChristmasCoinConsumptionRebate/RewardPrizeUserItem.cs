using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardPrizeUserItem : MonoBehaviour
{
    public Text Txt_Title;

    public void SetData(int index, string userName)
    {
        Txt_Title.text = (index + 1) + "." + userName;
    }
}
