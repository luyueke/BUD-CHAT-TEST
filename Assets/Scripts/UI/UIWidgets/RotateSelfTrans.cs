using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    public class RotateSelfTrans : MonoBehaviour {
        public float speed = 10;
        public bool isClockwise = true;
        public bool isRotate = false;

        void Update()
        {
            if (isRotate)
            {
                transform.Rotate(0, 0, speed * Time.deltaTime * (isClockwise ? 1 : -1));
            }
        }
    }
}
