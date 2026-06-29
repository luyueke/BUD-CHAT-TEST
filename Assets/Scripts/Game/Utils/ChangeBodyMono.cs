using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChangeBodyMono : MonoBehaviour
{
    public Transform oldp;
    public Transform newp;
    
    public void SetTrans()
    {
        var oldChilds = oldp.GetComponentsInChildren<Transform>(true);
        var newc = newp.GetComponentsInChildren<Transform>(true);
        var newChilds = newc.ToList();
        for (int i = 0; i < oldChilds.Length; i++)
        {
            var trans = newChilds.Find(x => x.name == oldChilds[i].name);
            if(trans)
            {
                trans.localPosition = oldChilds[i].localPosition;
                trans.localRotation = oldChilds[i].localRotation;
                trans.localScale = oldChilds[i].localScale;
            }
            else
            {
                Debug.LogError(oldChilds[i].name);
            }
        }
    }
}
