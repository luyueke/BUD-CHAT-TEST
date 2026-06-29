using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using UnityEngine;

public class PreviewInfoPanel : MonoBehaviour
{
    public RemoteImageBehaviour Rm_Cover;
    public SuperTextMesh Txt_Name;
    public SuperTextMesh Txt_Desc;
    private UgcBaseInfo _curUgcInfo;

    public void SetData(UgcBaseInfo info)
    {
        this._curUgcInfo = info;
        Txt_Name.SetText(_curUgcInfo.name);
        Txt_Desc.SetText(_curUgcInfo.desc);
        Rm_Cover.Load(_curUgcInfo.cover);
    }
}
