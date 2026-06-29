
using UnityEngine;
using UnityEngine.UI;


namespace UI.BaseWidgets
{
    
    /// <summary>
    /// Text基础类
    /// </summary>
    [AddComponentMenu("BudUI/CText", 30)]
    public class CText : Text
    {
        public Font defaultFont;

        public void Reset()
        {
            font = defaultFont;
        }
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        protected override void Start() 
        {
            base.Start();
        }
        
        protected override void OnDestroy()
        {
        }
    }
}


