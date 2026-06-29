// @Author: YangJie
// @Description: 模版数据管理类, 包括地图、素材、衣服、帽子、材质、图案等
// @Date:  2023/07/28
// @Modify:

using System.Collections.Generic;
using Es;

namespace GameData.Managers
{


    public enum TemplateType
    {
        Map = 10000,
        UgcItem = 20000, // 素材
        Clothes = 30000,
        Hat = 40000,
        Material = 50000,
        Pattern = 60000,
    }


    public class TemplateManager : GlobalInstance<TemplateManager>
    {
        public List<MapTemplate> GetAllMapTemplates()
        {
            return DataTables.GetMapTemplateList();
        }

        public MapTemplate GetMapTemplateById(string id)
        {
            return DataTables.GetMapTemplate(id);
        }

        public List<PropTemplate> GetAllProps()
        {
            return DataTables.GetPropTemplateList();
        }

        public PropTemplate GetPropById(string id)
        {
            return DataTables.GetPropTemplate(id);
        }
    }
}
