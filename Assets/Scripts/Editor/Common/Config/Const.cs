using System.IO;
using UnityEngine;

namespace common.editor
{
    public static class Const
    {
    #region 道具工具
        public const string UIPropMenu = "BudTools/道具工具/";
        public static string ScriptPath = Path.Combine(Application.dataPath, "Scripts");
        public static string PropConfigPath = Path.Combine(ScriptPath, "GameData", "MapData", "Config");
        public static string ComponentIdFile = Path.Combine(PropConfigPath, "NodeComponentId.cs");
        public static string NodeModelTypeFile = Path.Combine(PropConfigPath, "NodeModelType.cs");

        public static string PropLogicPath = Path.Combine(ScriptPath, "Game", "Props");
        public static string PropLogicBehaviourPath = Path.Combine(ScriptPath, "Game", "Props", "PropsBehaviours");
        public static string PropLogicComponentPath = Path.Combine(ScriptPath, "Game", "Props", "PropsComponents");
        public static string PropLogicManagerPath = Path.Combine(ScriptPath, "Game", "Props", "PropsManagers");

        public static string PropGeneratedPath = Path.Combine(ScriptPath, "Game", "Generated");
        public static string PropManagerRegisterFile = Path.Combine(PropGeneratedPath, "EntityManagerRegister.cs");
        public static string PropComponentRegisterFile = Path.Combine(PropGeneratedPath, "EntityComponentRegister.cs");
    #endregion

    #region Pb工具
        public const string UIPBMenu = "BudTools/PB工具";
    #endregion
    }
}