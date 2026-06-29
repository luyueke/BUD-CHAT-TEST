using Sirenix.OdinInspector.Editor;
using UnityEditor;

public class AuditToolEditor : OdinEditorWindow {


    [MenuItem("BudTools/审核工具", false, 0)]
    private static void Open()
    {
        GetWindow<AuditToolEditor>().Show();
    }


}
