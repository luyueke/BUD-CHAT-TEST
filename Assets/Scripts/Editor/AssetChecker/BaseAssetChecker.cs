
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AssetChecker {
    public abstract class BaseAssetChecker : ScriptableObject {
        public bool isOn;

        public List<string> extensions;

        public List<string> ignorePaths;

        public List<string> checkPaths;

        public virtual void Init() {

        }

        public virtual bool Check(AssetImporter assetImporter) {
            return true;
        }

        public virtual bool CheckPath(string assetPath) {

            if (extensions != null && extensions.Count > 0) {
                if (!extensions.Any(assetPath.EndsWith)) {
                    return false;
                }
            }

            if (ignorePaths != null && ignorePaths.Count > 0) {
                foreach (var ignorePath in ignorePaths) {
                    if (assetPath.Contains(ignorePath)) {
                        return false;
                    }
                }
            }

            if (checkPaths == null || checkPaths.Count == 0) {
                return true;
            }
            foreach (var checkPath in checkPaths) {
                if (assetPath.Contains(checkPath)) {
                    return true;
                }
            }
            return false;
        }


    }
}
