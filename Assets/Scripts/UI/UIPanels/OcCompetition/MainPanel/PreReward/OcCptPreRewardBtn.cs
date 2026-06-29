using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCptPreRewardBtn : MonoBehaviour
    {
        public Button Btn;

        public string Title;

        public string Content;

        public int Idx;

        public Action<string,string,int> ac;
        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);
        }


        private void OnBtn() 
        {
            ac?.Invoke(Title,Content,Idx);
        }
    }
}