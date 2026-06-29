using UnityEngine;
using System.Collections;
namespace Fsbm.Runtime
{
    /// <summary>
    /// 排除布局
    /// </summary>
    public class IgnoreLayout : MonoBehaviour
    {
        protected void OnEnable()
        {
            InvalidLayout();
        }

        protected void OnDisable()
        {
            InvalidLayout();
        }
        protected void OnDestroy()
        {
            InvalidLayout();
        }
        private void InvalidLayout()
        {
            if (transform.parent != null)
            {
                BaseLayout layout = transform.parent.GetComponent<BaseLayout>();
                if (layout != null)
                {
                    if (layout.isAutoLayout)
                        layout.InvalidView();
                }
            }
        }
    }
}
