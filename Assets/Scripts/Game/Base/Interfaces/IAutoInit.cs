// @Author: YangJie
// @Description: 实现该接口的类会在进入编辑及游玩时自动调用Init 方法
// @Date:  2023/07/26
// @Modify:

namespace Game.Base
{
    public interface IAutoInit
    {
        /// <summary>
        /// 该方法在进入游戏中会自动调用
        /// </summary>
        public void Init();
    }
}