using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(StateExcludeConfig))]
public class StateExcludeConfigCustom : Editor
{
    StateExcludeConfig stateExcludeConfig;

    Assembly assembly;

    Dictionary<PlayerStateType, string[]> playerStateTypeDic = new Dictionary<PlayerStateType, string[]>();

    PlayerStateType hType;
    PlayerStateType vType;

    private string[] playerStateTypeNames;

    private string[] excludeTypeNames;

    private readonly int labelWidth = 60;
    private readonly int labelSpace = 30;
    int hCount = 0;
    int vCount = 0;

    private void OnEnable()
    {
        stateExcludeConfig = target as StateExcludeConfig;

        if (stateExcludeConfig == null) return;

        assembly = Assembly.Load("CharacterAvatar");

        Array excludeTypeArray = Enum.GetValues(typeof(StateExcludeType));
        excludeTypeNames = new string[excludeTypeArray.Length];
        for (int i = 0; i < excludeTypeArray.Length; i++)
        {
            excludeTypeNames[i] = PropertyAttributeUtil.GetEnumPropertyAttribute<LabelAttribute>(excludeTypeArray.GetValue(i) as Enum).label;
        }

        RefreshExcludeType();

        if (stateExcludeConfig.stateExcludeTypes == null)
        {
            stateExcludeConfig.stateExcludeTypes = new StateExcludeTypeList();
        }
    }

    void RefreshExcludeType()
    {
        playerStateTypeDic.Clear();

        hType = stateExcludeConfig.horizontalType;
        vType = stateExcludeConfig.verticalType;

        Array stateTypeArray = Enum.GetValues(typeof(PlayerStateType));
        playerStateTypeNames = new string[stateTypeArray.Length];

        for (int i = 0; i < stateTypeArray.Length; i++)
        {
            PlayerStateType playerStateType = (PlayerStateType)stateTypeArray.GetValue(i);
            playerStateTypeNames[i] = PropertyAttributeUtil.GetEnumPropertyAttribute<LabelAttribute>(playerStateType).label;

            Type singleStateType = assembly.GetType(stateTypeArray.GetValue(i).ToString());
            Array singleStateTypeArray = singleStateType.GetEnumValues();
            string[] singleStateAttributes = new string[singleStateTypeArray.Length - 1];
            playerStateTypeDic.Add(playerStateType, singleStateAttributes);

            int index = 0;
            for (int j = 0; j < singleStateTypeArray.Length; j++)
            {
                string label = PropertyAttributeUtil.GetEnumPropertyAttribute<LabelAttribute>(singleStateTypeArray.GetValue(j) as Enum).label;

                if (!"None".Equals(label))
                {
                    singleStateAttributes[index] = label;
                    index++;
                }

                //singleStateAttributes[j] = PropertyAttributeUtil.GetEnumPropertyAttribute<LabelAttribute>(singleStateTypeArray.GetValue(j) as Enum).label;

            }
        }

        hCount = playerStateTypeDic[stateExcludeConfig.horizontalType].Length;
        vCount = playerStateTypeDic[stateExcludeConfig.verticalType].Length;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (stateExcludeConfig == null) return;

        #region 横向，纵向
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("横向", GUILayout.Width(50));
        stateExcludeConfig.horizontalType = (PlayerStateType)EditorGUILayout.Popup((int)stateExcludeConfig.horizontalType, playerStateTypeNames);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("纵向", GUILayout.Width(50));
        stateExcludeConfig.verticalType = (PlayerStateType)EditorGUILayout.Popup((int)stateExcludeConfig.verticalType, playerStateTypeNames);

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(50);

        #endregion

        #region 互斥表格
        if (playerStateTypeDic[stateExcludeConfig.verticalType].Length == vCount && playerStateTypeDic[stateExcludeConfig.horizontalType].Length == hCount)
        {
            EditorGUILayout.BeginHorizontal();
            string[] hTypeStr = playerStateTypeDic[stateExcludeConfig.horizontalType];

            GUILayout.Label("", GUILayout.Width(labelWidth));

            for (int i = 0; i < hTypeStr.Length; i++)
            {
                GUILayout.Label(hTypeStr[i], GUILayout.Width(labelWidth));
            }
            EditorGUILayout.EndHorizontal();

            string[] vTypeStr = playerStateTypeDic[stateExcludeConfig.verticalType];

            for (int i = 0; i < vTypeStr.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(vTypeStr[i], GUILayout.Width(labelWidth));

                for (int j = 0; j < hCount; j++)
                {
                    StateExcludeType excludeType = (StateExcludeType)EditorGUILayout.Popup((int)stateExcludeConfig.stateExcludeTypes.GetStateExcludeType(j, i), excludeTypeNames, GUILayout.Width(labelWidth));

                    stateExcludeConfig.stateExcludeTypes.SetStateExcludeType(j, i, excludeType);
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(labelSpace);
            }
        }
        #endregion

        #region changed
        if (GUI.changed)
        {
            if (hType != stateExcludeConfig.horizontalType || vType != stateExcludeConfig.verticalType)
            {
                RefreshExcludeType();

                stateExcludeConfig.stateExcludeTypes.Clear();
            }

            EditorUtility.SetDirty(target);
        }
        #endregion
    }
}