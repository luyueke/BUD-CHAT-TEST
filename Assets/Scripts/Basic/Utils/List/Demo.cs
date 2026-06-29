using Fsbm.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fsbm.Runtime
{
    public class Demo : MonoBehaviour
    {
        public ScrollList ScrollList;
        // Use this for initialization
        void Start()
        {
            var ls = new List<int>();
            for (int i = 0; i < 10; i++) {
                ls.Add(i);
            }
            ScrollList.datas = ls;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}