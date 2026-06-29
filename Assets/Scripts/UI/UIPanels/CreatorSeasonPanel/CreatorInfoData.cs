using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CreatorSelectType
{
    All = 0,
    Clothes = 1,//2D皮肤
    Skin = 2,//3D皮肤
    Action = 3,//动作
    Map = 4,//地图
    Tools = 5,//工具
}

public class CreatorScoreRankInfoData
{
    public List<UserRankData> list;
    public UserRankData userRankData;
}
public class UserRankData
{
    public AccountUserInfo userInfo;
    public CreatorUserRankInfo userRank;
}

public class CreatorUserRankInfo
{
    public int score;
    public int rank;
}

public class CreatorUserScoreInfoList
{
    public List<CreatorUserScoreInfoData> list;
}

public class CreatorUserScoreInfoData
{
    public int category;//all = 0； //全服 Skin2D = 1; Skin3D = 2; Animation = 3; Map = 4; Tools = 5;
    public int score;
    public int rank;
    public int interactNum;
    public int publishNum;
    public List<scoreDetails> scoreDetails;
}
public class scoreDetails
{
    public string type;//0 base 1 创作分 2 活动 3 商城
    public string score;
    public int maxScore;
}
public class CreatorBadgeListInfoData
{
    public List<CreatorBadgeInfoData> list;
}


