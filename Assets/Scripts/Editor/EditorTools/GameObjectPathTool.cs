using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class GameObjectPathTool : EditorWindow
{
    private Vector2 scrollPosition;
    private bool autoRefresh = true;
    private float refreshInterval = 1.0f;
    private float lastRefreshTime;
    private List<string> pathHistory = new List<string>();
    private bool showHistory = true;
    private int maxHistoryCount = 10;
    private bool includeComponents = true;
    private string searchFilter = "";
    private GUIStyle pathStyle;

    [MenuItem("Tools/GameObject Path Tool %#p")] // Ctrl+Shift+P
    public static void ShowWindow()
    {
        GetWindow<GameObjectPathTool>("路径查看器");
    }

    private void OnEnable()
    {
        // 初始化样式
        pathStyle = new GUIStyle();
        pathStyle.normal.textColor = Color.white;
        pathStyle.wordWrap = true;
        pathStyle.richText = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawToolbar();
        DrawSearchBar();
        DrawSelectedObjectInfo();
        DrawPathHistory();

        EditorGUILayout.EndVertical();

        // 自动刷新
        if (autoRefresh && EditorApplication.isPlaying)
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastRefreshTime >= refreshInterval)
            {
                lastRefreshTime = currentTime;
                Repaint();
            }
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 自动刷新开关
        autoRefresh = EditorGUILayout.ToggleLeft("自动刷新", autoRefresh, GUILayout.Width(70));

        if (autoRefresh)
        {
            refreshInterval = EditorGUILayout.Slider(refreshInterval, 0.1f, 5f, GUILayout.Width(100));
            EditorGUILayout.LabelField("秒", GUILayout.Width(20));
        }

        includeComponents = EditorGUILayout.ToggleLeft("显示组件", includeComponents, GUILayout.Width(80));

        if (GUILayout.Button("复制路径", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            CopySelectedObjectPath();
        }

        if (GUILayout.Button("清除历史", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            pathHistory.Clear();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchFilter = EditorGUILayout.TextField("搜索", searchFilter, EditorStyles.toolbarSearchField);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSelectedObjectInfo()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前选中物体:", EditorStyles.boldLabel);

        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            string path = GetGameObjectPath(selectedObject);

            // 如果有搜索过滤
            if (!string.IsNullOrEmpty(searchFilter) && !path.ToLower().Contains(searchFilter.ToLower()))
            {
                EditorGUILayout.HelpBox("当前路径不匹配搜索条件", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 显示基本信息
            EditorGUILayout.LabelField("名称:", selectedObject.name);
            EditorGUILayout.LabelField("路径:", path, pathStyle);

            // 显示组件信息
            if (includeComponents)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("组件列表:", EditorStyles.boldLabel);
                Component[] components = selectedObject.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component != null)
                    {
                        EditorGUILayout.LabelField($"- {component.GetType().Name}", pathStyle);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            // 添加到历史记录
            if (!pathHistory.Contains(path))
            {
                pathHistory.Insert(0, path);
                if (pathHistory.Count > maxHistoryCount)
                {
                    pathHistory.RemoveAt(pathHistory.Count - 1);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请选择一个场景物体", MessageType.Info);
        }
    }

    private void DrawPathHistory()
    {
        showHistory = EditorGUILayout.Foldout(showHistory, "历史记录", true);
        if (showHistory && pathHistory.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < pathHistory.Count; i++)
            {
                string path = pathHistory[i];

                // 搜索过滤
                if (!string.IsNullOrEmpty(searchFilter) && !path.ToLower().Contains(searchFilter.ToLower()))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                if (GUILayout.Button(path, EditorStyles.label))
                {
                    // 点击路径时查找并选中对象
                    GameObject obj = FindObjectByPath(path);
                    if (obj != null)
                    {
                        Selection.activeGameObject = obj;
                    }
                }

                if (GUILayout.Button("复制", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    GUIUtility.systemCopyBuffer = path;
                    Debug.Log($"已复制路径: {path}");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return string.Empty;

        StringBuilder path = new StringBuilder(obj.name);
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path.Insert(0, "/");
            path.Insert(0, parent.name);
            parent = parent.parent;
        }

        return path.ToString();
    }

    private GameObject FindObjectByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string[] elements = path.Split('/');
        GameObject current = null;

        // 查找根物体
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == elements[0])
            {
                current = root;
                break;
            }
        }

        if (current == null) return null;

        // 遍历路径
        for (int i = 1; i < elements.Length; i++)
        {
            Transform child = current.transform.Find(elements[i]);
            if (child == null) return null;
            current = child.gameObject;
        }

        return current;
    }

    private void CopySelectedObjectPath()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            string path = GetGameObjectPath(selectedObject);
            GUIUtility.systemCopyBuffer = path;
            Debug.Log($"已复制路径: {path}");
        }
    }
}