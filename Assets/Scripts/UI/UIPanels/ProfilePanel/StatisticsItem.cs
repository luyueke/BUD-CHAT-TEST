using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class StatisticsItem : MonoBehaviour
    {
        [SerializeField] private Text NumText;

        [SerializeField] private Text DescText;
        
        

        public void SetNum(int num)
        {
            NumText.SetText(num + "");
        }
    }
}
