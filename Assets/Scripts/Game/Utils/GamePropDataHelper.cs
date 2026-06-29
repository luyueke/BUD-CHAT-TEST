using GameData.Config;
using System.Collections.Generic;
namespace Game.Utils
{
    public static class GamePropDataHelper
    {
        public static List<Es.GamePropData> GetPropDataList()
        {
            return Es.DataTables.GetGamePropDataList();
        }

        public static Es.GamePropData GetPropDataByID(string id)
        {
            return Es.DataTables.GetGamePropData(id);
        }

        public static NodeModelType GetNodeModelTypeByID(string id)
        {
            var propData = Es.DataTables.GetGamePropData(id);
            return (NodeModelType)propData.ModelType;
        }

        /// <summary>
        /// 根据ModeType获取道具配置。
        /// 1. 默认只会获取第一个找到的道具。
        /// 2. 用于可以使用ModeType唯一标识的道具
        /// </summary>
        public static Es.GamePropData GetPropIdByNodeModelType(NodeModelType modelType)
        {
            var propDataList = Es.DataTables.GetGamePropDataList();
            var modelInt = (int)modelType;
            return propDataList.Find(x=>x.ModelType == modelInt);
        }
    }
}