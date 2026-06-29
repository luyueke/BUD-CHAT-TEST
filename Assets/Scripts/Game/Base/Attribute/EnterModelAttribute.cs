// @Author: YangJie
// @Description:
// @Date:  2023/07/12
// @Modify:

using GameData;

namespace SceneController.Attribute
{
    public class EnterModelAttribute : System.Attribute
    {
        
        public EnterGameModel[] EnterGameModels { get; private set; }
        
        public EnterModelAttribute(params EnterGameModel[] enterGameModels)
        {
            EnterGameModels = enterGameModels;
        }
    }
}