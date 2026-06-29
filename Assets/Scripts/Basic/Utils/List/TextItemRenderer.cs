using UnityEngine;
using System.Collections;
using UnityEngine.UI;
#pragma warning disable 649
namespace Fsbm.Runtime
{
    public class TextItemRenderer : ItemRenderer
    {
        [SerializeField]
        private Text _textField;

        protected override void UpdateView()
        {
            base.UpdateView();
            if (_textField != null)
            {
                if(data!=null)
                    _textField.text = data.ToString();
            }
                
        }
    }
}
