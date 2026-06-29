using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIHospitalNpcShowListItem : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] CButton _selfBtn;
    [SerializeField] RemoteImageBehaviour _cover;

    private HospitalNPCData _selfData;

    public void SetData(HospitalNPCData selfData)
    {
        _selfData = selfData;
        InitHeadIcon(); 
    }

    private void InitHeadIcon()
    {
        _cover.Load(_selfData.cover);
    }

    public void OnClick()
    {
        //todo 这里点击需要展示一下npc更具体的信息
    }
}
