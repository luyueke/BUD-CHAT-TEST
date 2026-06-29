/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-05 15:25:57
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-05 15:40:37
 * @ Description: 用来保存编辑过程中一些数据，退出编辑就会清理
 */

namespace GameData
{
    public class EditorSessionData
    {
        public bool GamePropertyEditViewExpandState = true; // 属性View 的展开状态
        public bool GamePropertyEditPanelFoldState = false; // 属性Panel 的折叠状态
    }
}
