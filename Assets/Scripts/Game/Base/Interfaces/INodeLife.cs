using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Base
{
    public interface INodeLife
    {
        void OnCloneNode(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour); // 克隆完成
        void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType); // 创建完成
        void OnRemoveNode(NodeBaseBehaviour nodeBehaviour); // 移除回调
        void OnRevertNode(NodeBaseBehaviour nodeBehaviour); // 回滚回调，从undo池子回到游戏
    }
}
