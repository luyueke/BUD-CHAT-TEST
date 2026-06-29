using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetChecker {
    public class AssetCheckPostProcessor : AssetPostprocessor {

        private static List<BaseAssetChecker> assetCheckers;
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths) {
            InitCheckers();
            foreach (var assetPath in importedAssets) {
                HandleAssetPostImported(assetPath);
            }
            SaveCheckers();
        }


        private static void InitCheckers() {
            assetCheckers = new List<BaseAssetChecker>();
            var animCheckGuids = AssetDatabase.FindAssets("t:BaseAssetChecker");
            foreach (string animCheckGuid in animCheckGuids) {
                var animCheck = AssetDatabase.LoadAssetAtPath<BaseAssetChecker>(AssetDatabase.GUIDToAssetPath(animCheckGuid));
                if (animCheck != null && animCheck.isOn) {
                    animCheck.Init();
                    assetCheckers.Add(animCheck);
                }
            }
        }

        private static void SaveCheckers() {
            foreach (var tmpChecker in assetCheckers) {
                AssetDatabase.SaveAssetIfDirty(tmpChecker);
            }
        }

        private static void HandleAssetPostImported(string assetPath) {
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            foreach (var assetChecker in assetCheckers) {
                if (assetChecker.CheckPath(assetPath)) {
                    assetChecker.Check(assetImporter);
                }
            }
        }
    }
}
