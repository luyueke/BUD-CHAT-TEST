using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JoinInContestItem : MonoBehaviour
{
    [SerializeField] public RemoteImageBehaviour remoteImageBehaviour;
    [SerializeField] Text contestName;
    [SerializeField] Image contestColor;
    [SerializeField] Text contestTime;
    [SerializeField] public Toggle Toggle;

    public string ContestId;
    public ContestInfo Data;
    public void SetData(ContestInfo data)
    {
        Data = data;
        ContestId = data.contestId;
        remoteImageBehaviour.Load(data.bannerUrl);
        contestName.text = data.contestName;
        contestTime.text = ContestEventManager.GetLeftTimeStr(data);
        DataUtil.TryGetFromList(data.themeColorList, 1, out string color2);
        ColorUtility.TryParseHtmlString(color2, out Color color);
        contestColor.color = color;
    }
}
