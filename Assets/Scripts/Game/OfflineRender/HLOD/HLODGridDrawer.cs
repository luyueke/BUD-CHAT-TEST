// @Author: YangJie
// @Description:
// @Date:  2023/10/10
// @Modify:

using UnityEngine;

namespace HLOD
{
    public class HLODGridDrawer : MonoBehaviour
    {

        private HLODGrid hlodGrid = null;
        public void SetGrid(HLODGrid grid)
        {
            hlodGrid = grid;
        }
        
        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (hlodGrid != null)
            {
                hlodGrid.DrawGizmos();
            } 
#endif

        }

    }
}