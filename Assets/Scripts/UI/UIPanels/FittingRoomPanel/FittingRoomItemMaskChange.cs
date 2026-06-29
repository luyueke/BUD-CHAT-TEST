using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FittingRoomItemMaskChange : MonoBehaviour
{
    public Image Mask;
    public Material Mat1;
    public Material Mat2;

    public void OnEnable()
    {


        if (Mask) Mask.material = Mat1;
    }

    public void OnDisable()
    {
        if (Mask) Mask.material = Mat2;
    }
}
