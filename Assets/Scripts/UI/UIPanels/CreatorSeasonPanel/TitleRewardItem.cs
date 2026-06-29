using Es;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TitleRewardItem : MonoBehaviour
{
    [SerializeField] private Text TitleName;
    [SerializeField] private Text Level;
    [SerializeField] private Text Score;
    [SerializeField] private Transform TitleObj;
    [SerializeField] private UserBadge UserBadge;

    public void SetData(CreatorBadgeInfoData data)
    {
        Level.text = GetLevelStr(data.level);
        Score.text = data.score.ToString();
        if (UserBadge != null) UserBadge.SetData(data.category, data.level);
        RefreshTitleObj(data);
        GetCategoryStr(data);
    }

    private void RefreshTitleObj(CreatorBadgeInfoData data)
    {
        if (TitleObj == null)
            return;

        for (int i = TitleObj.childCount - 1; i >= 0; i--)
            DestroyImmediate(TitleObj.GetChild(i).gameObject);

        var list = DataTables.GetCreatorRewardConfigList();
        if (list == null)
            return;

        CreatorRewardConfig cfg = null;
        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (c == null) continue;
            if (c.Category == data.category && c.Level == data.level)
            {
                cfg = c;
                break;
            }
        }

        if (cfg == null || cfg.TitleId <= 0)
            return;

        var titleConfig = UserUIWidgetManager.Inst?.GetTitleData(cfg.TitleId);
        if (titleConfig == null || string.IsNullOrEmpty(titleConfig.Prefab))
            return;

        var prefab = Loader.Load<GameObject>(titleConfig.Prefab, gameObject);
        if (prefab == null)
            return;

        Instantiate(prefab, TitleObj);
    }

    private string GetLevelStr(int level)
    {
        string str = "";
        switch(level)
        {
            case 0:
                str = "初级";
            break;
            case 1:
                str = "高级";
            break;
            case 2:
                str = "前100";
            break;
            case 3:
                str = "前10";
            break;
            case 4:
                str = "前1";
            break;
        }

        return str;
    }

    private void GetCategoryStr(CreatorBadgeInfoData data)
    {
        if(data == null)
        {
            return;
        }
        TitleName.text = data.category switch
        {
            0 => "总榜徽章",
            1 => "2D皮肤徽章",
            2 => "3D皮肤徽章",
            3 => "动作徽章",
            4 => "地图徽章",
            5 => "工具徽章",
            _ => "总榜徽章"
        };
    }

}
