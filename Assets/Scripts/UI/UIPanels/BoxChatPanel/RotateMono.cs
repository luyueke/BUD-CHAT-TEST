using System.Collections;
using UnityEngine;

namespace Game
{
    public class RotateMono : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(-Vector3.forward);
        }
    }
}