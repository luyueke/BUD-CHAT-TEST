// @Author: YangJie
// @Description:
// @Date:  2023/08/07
// @Modify:

using System;
using Game.MapSetting;
using Game.Props.PropsComponents;
using System.Linq;
using Es;
using UI.UIWidgets;

namespace UI.UIPanels.GameEdit
{
    public class SkyboxViewAdapter : BasePropertyAdapter
    {
        private PgcListEditSubView subView;
        protected override void OnCreate()
        {
            subView = AddTabView<PgcListEditSubView>("种类");
            subView.isLoadAnim = true;
            var skyBoxList = Es.DataTables.GetNormalSkyboxDataList();
            Comparison<NormalSkyboxData> myComparison = SkyboxComparison;
            skyBoxList.Sort(myComparison);
            var skyBoxConfigList = skyBoxList.Where(x => !x.Id.StartsWith("2")).Select(x=>new SkyboxConfig(){ Id =x.Id, IconName = x.Icon }).ToList();
            subView.AddItemSelectListenerByItem(OnItemSelect);
            subView.AddUndoSelectListener(OnItemSelect);
            subView.InitConfig(skyBoxConfigList);
        }
        
        private  int SkyboxComparison(NormalSkyboxData p1, NormalSkyboxData p2)
        {
            if (p1.order >= p2.order)
            {
                return 1;
            }
            return -1;
        }

        protected override void OnSelectEntity()
        {
            var skyboxComp = SkyboxManager.Inst.GetComp();
            subView.SelectItemWithNoNotify(skyboxComp.skyboxId);
        }

        private void OnItemSelect(string id,GameIconLoadItem select)
        {
            var skyboxComp = SkyboxManager.Inst.GetComp();
            var normalSkyboxData = Es.DataTables.GetNormalSkyboxData(id);
            skyboxComp.skyboxId = normalSkyboxData.Id;
            skyboxComp.skyboxColor = new SkyboxColor()
            {
                sky = normalSkyboxData.Sky,
                equator = normalSkyboxData.Equator,
                ground = normalSkyboxData.Ground,
            };
            SkyboxManager.Inst.SetSkyboxData(() =>
            {
                if (select != null)
                {
                    select.HideLoading();
                }
                SkyboxManager.Inst.SetSkyboxColor();
                SkyboxManager.Inst.SetSkyboxLight();
            });
        }
        
        private void OnItemSelect(string id)
        {
            OnItemSelect(id,null);
        }
    }
}