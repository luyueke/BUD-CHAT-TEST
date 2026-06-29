using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PonyGashaponStarCom : MonoBehaviour
{
    public GameObject emptyGo;
    public GameObject fullGo;

    public void SetStar(bool isFull)
    {
        emptyGo.SetActive(!isFull);
        fullGo.SetActive(isFull);
    }

}
