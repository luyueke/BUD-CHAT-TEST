using Es;
using Fsbm.Runtime;
using System.Linq;
using UnityEngine.UI;

public class SeasonPassBuyLevelItem : ItemRenderer
{
    public Image Icon;

    public Text Count;

    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
    protected override void Init()
    {
        base.Init();
    }


    protected override void UpdateView()
    {
        base.UpdateView();

        var info = data as SeasonPassItemInfo;
        var reward = info.rewardInfo.itemList.First();
        Count.text = "x" + reward.amount;
        if (reward.IconSp != null)
        {
            Icon.sprite = reward.IconSp;
        }
        else
        {
            var iconName = info.IconName();
            Icon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, this.gameObject);
        }
    }
}