using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedItem : MonoBehaviour
{
    [SerializeField] RemoteImageBehaviour remoteImageBehaviour;

    public void SetData(UgcBaseInfo info)
    {
        remoteImageBehaviour.Load(info.cover);
    }
}
