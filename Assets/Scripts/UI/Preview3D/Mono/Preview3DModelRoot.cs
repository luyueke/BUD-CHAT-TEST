using UnityEngine;

namespace UI.Preview3D.Mono
{
    public class Preview3DModelRoot : MonoBehaviour
    {
        public Camera previewCamera;
        public RenderTexture rt;
        public Transform previewModel;

        private void Awake()
        {
        }
    }
}
