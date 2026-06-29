using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    [CustomEditor(typeof(Build))]
    [CanEditMultipleObjects]
    public class BuildEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            //var build = target as Build;
            if (GUILayout.Button("双平台全打包", EditorStyles.miniButton, GUILayout.Width(200)))
            {
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                {
                    var builds = new List<Build>();
                    for (int i = 0, L = targets.Length; i < L; i++)
                    {
                        var build = targets[i] as Build;
                        if (build) builds.Add(build);
                    }
                    if (builds.Count > 0)
                    {
                        Builder.BuildBundles(builds.ToArray());
                        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                        {
                            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                            Builder.BuildBundles(builds.ToArray());
                        }
                        else
                        {
                            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                            Builder.BuildBundles(builds.ToArray());
                        }
                    }
                }
                else
                {
                    Debug.Log("请切换安卓或者IOS打ab包");
                }
            }

            if (GUILayout.Button("当前平台全打包", EditorStyles.miniButton, GUILayout.Width(200)))
            {
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                {
                    var builds = new List<Build>();
                    for (int i = 0, L = targets.Length; i < L; i++)
                    {
                        var build = targets[i] as Build;
                        if (build) builds.Add(build);
                    }
                    if (builds.Count > 0)
                    {
                        Builder.BuildBundles(builds.ToArray());
                    }
                }
                else
                {
                    Debug.Log("请切换安卓或者IOS打ab包");
                }
            }

        }
    }
}