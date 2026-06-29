using UnityEngine;

namespace UI.Base
{
    public class UIOrderHelper
    {
        #region UI Panel 层级

        public static void SetPanelOrder(BasePanel p)
        {
            if (p == null || p.Config == null) return;
            var cfg = p.Config;
            var index = GetLastIndexByOrder(cfg.Order, p.transform);
            p.transform.SetSiblingIndex(index);
        }

        public static int GetLastIndexByOrder(int order, Transform t)
        {
            var root = t.parent;
            var transList = root.gameObject.GetAllChildren(true, true, new[] { t });
            if (transList is not { Count: > 0 })
            {
                return 0;
            }

            for (var i = transList.Count - 1; i >= 0; i--)
            {
                var child = transList[i];
                if (child == null) continue;

                var p = child.GetComponent<BasePanel>();
                if (p != null && p.Config != null && (int)p.Config.Order <= order)
                {
                    return transList.IndexOf(child) + 1;
                }
            }

            return 0;
        }

        #endregion
        
        #region UI Window层级

        // private void SetWindowOrder(BaseWindow window)
        // {
        //     if (window == null || window.Config == null) return;
        //     var cfg = window.Config;
        //     var index = GetLastIndexByOrder(cfg.Order, window.transform);
        //     window.transform.SetSiblingIndex(index);
        // }
        //
        // private int GetLastIndexByOrder(int order, Transform t)
        // {
        //     var root = t.parent;
        //     var transList = root.gameObject.GetAllChildren(true, true, new[] { t });
        //     if (transList is not { Count: > 0 })
        //     {
        //         return 0;
        //     }
        //
        //     for (var i = transList.Count - 1; i >= 0; i--)
        //     {
        //         var child = transList[i];
        //         if (child == null) continue;
        //
        //         var window = child.GetComponent<BaseWindow>();
        //         if (window != null && window.Config != null && (int)window.Config.Order <= order)
        //         {
        //             return transList.IndexOf(child) + 1;
        //         }
        //     }
        //
        //     return 0;
        // }
        //
        // private int GetFirstIndexByOrder(int order, Transform t)
        // {
        //     var root = t.parent;
        //     var allChilds = root.gameObject.GetAllChildren(true, true, new[] { t });
        //     if (allChilds == null || allChilds.Count <= 0)
        //     {
        //         return 0;
        //     }
        //
        //     for (int i = 0; i < allChilds.Count; i++)
        //     {
        //         var child = allChilds[i];
        //         if (child == null)
        //         {
        //             continue;
        //         }
        //
        //         var window = child.GetComponent<BaseWindow>();
        //         if (window != null && window.Config != null && window.Config.Order >= order)
        //         {
        //             return allChilds.IndexOf(child);
        //         }
        //     }
        //
        //     return 0;
        // }

        #endregion

    }
}