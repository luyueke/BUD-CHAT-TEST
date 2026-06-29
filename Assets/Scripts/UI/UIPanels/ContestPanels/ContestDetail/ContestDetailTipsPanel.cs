using UnityEngine.UI;
using UI.Base;
using GameData;
using UnityEngine;

public class ContestDetailTipsPanel : BasePanel<ContestDetailTipsPanel>
{
    [SerializeField] private Button hide;
    [SerializeField] private Text desc;
    [SerializeField] private Image bgImage;
    
    public override void OnCreate()
    {
        base.OnCreate();
        
        hide.onClick.AddListener(() =>
        {
            CloseSelf();
        });
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        
        ContestInfo contestInfo = args[0] as ContestInfo;
        if (contestInfo == null)
        {
            return;
        }
        SetTipsImage(contestInfo);
    }

    private void SetTipsImage(ContestInfo info)
    {
        DataUtil.TryGetFromList(info.themeColorList, 0, out string themeColor1);
        DataUtil.TryGetFromList(info.themeColorList, 1, out string themeColor2);
        if (!string.IsNullOrEmpty(themeColor1))
        {
            desc.color = DataUtil.DeSerializeColorCheckHash(themeColor1);
        }
        if (!string.IsNullOrEmpty(themeColor2))
        {
            bgImage.color = DataUtil.DeSerializeColorCheckHash(themeColor2);
        }
        string rule = info.rule;
        desc.text = rule;
    }
}
