/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-07 15:20:43
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-07 15:22:40
 * @ Description: 用于通知全局选择道具的监听接口。
 */

namespace Game.Base
{
    public interface INodeGlobalSelect
    {
        void OnSelectNode(NodeBaseBehaviour nodeBehaviour); // 选中节点回调
        void OnUnSelectNode(NodeBaseBehaviour nodeBehaviour); // 取消选中节点回调
        void OnUnSelectAll(); // 取消所有的选中
    }
}