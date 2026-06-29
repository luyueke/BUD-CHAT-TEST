using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarItem : MonoBehaviour
{
    public Image image;
    
    public void SetProgress(float ratio)
    {
        if (image)
        {
            image.fillAmount = Mathf.Clamp01(ratio);
        }
    }
    
    public void SetProgress(float cur, float max)
    {
        float ratio = max > 0f ? cur / max : 0f;
        SetProgress(ratio);
    }
}
