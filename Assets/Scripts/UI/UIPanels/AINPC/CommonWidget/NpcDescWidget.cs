using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NpcDescWidget : MonoBehaviour
{
    public Text Txt_Desc;
    
    public void SetDescription(string des)
    {
        Txt_Desc.SetLocalText(des);
    }
}
