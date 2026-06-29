
using Game.Base;
using TMPro;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class TrapSpawnBehaviour : NodeBaseBehaviour
    {
        TextMeshPro textMeshPro;
        private MeshRenderer[] _meshRenderers;

        public int Index = 0;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            textMeshPro = this.GetComponentInChildren<TextMeshPro>(true);
            _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        }
        
        public void SetIndex(int index)
        {
            textMeshPro.text = index.ToString();
        }

        public void SetRenderEnable(bool value)
        {
            if (_meshRenderers == null)
            {
                _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            }

            if (_meshRenderers != null)
            {
                foreach (var render in _meshRenderers)
                {
                    render.enabled = value;
                }
            }

            
        }
    }
}
        
