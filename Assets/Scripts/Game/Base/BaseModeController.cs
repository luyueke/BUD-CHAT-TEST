// @Author: YangJie
// @Description:
// @Date:  2023/07/17
// @Modify:

using System.Collections.Generic;

namespace Game.Base
{
    public abstract class BaseModeController
    {

        protected bool isInitialized = false;

        protected virtual void InitMode()
        {
            isInitialized = true;
        }

        public virtual void EnterMode()
        {
            if (!isInitialized)
            {
                InitMode();
            }

        }
        
        public virtual void LeaveMode()
        {
            
        }
        
        
        protected virtual List<IModeManager> GetAllModeManager()
        {
            List<IModeManager> nodeManagers = new List<IModeManager>();

            var cInstances = GameInstanceManager.GetAllInstances();
            if (cInstances != null && cInstances.Count > 0)
            {
                foreach (var item in cInstances.Values)
                {
                    if (item is IModeManager modeManager && !nodeManagers.Contains(modeManager))
                    {
                        nodeManagers.Add(modeManager);
                    }
                }
            }
            var gInstances = GlobalInstanceManager.GetAllInstances();
            if (gInstances != null && gInstances.Count > 0)
            {
                foreach (var item in gInstances.Values)
                {
                    if (item is IModeManager modeManager && !nodeManagers.Contains(modeManager))
                    {
                        nodeManagers.Add(modeManager);
                    }
                }
            }
            
            var nodeInstances = GlobalNodeManager.Inst.GetAll();
            if (nodeInstances != null && nodeInstances.Count > 0)
            {
                foreach (var item in nodeInstances)
                {
                    if (item is IModeManager modeManager && !nodeManagers.Contains(modeManager))
                    {
                        nodeManagers.Add(modeManager);
                    }
                }
            }
            
            
            
            return nodeManagers;
        }

    }
}