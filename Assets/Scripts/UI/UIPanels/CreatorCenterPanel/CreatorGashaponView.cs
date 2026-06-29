using System.Collections;
using System.Collections.Generic;
using UI.UIPanels.GashaponPanel;
using UnityEngine;

public class CreatorGashaponView : MonoBehaviour
{
    [Header("各个根节点")]
    [SerializeField] private Transform viewContent;//自定义扭蛋的根节点

    private string curGashaponId =  "lottery.colorfulRainbow";
    private CreatorCenterPanel _centerPanel;
    public void Init(CreatorCenterPanel centerPanel)
    {
        _centerPanel = centerPanel;
        CreateCustomView(curGashaponId);
    }
    private void CreateCustomView(string gashaponId)
    {
        var ViewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        GashaponType gashaponType = (GashaponType)ViewCfg.ViewId;
        // 特殊处理, 四合一扭蛋
        if (gashaponType == GashaponType.FantasyBunny || gashaponType == GashaponType.DemonRaven || gashaponType == GashaponType.RadiantApollo ) {
            ViewCfg = GashaponDataManager.Inst.GetGashaponViewCfg(GashaponType.WitchLuna);
            gashaponType = GashaponType.WitchLuna;
            gashaponId = ViewCfg.GashaId;
        }
        string viewName = gashaponType.ToString();
        string viewPath = "Assets/Loadable/UI/UIPanel/Rainbow/Rainbow.prefab";
        var packViewNode = Loader.Load<GameObject>(viewPath).Instantiate(viewContent);
        GashaponBaseView gashaponView = packViewNode.GetComponent<GashaponBaseView>();
        if (gashaponView is RainbowView)
        {
            var rainbowView = gashaponView as RainbowView; 
            rainbowView.SetCreatorPanel(_centerPanel);
        }
        gashaponView.OnCreate(gashaponId);
    }
}
