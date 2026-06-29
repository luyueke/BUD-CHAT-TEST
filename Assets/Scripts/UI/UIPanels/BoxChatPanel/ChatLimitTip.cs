using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ChatLimitTip : MonoBehaviour
    {
        public Button button;

        public Action ac;
        private void Awake()
        {
            button.onClick.AddListener(OnButton);
        }




        void OnButton() { 
            ac?.Invoke();
        }





    }
}