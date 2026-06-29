using Game.ECS;

namespace Game.Base
{
    /// <summary>
    /// 编辑器操作和道具Manager关联
    /// </summary>
    public interface INodeEdit
    {

        /// <summary>
        /// 编辑器内选中道具
        /// </summary>
        public void OnSelectProp(SceneEntity entity);
        
        /// <summary>
        /// 取消选中道具
        /// </summary>
        public void OnUnSelectProp(SceneEntity entity);
        
    }
}