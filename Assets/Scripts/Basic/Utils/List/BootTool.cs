using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Fsbm.Runtime
{

    public static class BootTool
    {

        private static Transform _root;
        public static Transform GetRoot()
        {
            if (_root == null)
            {
                GameObject go = GameObject.Find("GameFramework");
                if (go == null)
                {
                    go = new GameObject("GameFramework");
                }

                GameObject.DontDestroyOnLoad(go);

                _root = go.transform;
            }
            return _root;
        }
        public static Transform GetRootChild(string name)
        {
            Transform root = GetRoot();
            Transform child = root.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name);
                child = go.transform;
                child.parent = root;
            }

            return child;
        }

        public static T GetRootComponent<T>(string name) where T : Component
        {
            Transform go = GetRootChild(name);
            return GetComponent<T>(go.gameObject);
        }

        /// <summary>
        /// 获取一个GameObject下的组件，组件不存布则创添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <returns></returns>
        public static T GetComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
        public static Component GetComponent(Type type, GameObject go)
        {
            return GetComponent(go, type);
        }
        public static Component GetComponent(GameObject go, Type type)
        {
            return go.GetComponent(type) ?? go.AddComponent(type);
        }


        /// <summary>
        /// 在指定父级下创建一个新的GameObject
        /// </summary>
        /// <param name="parent">父级</param>
        /// <param name="name">GameObject名字</param>
        /// <returns></returns>
        public static GameObject CreateChild(string name, Transform parent)
        {

            GameObject go = name != null ? new GameObject(name) : new GameObject();

            if (parent != null)
            {
                SetChildParent(go, parent);
                go.layer = parent.gameObject.layer;
                Transform t = go.transform;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            return go;
        }
        public static GameObject CreateUGUIChild(string name, Transform parent)
        {
            GameObject go = CreateChild(name, parent);
            go.AddComponent<RectTransform>();

            return go;
        }

        public static GameObject CreateChild(GameObject prefab, Transform parent)
        {
            GameObject go = GameObject.Instantiate(prefab, parent) as GameObject;
            if (go != null)
                go.name = prefab.name;
            return go;
        }
        public static T CreateChild<T>(GameObject prefab, Transform parent) where T : Component
        {
            GameObject go = CreateChild(prefab, parent);
            if (go != null)
                return go.GetComponent<T>();
            return null;
        }
        public static bool IsPrefabs(this GameObject prefab)
        {
            if (prefab == null)
                return false;
            if (prefab.scene.name == null)
            {
                if (prefab.activeInHierarchy)
                    return false;
                else if (prefab.transform.parent != null)
                    return false;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 添加一个预置到指定父级下
        /// </summary>
        /// <param name="prefab">已加载的预置</param>
        /// <param name="parent">父级</param>
        /// <returns></returns>
        public static GameObject AddChild(GameObject prefab, Transform parent)
        {
            GameObject go = prefab;
            if (IsPrefabs(prefab))
            {
                go = GameObject.Instantiate(prefab, parent) as GameObject;
                go.name = prefab.name;
            }
            else if (go != null)
            {
                if (parent != go.transform.parent)
                    SetChildParent(go, parent, false);
            }

            return go;
        }
        public static T AddChild<T>(GameObject prefab, Transform parent) where T : Component
        {
            GameObject go = AddChild(prefab, parent);
            if (go != null)
                return go.GetComponent<T>();
            else
                return null;
        }
        /// <summary>
        /// 添加一个预置到指定父级下
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="prefab">预置的路径</param>
        /// <returns></returns>
        public static GameObject AddChild(string prefab, Transform parent)
        {
            GameObject go = Resources.Load(prefab) as GameObject;
            return AddChild(go, parent);
        }

        /// <summary>
        /// 把一对象移动另一父级下
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        public static void SetChildParent(GameObject child, Transform parent, bool worldPositionStays = false)
        {
            Transform t = child.transform;
            if (t.parent != parent)
                t.SetParent(parent, worldPositionStays);
            //t.parent=parent;
            // SetLayer(child, parent.gameObject.layer);
        }
        /// <summary>
        /// 设置一个GameObject及其所有子对象的layer
        /// </summary>
        /// <param name="go"></param>
        /// <param name="layer"></param>
        public static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0, imax = t.childCount; i < imax; ++i)
            {
                Transform child = t.GetChild(i);
                SetLayer(child.gameObject, layer);
            }
        }


        /// <summary>
        /// 把一个gameObject移至指定父级的路径下 ,路径使用"/"格式
        /// </summary>
        /// <param name="g"></param>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        public static void MoveIn(Transform transform, string path, Transform parent = null)
        {
            string[] array = path.Split(new Char[] { '/' });
            GameObject go = null;
            if (parent == null)
            {
                go = GameObject.Find(array[0]);
                if (go == null)
                    go = new GameObject(array[0]);
            }
            else
            {
                Transform tf = parent.Find(array[0]);
                if (tf == null)
                    go = new GameObject(array[0]);
                if (go != null)
                    go.transform.parent = tf;
            }
            if (go != null)
            {
                for (int i = 1; i < array.Length; i++)
                {
                    string str = array[i];
                    Transform tf = go.transform.Find(str);
                    if (tf == null)
                    {
                        GameObject child = new GameObject(str);
                        child.transform.parent = go.transform;
                        go = child;
                    }
                    else
                        go = tf.gameObject;
                }
                transform.parent = go.transform;
            }
        }


        public static Transform Find(Transform parent, string name)
        {
            Transform tf = null;
            if (parent != null)
            {
                if (parent.gameObject.activeInHierarchy)
                    tf = parent.Find(name);
                else
                {
                    foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.parent == parent && child.name == name)
                        {
                            tf = child;
                            break;
                        }

                    }
                }
            }
            return tf;
        }

        public static Vector2 MouseToLocalPoint(Transform tf)
        {
            return ScreenPointToLocalPoint(tf, Input.mousePosition);
        }
        public static Vector2 ScreenPointToLocalPoint(Transform tf, Vector2 screenPoint)
        {
            Vector2 pos;
            RectTransform parent = tf as RectTransform;
            if (parent != null)
            {
                Canvas canvas = tf.GetComponentInParent<Canvas>();
                canvas = canvas.rootCanvas;
                Camera cam = null;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    cam = canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out pos))
                {
                    return pos;
                }
            }
            return new Vector2(0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="upOrDown">0:自动判断1：上，-1：下</param>
        public static void SetPosByMouse(Transform transform, int upOrDown = 0)
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null)
                return;

            RectTransform parent = rt.transform.parent as RectTransform;
            if (parent != null)
            {
                Vector2 pos = BootTool.MouseToLocalPoint(rt.parent);

                float y = pos.y;
                if (upOrDown == 0)
                {
                    if (y <= 0)
                        upOrDown = -1;
                    else
                        upOrDown = 1;
                }
                if (upOrDown == -1)
                {
                    if (y + rt.rect.height > parent.rect.max.y)
                        y = pos.y - rt.rect.max.y;
                    else
                        y = pos.y - rt.rect.min.y;
                }
                else
                {
                    if (y - rt.rect.height < parent.rect.min.y)
                        y = pos.y - rt.rect.min.y;
                    else
                        y = pos.y - rt.rect.max.y;
                }
                rt.localPosition = new Vector3(pos.x, y);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceTransform"></param>
        /// <param name="targetTransform"></param>
        /// <param name="upOrDown">0:自动判断1：上，-1：下</param>
        public static void SetPosByTarget(Transform sourceTransform, Transform targetTransform, int upOrDown = 0)
        {
            RectTransform rt = sourceTransform as RectTransform;
            RectTransform target = targetTransform as RectTransform;
            if (rt == null || target == null)
                return;
            RectTransform parent = rt.transform.parent as RectTransform;
            RectTransform targetParent = target.transform.parent as RectTransform;
            if (parent == null || targetParent == null)
                return;

            float x = -target.pivot.x * target.rect.width + target.rect.width / 2;
            float y = -target.pivot.y * target.rect.height + target.rect.height / 2;
            Canvas canvas = target.GetComponentInParent<Canvas>();
            canvas = canvas.rootCanvas;
            if (canvas == null)
                return;
            Transform tf = target;
            float scaleX = 1;
            float scaleY = 1;
            while (true)
            {
                if (tf == null || tf == canvas.transform)
                {
                    break;
                }
                else
                {
                    x = x * tf.localScale.x + tf.transform.localPosition.x;
                    y = y * tf.localScale.y + tf.transform.localPosition.y;
                    scaleX = scaleX * tf.localScale.x;
                    scaleY = scaleY * tf.localScale.y;
                    tf = tf.parent;
                }
            }
            if (upOrDown == 0)
            {
                if (y <= 0)
                    upOrDown = -1;
                else
                    upOrDown = 1;
            }
            if (upOrDown == -1)
            {
                if (y + rt.rect.height + target.rect.height * scaleX / 2 > parent.rect.max.y)
                    y = y - target.rect.height * scaleX / 2 - rt.rect.max.y;
                else
                    y = y + target.rect.height * scaleY / 2 - rt.rect.min.y;
            }
            else
            {
                if (y - rt.rect.height - target.rect.height * scaleX / 2 < parent.rect.min.y)
                    y = y + target.rect.height * scaleY / 2 - rt.rect.min.y;
                else
                    y = y - target.rect.height * scaleX / 2 - rt.rect.max.y;
            }
            rt.localPosition = new Vector3(x, y);
        }
        public static void ClampInParent(RectTransform rt)
        {
            if (rt == null)
                return;
            RectTransform parent = rt.parent as RectTransform;
            Vector3 pos = rt.localPosition;
            if (parent != null)
            {

                Vector3 minPosition = parent.rect.min - rt.rect.min;
                Vector3 maxPosition = parent.rect.max - rt.rect.max;
                if (minPosition.x < maxPosition.x)
                    pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
                else
                    pos.x = Mathf.Clamp(pos.x, maxPosition.x, minPosition.x);
                if (minPosition.y < maxPosition.y)
                    pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);
                else
                    pos.y = Mathf.Clamp(pos.y, maxPosition.y, minPosition.y);
            }
            rt.localPosition = pos;
        }
        public static void ClampSize(RectTransform rt, float minWidth, float minHeight, float maxWidth, float maxHeight)
        {
            if (rt == null)
                return;

            float w = Mathf.Clamp(rt.rect.width, minWidth, maxWidth > 0 ? maxWidth : float.PositiveInfinity);
            float h = Mathf.Clamp(rt.rect.height, minHeight, maxHeight > 0 ? maxHeight : float.PositiveInfinity);
            if (rt.rect.width != w || rt.rect.height != h)
                rt.sizeDelta = new Vector2(w, h);
        }


        public static void AddOrderInLayer(GameObject go, int order, bool include = true)
        {
            if (go == null)
                return;

            SortingGroup[] renders = go.GetComponentsInChildren<SortingGroup>(true);
            if (renders != null)
            {
                for (int i = 0; i < renders.Length; i++)
                {
                    var render = renders[i];
                    if (include || render.gameObject != go)
                        render.sortingOrder += order;
                }
            }
            Canvas[] canvases = go.GetComponentsInChildren<Canvas>(true);
            if (canvases != null)
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    var canvase = canvases[i];
                    if (include || canvase.gameObject != go)
                        canvases[i].sortingOrder += order;
                }
            }

        }

        public static void SetSortingLayer(GameObject go, Transform layer)
        {
            //if (go == null)
            //    return;
            //Canvas canvas = layer.GetComponent<Canvas>();
            //if (canvas == null)
            //    canvas = layer.GetComponentInParent<Canvas>();
            //if (canvas != null)
            //{
            //    if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            //    {
            //        var layerId = canvas.sortingLayerID;
            //        var renders = go.GetComponentsInChildren<SortingGroup>(true);
            //        if (renders != null)
            //        {
            //            for (int i = 0; i < renders.Length; i++)
            //            {
            //                renders[i].sortingLayerID = layerId;
            //            }
            //        }
            //        Canvas[] canvases = go.GetComponentsInChildren<Canvas>(true);
            //        if (canvases != null)
            //        {
            //            for (int i = 0; i < canvases.Length; i++)
            //            {
            //                canvases[i].sortingLayerID = layerId;
            //            }
            //        }
            //    }
            //}
        }


        public static void SetLayer(GameObject go, Transform layer)
        {
            if (go == null)
                return;
            var l = layer.gameObject.layer;
            var renders = go.GetComponentsInChildren<Renderer>(true);
            if (renders != null)
            {
                for (int i = 0; i < renders.Length; i++)
                {
                    renders[i].gameObject.layer = l;
                }
            }
            var canvasRenderers = go.GetComponentsInChildren<CanvasRenderer>(true);
            if (canvasRenderers != null)
            {
                for (int i = 0; i < canvasRenderers.Length; i++)
                {
                    canvasRenderers[i].gameObject.layer = l;
                }
            }
            var canvas = go.GetComponentsInChildren<Canvas>(true);
            if (canvas != null)
            {
                for (int i = 0; i < canvas.Length; i++)
                {
                    canvas[i].gameObject.layer = l;
                }
            }
        }

        public static void SetSiblingIndexBefore(this Transform tf, Transform target)
        {
            SetChildParent(tf.gameObject, target.parent);
            int index = tf.GetSiblingIndex();
            int targetIndex = target.GetSiblingIndex();
            if (index > targetIndex)
                tf.SetSiblingIndex(targetIndex + 1);
            else
                tf.SetSiblingIndex(targetIndex);
        }
        public static void SetSiblingIndexAfter(this Transform tf, Transform target)
        {
            SetChildParent(tf.gameObject, target.parent);
            int index = tf.GetSiblingIndex();
            int targetIndex = target.GetSiblingIndex();
            if (index > targetIndex)
                tf.SetSiblingIndex(targetIndex);
            else
                tf.SetSiblingIndex(targetIndex - 1);
        }

        public static void Center(this GameObject tf)
        {
            Center(tf.transform);
        }

        public static void Center(this Transform tf)
        {
            RectTransform rt = tf as RectTransform;
            RectTransform parent = rt.parent as RectTransform;
            float x = rt.pivot.x * rt.sizeDelta.x - rt.sizeDelta.x / 2;
            float y = rt.pivot.y * rt.sizeDelta.y - rt.sizeDelta.y / 2;
            if (parent != null)
            {
                float cx = parent.pivot.x * parent.rect.width - parent.rect.width / 2;
                float cy = parent.pivot.y * parent.rect.height - parent.rect.height / 2;
                x = x + cx;
                y = y + cy;
            }
            rt.localPosition = new Vector3(x, y, 0);
        }



        public static bool IsAlive(this GameObject go)
        {
            if (go != null)
            {
                try
                {
                    if (go.name != null)
                        return true;
                    else
                        return false;
                }
                catch
                {
                    //Debug.LogError(ee);
                    return false;
                }
            }
            return false;
        }

        public static bool IsAlive(Component com)
        {
            if (com != null)
            {
                try
                {
                    return IsAlive(com.gameObject);
                }
                catch
                {
                    // Debug.LogError(ee);
                    return false;
                }
            }
            return false;
        }

        public static bool IsAlive(this UnityEngine.Object obj)
        {
            if (obj != null)
            {
                try
                {
                    if (obj.hideFlags >= 0)
                        return true;

                    else
                        return false;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        public static bool IsAlive(this Delegate fun)
        {
            if (fun != null)
            {
                object target = fun.Target;
                if (target == null)
                {
                    if (fun.Method.IsStatic)
                        return true;
                    return false;
                }
                else if (target is UnityEngine.Object)
                {
                    return IsAlive((UnityEngine.Object)target);
                }
                return true;

            }
            return false;
        }




        public static void DataFrom(this IDictionary dic, IEnumerable keys, IEnumerable values)
        {
            IEnumerator keyer = keys.GetEnumerator();
            IEnumerator valuer = values.GetEnumerator();
            while (keyer.MoveNext())
            {
                valuer.MoveNext();
                if (keyer.Current != null && dic.Contains(keyer.Current) == false)
                    dic.Add(keyer.Current, valuer.Current);
            }
        }

        public static T CreateMono<T>(string gameObjectName, bool findAndDestroyOld, bool dontDestroyOnLoad, HideFlags flags = HideFlags.None) where T : MonoBehaviour
        {
            GameObject go = null;
            if (findAndDestroyOld)
            {
                go = GameObject.Find(gameObjectName);
                if (go != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(go);
                    else
                        UnityEngine.Object.DestroyImmediate(go);
                }
            }

            go = new GameObject(gameObjectName);
            go.hideFlags = flags;

            var mono = go.AddComponent<T>();

            if (Application.isPlaying && dontDestroyOnLoad)
                GameObject.DontDestroyOnLoad(go);
            return mono;
        }

        public static bool IsMobilePlatform()
        {
            return Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer;
        }

        public static bool IsIPhonePlatform()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer;
        }

        public static bool IsAndroidPlatform()
        {
            return Application.platform == RuntimePlatform.Android;
        }


        public static void RemoveAllChildren(this Transform tran)
        {
            var n = tran.childCount;
            for (var i = 0; i < n; i++)
            {
                if (tran.childCount > 0)
                {
                    var child = tran.GetChild(tran.childCount - 1);
                    UnityEngine.GameObject.Destroy(child.gameObject);
                }

            }

        }

    }
}
