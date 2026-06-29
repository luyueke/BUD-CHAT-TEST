
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using UnityEngine;
using UnityEngine.UI;

public class RemovedPropItem : MonoBehaviour
{
    public RemoteImageBehaviour iconRemoteImageBehaviour;

    private void Start()
    {
       
    }
    
    public void OnInitCreate(UgcBaseInfo ugcBaseInfo)
    {
        string mapCoverUrl = ugcBaseInfo.cover;
        iconRemoteImageBehaviour.Load(mapCoverUrl, true, null);
    }

}