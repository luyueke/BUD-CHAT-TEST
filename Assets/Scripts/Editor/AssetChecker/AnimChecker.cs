using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AssetChecker {

    [CreateAssetMenu(fileName = "AnimChecker", menuName = "AssetChecker/AnimChecker")]
    public class AnimChecker : BaseAssetChecker {

        public override bool Check(AssetImporter assetImporter) {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetImporter.assetPath);
            if (FixAnimationAccuracy(clip)) {
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssetIfDirty(clip);
            }
            CompressAnimation(assetImporter.assetPath);
            return true;
        }


        private void CompressAnimation(string assetPath) {
            var animationContent = File.ReadAllText(assetPath);
            if (animationContent.Contains("m_UseHighQualityCurve: 1"))
            {
                animationContent = animationContent.Replace("m_UseHighQualityCurve: 1", "m_UseHighQualityCurve: 0");
                File.WriteAllText(assetPath, animationContent);
            }
        }

        private bool FixAnimationAccuracy(AnimationClip clip) {
            bool isChangeFixed = false;
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            if (curveBindings == null || curveBindings.Length == 0) return false;

            foreach (var curveDate in curveBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, curveDate);
                if (curve == null || curve.keys == null)
                {
                    continue;
                }

                var keyFrames = curve.keys;
                for (var i = 0; i < keyFrames.Length; i++)
                {
                    var key = keyFrames[i];

                    var tmpValue = key.value.ToString(CultureInfo.InvariantCulture);
                    key.value = float.Parse(key.value.ToString("f3"));
                    if (key.value.ToString(CultureInfo.InvariantCulture) != tmpValue)
                    {
                        isChangeFixed = true;
                    }

                    tmpValue = key.inTangent.ToString(CultureInfo.InvariantCulture);
                    key.inTangent = float.Parse(key.inTangent.ToString("f3"));
                    if (key.inTangent.ToString(CultureInfo.InvariantCulture) != tmpValue)
                    {
                        isChangeFixed = true;
                    }
                    tmpValue = key.outTangent.ToString(CultureInfo.InvariantCulture);
                    key.outTangent = float.Parse(key.outTangent.ToString("f3"));
                    if (key.outTangent.ToString(CultureInfo.InvariantCulture) != tmpValue)
                    {
                        isChangeFixed = true;
                    }

                    keyFrames[i] = key;
                }
                if (isChangeFixed)
                {
                    curve.keys = keyFrames;
                    clip.SetCurve(curveDate.path, curveDate.type, curveDate.propertyName, curve);
                }
            }
            return isChangeFixed;
        }

    }
}
