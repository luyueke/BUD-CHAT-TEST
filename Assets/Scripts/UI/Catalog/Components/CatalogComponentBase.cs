using UnityEngine;

namespace UI.Catalog.Components
{
    /// <summary>
    /// 目录组件基类，所有目录面板中的组件都应继承此类
    /// </summary>
    public abstract class CatalogComponentBase : MonoBehaviour
    {
        public abstract void updateUI(Gallery gallery);


    }
}
