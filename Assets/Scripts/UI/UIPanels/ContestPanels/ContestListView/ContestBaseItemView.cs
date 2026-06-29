using System;
using UnityEngine;
using GameData;

public class ContestBaseItemView : MonoBehaviour
{
    public virtual void Init(ContestEntryInfo data, Action<ContestEntryInfo> onSelect)
    {
        
    }

    public virtual void UpdateRankState(bool isHide)
    {
        
    }
    public virtual void HidePrice(bool isHide)
    {

    }
}