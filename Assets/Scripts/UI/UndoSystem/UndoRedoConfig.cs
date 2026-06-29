namespace UndoSystem
{
    public class UndoHelperName
    {
        public static string TransformUndoHelper = "TransformUndoHelper";
        public static string CreateDestroyUndoHelper = "CreateDestroyUndoHelper";
        public static string BaseMaterialUndoHelper = "BaseMaterialUndoHelper";
        public static string CombineUndoHelper = "CombineUndoHelper";
        public static string LockHideUndoHelper = "LockHideUndoHelper";
        public static string WinConditionUndoHelper = "WinConditionUndoHelper";
        public static string PGCCommonSelectUndoHelper = "PGCCommonSelectUndoHelper";
        public static string ColorCommonSelectUndoHelper = "ColorCommonSelectUndoHelper";
        public static string MatCommonSelectUndoHelper = "MatCommonSelectUndoHelper";
        public static string UGCClothElementUndoHelper = "UGCClothElementUndoHelper";
        public static string UGCClothesCreateDestroyUndoHelper = "UGCClothesCreateDestroyUndoHelper";
        public static string UGCClothDrawUndoHelper = "UGCClothDrawUndoHelper";
        public static string EditTilingUndoHelper = "EditTilingUndoHelper";
        public static string WaterCubeUndoHelper = "WaterCubeUndoHelper";
        public static string CombineNodeSelectUndoHelper = "CombineNodeSelectUndoHelper";
    }

    public class UndoRedoConfig
    {
        public enum CreateUndoMode
        {
            Create,
            Destroy,
            Duplicate,
        }

        public enum CombineUndoMode
        {
            Combine,
            UnCombine
        }

    }
}
