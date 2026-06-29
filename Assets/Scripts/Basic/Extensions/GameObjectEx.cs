using UnityEngine;
using System.Collections.Generic;


public static class GameObjectEx {
    /// <summary>
    /// 查找子物体，结果包含隐藏节点, 不包含自身
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="findSon">只查找一层子节点</param>
    /// <param name="includeInactive">是否查找隐藏的节点</param>
    /// <param name="skip">跳过某些节点不进行查找</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> GetAllChildren<T>(this GameObject gameObject, bool findSon, bool includeInactive,
        T[] skip = null) where T : Component {
        var childTargets = gameObject.GetComponentsInChildren<T>(includeInactive);
        var allChildren = new List<T>(childTargets.Length);

        foreach (var child in childTargets) {
            if (child.gameObject == gameObject) {
                continue;
            }

            if (findSon && child.transform.parent != gameObject.transform) {
                continue;
            }

            if (skip != null) {
                foreach (var s in skip) {
                    if (child != s) {
                        allChildren.Add(child);
                    }
                }
            } else {
                allChildren.Add(child);
            }
        }

        return allChildren;
    }
    /// <summary>
    /// 获取 GameObject 的 Bounds
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="isReset"> 是否重置位置及缩放 </param>
    /// <returns></returns>
    public static Bounds GetBounds(this GameObject gameObject, bool isReset = false) {
        var lastPosition = gameObject.transform.localPosition;
        var lastRotation = gameObject.transform.localRotation;
        var lastScale = gameObject.transform.localScale;
        var lastParent = gameObject.transform.parent;



        var renders = gameObject.transform.GetComponentsInChildren<MeshRenderer>(true);
        if (renders.Length == 0) {
            return default;
        }

        if (isReset) {
            gameObject.transform.SetParent(null);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
        }
        var center = Vector3.zero;
        foreach (var child in renders) {
            center += child.bounds.center;
        }
        center /= renders.Length;
        var bounds = new Bounds(center, Vector3.zero);
        foreach (var child in renders) {
            bounds.Encapsulate(child.bounds);
        }
        if (isReset) {
            gameObject.transform.SetParent(lastParent);
            gameObject.transform.localPosition = lastPosition;
            gameObject.transform.localRotation = lastRotation;
            gameObject.transform.localScale = lastScale;
        }
        return bounds;
    }

    public static List<GameObject> GetChildrens(this GameObject gameObject)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            gameObjects.Add(gameObject.transform.GetChild(i).gameObject);
        }
        return gameObjects;
    }


    public static List<GameObject> GetAllChildren(this GameObject gameObject) {
        Transform[] childTransforms = gameObject.GetComponentsInChildren<Transform>(true);
        var allChildren = new List<GameObject>(childTransforms.Length);

        foreach (var child in childTransforms) {
            if (child.gameObject != gameObject)
                allChildren.Add(child.gameObject);
        }

        return allChildren;
    }

    public static Transform FindChildByName(Transform parent, string name) {
        Transform child = parent.Find(name);
        if (child != null) {
            return child;
        }
        for (int i = 0; i < parent.childCount; i++) {
            child = FindChildByName(parent.GetChild(i), name);
            if (child != null) {
                break;
            }
        }
        return child;
    }

    public static Transform FindChildByName(GameObject parent, string name) {
        if (parent == null) {
            return null;
        }
        return FindChildByName(parent.transform, name);
    }

    public static T FindComponentByName<T>(GameObject parent, string name) where T : Component {
        return FindComponentByName<T>(parent.transform, name);
    }

    public static T FindComponentByName<T>(Transform parent, string name) where T : Component {
        Transform child = FindChildByName(parent, name);
        if (child != null) {
            return child.GetComponent<T>();
        }
        return null;
    }

    public static bool TryGetComponentInParent<T>(this GameObject gameObject, out T component) where T : Component {
        component = gameObject.GetComponentInParent<T>();
        return component != null;
    }

    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component {
        if (!gameObject.TryGetComponent<T>(out var tmpComponent)) {
            tmpComponent = gameObject.AddComponent<T>();
        }
        return tmpComponent;
    }

    public static void Reset(this GameObject gameObject, bool isLocal = true) {
        if (gameObject == null) {
            return;
        }
        if (isLocal) {
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
        } else {
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;

            // Scale 无全局设置
            gameObject.transform.localScale = Vector3.one;
        }
    }

    public static bool IsChildOf(this GameObject gameObject, Transform parent) {
        if (gameObject == null) {
            return false;
        }

        if (parent != null) {
            if (gameObject.transform.parent == parent) {
                return true;
            }

            if (gameObject.transform.parent == null) {
                return false;
            }

            return IsChildOf(gameObject.transform.parent.gameObject, parent);
        }

        return false;
    }

    public static void ClearChildren(this GameObject go)
    {
        ClearChildren(go.transform);
    }

    public static List<GameObject> GetChildNodes(this GameObject nodeParent, string compareStr) {
        return GetChildNodes(nodeParent.transform, compareStr);
    }

    public static List<GameObject> GetChildNodes(this Transform nodeParent, string compareStr) {
        List<GameObject> nodes = new List<GameObject>();
        for (int i = 0; i < nodeParent.childCount; i++) {
            var node = nodeParent.GetChild(i);
            if (node.name.Contains(compareStr)) {
                nodes.Add(node.gameObject);
            }
        }
        return nodes;
    }


    public static void ClearChildren(this Transform trans)
    {
        if (trans == null)
        {
            return;
        }
        foreach (Transform t in trans)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(t.gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(t.gameObject);
            }
        }
    }

    public static void SetActiveValid(this GameObject obj,bool bo) 
    {
        if (obj == null)
        {
            return;
        }
        if (bo)
        {
            if (!obj.gameObject.activeSelf) obj.gameObject.SetActive(true);
        }
        else
        {
            if (obj.gameObject.activeSelf) obj.gameObject.SetActive(false);
        }
    }


}
