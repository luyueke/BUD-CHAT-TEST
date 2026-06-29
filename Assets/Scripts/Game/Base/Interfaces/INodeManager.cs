/**
* @ Author: Jun Zhou
* @ Create Time: 2023-07-17 14:52:22
* @ Modified by: Jun Zhou
* @ Modified time: 2023-07-18 17:54:37
* @ Description: 道具流程控制基础接口
*/

using Game.Config;
using Game.ECS;

namespace Game.Base
{
    public interface INodeManager
    {
        void OnPrepareData(SceneEntity entity); // 数据准备阶段
        GameGlobalEnum.NodeOpReason CheckCreateCondition(string propId); // 判断可以创建的条件
        GameGlobalEnum.NodeOpReason CheckRemoveCondition(string propId); // 判断可以删除的条件
        void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour); // 克隆完成
        void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType); // 创建完成
        void OnRemoveNode(NodeBaseBehaviour nodeBehaviour); // 移除回调
        void OnRevertNode(NodeBaseBehaviour nodeBehaviour);

        void Release();

    }
}