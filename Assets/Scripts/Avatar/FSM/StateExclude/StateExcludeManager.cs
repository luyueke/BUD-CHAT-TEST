using FSM;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SingleStateType
{
    public string typeName;
    public string typeValue;
}

public class SingleStateTypeExclude
{
    public Dictionary<string, Dictionary<SingleStateType, StateExcludeType>> excludeDic;
}

[Serializable]
public class StateExcludeConfigList
{
    public List<StateExcludeConfig> configList;

    public StateExcludeConfigList()
    {
        configList = new List<StateExcludeConfig>();
    }
}

public class StateExcludeManager : GameInstance<StateExcludeManager>
{
    private readonly string StateExcludeConfigPath = "Assets/Arts/Config/ExcludeConfig/";
    private readonly string PlayerStateTypeConfigPath = "Assets/Arts/Config/PlayerStateConfig/";
    //private readonly string AnimationOverrideConfigPath = "ConfigAssets/AnimOverride";

    // 状态分类配置
    Dictionary<PlayerState, StateType> stateTypeConfig;

    // 互斥配置
    Dictionary<string, SingleStateTypeExclude> excludeConfig;

    // 替换动画片段配置
    //Dictionary<PlayerState, List<AnimationClipList>> animationOverrideDic;

    public StateExcludeManager()
    {
        excludeConfig = new Dictionary<string, SingleStateTypeExclude>();
        stateTypeConfig = new Dictionary<PlayerState, StateType>();
        //animationOverrideDic = new Dictionary<PlayerState, List<AnimationClipList>>();

        LoadConfig();
    }

    private void LoadConfig()
    {
        LoadStateExcludeConfig();
        LoadPlayerStateConfig();
        LoadOverriAnimConfig();
    }

    // 后续实际项目中改动读取
    /// <summary>
    /// 加载状态互斥配置
    /// </summary>
    private void LoadStateExcludeConfig()
    {
        var playerStateTypeArray = Enum.GetValues(typeof(PlayerStateType));
        StateExcludeConfig[] stateExcludeConfigList = new StateExcludeConfig[playerStateTypeArray.Length * playerStateTypeArray.Length];

        int index = 0;
        foreach (var stateTypeFrom in playerStateTypeArray)
        {
            foreach (var stateTypeTo in playerStateTypeArray)
            {
                string fromName = stateTypeFrom.ToString().Replace("Player", "");
                string toName = stateTypeTo.ToString().Replace("Player", "");
                string path = StateExcludeConfigPath + fromName + "To" + toName + ".asset";
                stateExcludeConfigList[index] = Loader.Load<StateExcludeConfig>(path).RetainAsset();
                index++;
            }
        }

        //var stateExcludeConfigList = Resources.LoadAll<StateExcludeConfig>(StateExcludeConfigPath);

        foreach (var stateExcludeConfig in stateExcludeConfigList)
        {
            ParseStateExcludeConfig(stateExcludeConfig);
        }
    }

    private void LoadPlayerStateConfig()
    {
        var stateTypeArray = Enum.GetValues(typeof(PlayerState));
        StateType[] playerStateTypeList = new StateType[stateTypeArray.Length - 1];

        int index = 0;
        foreach (var stateType in stateTypeArray)
        {
            if (stateType.ToString().Equals("None")) continue;

            string path = PlayerStateTypeConfigPath + stateType.ToString() + "State.asset";
            playerStateTypeList[index] = Loader.Load<StateType>(path).RetainAsset();
            index++;
        }

        foreach (var playerStateType in playerStateTypeList)
        {
            if (!stateTypeConfig.ContainsKey(playerStateType.playerState))
            {
                stateTypeConfig.Add(playerStateType.playerState, playerStateType);
            }
            else
            {
                Debug.LogError("存在同样的配置 : " + playerStateType.name);
            }
        }
    }

    private void ParseStateExcludeConfig(StateExcludeConfig stateExcludeConfig)
    {
        Type hType = Type.GetType(stateExcludeConfig.horizontalType.ToString());
        Array hEnumArray = hType.GetEnumValues();

        Type vType = Type.GetType(stateExcludeConfig.verticalType.ToString());
        Array vEnumArray = vType.GetEnumValues();

        foreach (var singleStateExcludeTypeConfig in stateExcludeConfig.stateExcludeTypes.stateExcludeTypeList)
        {
            AddExcludeType(vEnumArray.GetValue(singleStateExcludeTypeConfig.RealVIndex), hEnumArray.GetValue(singleStateExcludeTypeConfig.RealHIndex), singleStateExcludeTypeConfig.stateExcludeType);
        }
    }

    /// <summary>
    /// 添加状态互斥类型
    /// </summary>
    /// <param name="enterType"></param>
    /// <param name="curType"></param>
    /// <param name="stateExcludeType"></param>
    private void AddExcludeType(object enterType, object curType, StateExcludeType stateExcludeType)
    {
        SingleStateType stateType = new SingleStateType();
        stateType.typeName = curType.GetType().Name;
        stateType.typeValue = curType.ToString();

        string enterTypeName = enterType.GetType().Name;
        SingleStateTypeExclude stateTypeExclude;
        if (!excludeConfig.TryGetValue(enterTypeName, out stateTypeExclude))
        {
            stateTypeExclude = new SingleStateTypeExclude();
            stateTypeExclude.excludeDic = new Dictionary<string, Dictionary<SingleStateType, StateExcludeType>>();

            excludeConfig.Add(enterTypeName, stateTypeExclude);
        }

        string enterTypeValue = enterType.ToString();
        Dictionary<SingleStateType, StateExcludeType> excludeTypeDic;
        if (!stateTypeExclude.excludeDic.TryGetValue(enterTypeValue, out excludeTypeDic))
        {
            excludeTypeDic = new Dictionary<SingleStateType, StateExcludeType>();
            stateTypeExclude.excludeDic.Add(enterTypeValue, excludeTypeDic);
        }

        excludeTypeDic.Add(stateType, stateExcludeType);
    }

    /// <summary>
    /// 是否能进入当前状态
    /// </summary>
    /// <param name="enterState"></param>
    /// <param name="curStateIDs"></param>
    /// <returns></returns>
    public bool CanEnterState(PlayerState enterState, PlayerState[] curStateIDs, out PlayerState beState)
    {
        beState = PlayerState.Default;
        if (curStateIDs == null || curStateIDs.Length == 0) return false;

        for (int i = 0; i < curStateIDs.Length; i++)
        {
            StateExcludeType excludeType = GetExludeType(enterState, curStateIDs[i]);
            if (excludeType == StateExcludeType.Ignore)
            {
                beState = curStateIDs[i];
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取进入状态互斥类型
    /// </summary>
    /// <param name="enterState"></param>
    /// <param name="curStateIDs"></param>
    /// <returns></returns>
    public Dictionary<PlayerState, StateExcludeType> GetEnterStateExcludeType(PlayerState enterState, PlayerState[] curStateIDs)
    {
        if (curStateIDs == null || curStateIDs.Length == 0) return null;

        Dictionary<PlayerState, StateExcludeType> excludeTypeDic = new Dictionary<PlayerState, StateExcludeType>();

        for (int i = 0; i < curStateIDs.Length; i++)
        {
            StateExcludeType excludeType = GetExludeType(enterState, curStateIDs[i]);
            if (excludeType != StateExcludeType.None)
            {
                excludeTypeDic.Add(curStateIDs[i], GetExludeType(enterState, curStateIDs[i]));
            }
        }

        return excludeTypeDic;
    }

    /// <summary>
    /// 获取互斥关系
    /// </summary>
    /// <param name="enterState"></param>
    /// <param name="curState"></param>
    /// <returns></returns>
    public StateExcludeType GetExludeType(PlayerState enterState, PlayerState curState)
    {
        // 根据PlayerState 获取相关分类 （可能属于多个分类）
        StateType enterStateType = stateTypeConfig[enterState];
        StateType curStateType = stateTypeConfig[curState];

        List<object> enterStateTypeList = enterStateType.GetStateType();
        List<object> curStateTypeList = curStateType.GetStateType();

        foreach (var enterType in enterStateTypeList)
        {
            SingleStateTypeExclude singleEnterStateTypeExclude = excludeConfig[enterType.GetType().Name];

            foreach (var singleStateType in singleEnterStateTypeExclude.excludeDic[enterType.ToString()])
            {
                foreach (var state in curStateTypeList)
                {
                    if (singleStateType.Key.typeName.Equals(state.GetType().Name) && singleStateType.Key.typeValue.Equals(state.ToString()))
                    {
                        return singleStateType.Value;
                    }
                }
            }
        }

        return StateExcludeType.None;
    }

    /// <summary>
    /// 加载替换动画片断配置
    /// </summary>
    private void LoadOverriAnimConfig()
    {
        //var animationOverrideList = Resources.LoadAll<AnimationOverrideList>(AnimationOverrideConfigPath);

        //foreach (var animationOverride in animationOverrideList)
        //{
        //    if (animationOverride.mainStates == null || animationOverride.mainStates.Count == 0)
        //    {
        //        continue;
        //    }

        //    foreach (var mainState in animationOverride.mainStates)
        //    {
        //        if (!animationOverrideDic.ContainsKey(mainState))
        //        {
        //            List<AnimationClipList> animationClipList = new List<AnimationClipList>();
        //            animationOverrideDic.Add(mainState, animationClipList);
        //        }

        //        animationOverride.animationClipList.name = animationOverride.name;
        //        animationOverrideDic[mainState].Add(animationOverride.animationClipList);
        //    }
        //}

        //foreach (var animationClipList in animationOverrideDic.Values)
        //{
        //    animationClipList.Sort();
        //}
    }

    //public AnimationClipList GetOverrideClipList(PlayerState mainState, PlayerState[] allState)
    //{
    //    foreach (var animationOverride in animationOverrideDic)
    //    {
    //        if (animationOverride.Key.Equals(mainState))
    //        {
    //            List<AnimationClipList> animationClipList = animationOverride.Value;
    //            for (int i = 0; i < animationClipList.Count; i++)
    //            {
    //                if (animationClipList[i].ByContains(allState))
    //                {
    //                    return animationClipList[i];
    //                }
    //            }
    //        }
    //    }

    //    return null;
    //}

    public StateType GetStateType(PlayerState stateID) {
        StateType stateType;
        stateTypeConfig.TryGetValue(stateID, out stateType);
        return stateType;
    }

    public override void Release()
    {
        base.Release();

        stateTypeConfig.Clear();
        excludeConfig.Clear();
        //animationOverrideDic.Clear();
    }
}