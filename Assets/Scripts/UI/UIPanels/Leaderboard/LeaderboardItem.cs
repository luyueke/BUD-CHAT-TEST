using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardItem : MonoBehaviour
{
    public UserInfoView UserInfoView;
    public Text Txt_Index;
    public Text Txt_Score;
    public Text Txt_Suffix;
    private LeaderboardItemData _curData;
    private RankType _curRankType;

    public void InitData(RankType rankType, LeaderboardItemData data)
    {
        if(data == null)
            return;
        
        this._curRankType = rankType;
        UserInfoView.SetData(data.userInfo);
        Txt_Index.text = data.rank.ToString();
        SetScore(data.score);
        SetSuffix();

        if (data.rank == 0)
        {
            Txt_Index.text = "-";
        }
    }

    private void SetScore(int score)
    {
        Txt_Score.text = score.ToString();
    }

    private void SetSuffix()
    {
        var suffix = "";
        switch (_curRankType)
        {
            case RankType.YandereTheBest:
                suffix = "次";
                break;
            
            case RankType.YandereTheFast:
                suffix = "s";
                break;
        }

        Txt_Suffix.text = suffix;
    }
}
