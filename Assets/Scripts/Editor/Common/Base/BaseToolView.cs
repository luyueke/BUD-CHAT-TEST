/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-09 11:19:44
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-24 11:38:54
 * @ Description:
 */

using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

namespace common.editor
{
    public abstract class BaseToolView
    {
        protected OdinMenuEditorWindow window;

        protected BaseToolView(OdinMenuEditorWindow window)
        {
            this.window = window;
        }
    }
}