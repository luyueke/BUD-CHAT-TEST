using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StateExcludeConfig", menuName = "StateExcludeConfig", order = 1)]
public class StateExcludeConfig : ScriptableObject
{
    [HideInInspector]
    public PlayerStateType horizontalType;
    [HideInInspector]
    public PlayerStateType verticalType;

    [HideInInspector]
    public StateExcludeTypeList stateExcludeTypes;
}

[Serializable]
public class StateExcludeTypeList
{
    public List<StateExcludeTypeConfig> stateExcludeTypeList;

    public StateExcludeTypeList()
    {
        stateExcludeTypeList = new List<StateExcludeTypeConfig>();
    }

    public void SetStateExcludeType(int hIndex, int vIndex, StateExcludeType stateExcludeType)
    {
        StateExcludeTypeConfig singleStateExcludeType = stateExcludeTypeList.Find(x => x.HIndex == hIndex && x.VIndex == vIndex);

        if (singleStateExcludeType == null)
        {
            singleStateExcludeType = new StateExcludeTypeConfig(hIndex, vIndex, stateExcludeType);
            stateExcludeTypeList.Add(singleStateExcludeType);
        }
        else
        {
            singleStateExcludeType.stateExcludeType = stateExcludeType;
        }
    }

    public StateExcludeType GetStateExcludeType(int hIndex, int vIndex)
    {
        StateExcludeTypeConfig singleStateExcludeType = stateExcludeTypeList.Find(x => x.HIndex == hIndex && x.VIndex == vIndex);

        if (singleStateExcludeType == null)
        {
            singleStateExcludeType = new StateExcludeTypeConfig(hIndex, vIndex, StateExcludeType.None);
            stateExcludeTypeList.Add(singleStateExcludeType);
        }

        return singleStateExcludeType.stateExcludeType;
    }

    public void Clear()
    {
        stateExcludeTypeList.Clear();
    }
}

[Serializable]
public class StateExcludeTypeConfig
{
    [SerializeField]
    private int hIndex;
    [SerializeField]
    private int vIndex;
    public StateExcludeType stateExcludeType;

    public int HIndex
    {
        get
        {
            return hIndex - 1;
        }

        set
        {
            hIndex = value + 1;
        }
    }

    public int VIndex
    {
        get
        {
            return vIndex - 1;
        }

        set
        {
            vIndex = value + 1;
        }
    }

    public int RealHIndex => hIndex;
    public int RealVIndex => vIndex;

    public StateExcludeTypeConfig(int hIndex, int vIndex, StateExcludeType stateExcludeType)
    {
        this.HIndex = hIndex;
        this.VIndex = vIndex;
        this.stateExcludeType = stateExcludeType;
    }
}