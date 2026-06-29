using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Props
{
    public class DecisionBarUI : MonoBehaviour
    {
        public Image Img_Fill;

        public void SetProgress(float progress)
        {
            Img_Fill.fillAmount = progress;
        }
    }
}