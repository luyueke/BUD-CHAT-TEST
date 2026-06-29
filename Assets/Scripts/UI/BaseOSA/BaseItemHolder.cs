// @Author: YangJie
// @Description:
// @Date:  2023/09/12
// @Modify:

using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.IO;
using frame8.Logic.Misc.Other.Extensions;

namespace UI.BaseOSA
{
    public class BaseItemHolder<T, TD>:  CellViewsHolder where T: BaseData where TD: BaseItem<T>, new()
    {
        public TD item;
        
        public override void CollectViews()
        {
            base.CollectViews();
            item = root.GetComponentInParent<TD>();
        }
        
        public void UpdateViews(T data)
        {
            item.Init(data);
        }
    }
}