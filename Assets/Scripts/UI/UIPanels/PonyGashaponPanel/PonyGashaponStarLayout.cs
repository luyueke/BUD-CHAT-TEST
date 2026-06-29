using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PonyGashaponStarLayout : MonoBehaviour
{
    public List<PonyGashaponStarCom> ponyGashaponStarComList;
    void Start()
    {
        for(int i = 0; i < ponyGashaponStarComList.Count; i++)
        {
            ponyGashaponStarComList[i].SetStar(false);
        }
    }

    public void SetStarCount(int count)
    {
        for(int i = 0; i < ponyGashaponStarComList.Count; i++)
        {
            ponyGashaponStarComList[i].SetStar(i < count);
        }
    }

}
