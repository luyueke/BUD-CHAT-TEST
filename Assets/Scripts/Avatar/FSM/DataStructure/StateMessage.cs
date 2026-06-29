// @Author: YangJie
// @Description:
// @Date:  2023/09/05
// @Modify:

namespace Game.Avatar.FSM.DataStructure
{
    public class StateMessage
    {
        /// <summary>
        /// 进入水中
        /// </summary>
        public const string StateEnterWater = "StateEnterWater";
        
        /// <summary>
        /// 在水中状态切换
        /// </summary>
        public const string StateWaterChange = "StateWaterChange";
        
        /// <summary>
        /// 离开水中
        /// </summary>
        public const string StateExitWater = "StateExitWater";
    }
}