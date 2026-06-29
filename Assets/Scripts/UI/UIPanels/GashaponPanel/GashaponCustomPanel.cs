using Game.Store;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.GashaponPanel
{
    public class GashaponCustomPanel : BasePanel<GashaponCustomPanel>
    {
        [Header("各个根节点")]
        [SerializeField] private Transform viewContent;//自定义扭蛋的根节点

        private string curGashaponId;

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            if (args is { Length: > 0 })
            {
                curGashaponId = args[0] as string;
            }
            var gashaponView = CreateCustomView(curGashaponId);
        }


        private GashaponBaseView CreateCustomView(string gashaponId)
        {


            var ViewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
            GashaponType gashaponType = (GashaponType)ViewCfg.ViewId;
            // 特殊处理, 四合一扭蛋
            if (gashaponType == GashaponType.FantasyBunny || gashaponType == GashaponType.DemonRaven || gashaponType == GashaponType.RadiantApollo ) {
                ViewCfg = GashaponDataManager.Inst.GetGashaponViewCfg(GashaponType.WitchLuna);
                gashaponType = GashaponType.WitchLuna;
                gashaponId = ViewCfg.GashaId;
            }
            // 特殊处理, 虾虾崽三合一扭蛋
            if (gashaponType == GashaponType.BabyShrimpHuhu || gashaponType == GashaponType.BabyShrimpSuit || gashaponType == GashaponType.BabyShrimpWuwu || gashaponType == GashaponType.BabyShrimpRabbit)
            {
                ViewCfg = GashaponDataManager.Inst.GetGashaponViewCfg(GashaponType.BabyShrimpSuit);
                gashaponType = GashaponType.BabyShrimpSuit;
                gashaponId = ViewCfg.GashaId;
            }


            string viewName = gashaponType.ToString();
            string viewPath = GashaponUtils.ViewBasePath + viewName + "/" + viewName + ".prefab";
            var packViewNode = Loader.Load<GameObject>(viewPath).Instantiate(viewContent);
            GashaponBaseView gashaponView = packViewNode.GetComponent<GashaponBaseView>();
            gashaponView.SetMainPanel(this);
            gashaponView.OnCreate(gashaponId);
            return gashaponView;
        }
    }
}
