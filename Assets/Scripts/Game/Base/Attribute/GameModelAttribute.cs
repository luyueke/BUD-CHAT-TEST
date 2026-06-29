// @Author: YangJie
// @Description:
// @Date:  2023/07/18
// @Modify:

using GameData;

namespace SceneController.Attribute
{
    public class GameModelAttribute: System.Attribute
    {
        public GameMode GameMode { get; private set; }
        
        public GameModelAttribute(GameMode mode)
        {
            GameMode = mode;
        }
    }
}