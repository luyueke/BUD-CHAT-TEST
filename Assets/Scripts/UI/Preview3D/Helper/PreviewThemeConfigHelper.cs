using Es;
using UI.Preview3D.Base;
using UnityEngine;

namespace UI.Preview3D.Helper
{
    public class PreviewThemeConfigHelper
    {
        public static PreviewThemeConfig GetPreviewThemeConfig(string themeId)
        {
            return Es.DataTables.GetPreviewThemeConfig(themeId);
        }
    }
}