// @Author: YangJie
// @Description:
// @Date:  2023/08/08
// @Modify:

using UI.UIPanels.GameEdit.Light;

namespace UI.UIPanels.GameEdit
{
    public class DirLightViewAdapter : BasePropertyAdapter
    {

        private DirLightBasicSubView basicSubView;
        private DirLightAdvancedSubView advancedSubView;
        
        protected override void OnCreate()
        {
            basicSubView = AddTabView<DirLightBasicSubView>("基础");
            advancedSubView = AddTabView<DirLightAdvancedSubView>("高级");
        }
        protected override void OnSelectEntity()
        {
            
        }
    }
}