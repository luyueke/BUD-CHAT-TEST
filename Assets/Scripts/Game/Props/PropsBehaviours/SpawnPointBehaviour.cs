
using Game.Base;
using TMPro;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class SpawnPointBehaviour : NodeBaseBehaviour
    {
        public int Index { get; private set; }
        public bool IsDefault{ get; private set; }
        private TextMeshPro textPro;
        private GameObject selectEffect;
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            textPro = this.GetComponentInChildren<TextMeshPro>();
            selectEffect = transform.Find("spawnpoint_ef").gameObject;
        }

        public void SetIndex(int index)
        {
            Index = index;
            textPro.text = index.ToString();
        }

        public void SetDefault(int isDef)
        {
            IsDefault = isDef == 1;
        }

        public void SetEffectActive(bool isEffect)
        {
            selectEffect.SetActive(isEffect);
        }

    }
}
        
