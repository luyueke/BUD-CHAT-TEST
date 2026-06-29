
using Game.ECS;

namespace Game.Base
{
    public class InterfaceNotifyUtil
    {
        public static void OnCreateNode(NodeBaseBehaviour nodeBehaviour, NodeCreateType createType)
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeLife)
                    {
                        var editManager = (INodeLife)item;
                        editManager.OnCreateNode(nodeBehaviour,createType);
                    }
                }
            }
        }

        public static void OnCloneNode(NodeBaseBehaviour oldBehv, NodeBaseBehaviour newBehv)
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeLife)
                    {
                        var editManager = (INodeLife)item;
                        editManager.OnCloneNode(oldBehv,newBehv);
                    }
                }
            }
        }


        public static void OnRemoveNode(NodeBaseBehaviour nodeBehaviour)
        {
            //通知需要监听道具移除的管理器：如自定义碰撞体属性、发光属性、开关控制等
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeLife)
                    {
                        var editManager = (INodeLife)item;
                        editManager.OnRemoveNode(nodeBehaviour);
                    }
                }
            }
        }


        public static void OnRevertNode(NodeBaseBehaviour nodeBehaviour)
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeLife)
                    {
                        var editManager = (INodeLife)item;
                        editManager.OnRevertNode(nodeBehaviour);
                    }
                }
            }
        }

        public static void OnSelectNode(NodeBaseBehaviour nodeBehaviour)
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeGlobalSelect)
                    {
                        var editManager = (INodeGlobalSelect)item;
                        editManager.OnSelectNode(nodeBehaviour);
                    }
                }
            }
        }

        public static void OnUnSelectNode(NodeBaseBehaviour nodeBehaviour)
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeGlobalSelect)
                    {
                        var editManager = (INodeGlobalSelect)item;
                        editManager.OnUnSelectNode(nodeBehaviour);
                    }
                }
            }
        }

        public static void OnUnSelectAll()
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is INodeGlobalSelect)
                    {
                        var editManager = (INodeGlobalSelect)item;
                        editManager.OnUnSelectAll();
                    }
                }
            }
        }

        public static void OnCombine(SceneEntity entity)
        {
            var cInstancs = GameInstanceManager.GetAllInstances();
            if (cInstancs != null && cInstancs.Count > 0)
            {
                foreach (var item in cInstancs.Values)
                {
                    if (item is ICombine)
                    {
                        var editManager = (ICombine)item;
                        editManager.OnCombine(entity);
                    }
                }
            }
        }
    }
}

