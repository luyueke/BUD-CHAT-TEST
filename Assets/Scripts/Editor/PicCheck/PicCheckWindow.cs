using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PicCheckWindow : OdinMenuEditorWindow
{
    public List<BigTextureInfo> bigTextureInfos;
    
    private static readonly Color professionalColor = new Color(56f / 255, 56f / 255, 56f / 255, 1);
    private static readonly Color personaloColor = new Color(194f / 255, 194f / 255, 194f / 255, 1);
    
    public CheckFolderWindow checkFolderWindow;
    public FilterUIWindow filterUIWindow;
    public FilterUIPicWindow filterUIPicWindow;
    public FilterPicWindow filterPicWindow;
    
    private static bool isShowRef = true;
    private static string TEXTUREREF = "TEXTUREREF";
    public static Dictionary<string, List<string>> textureRefDependencies;
    
    [MenuItem("Tools/图片检测/图片筛选")]
    public static void OpenWindow()
    {
        var window = GetWindow<PicCheckWindow>("图片筛选");
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
    }

    [MenuItem("Tools/图片检测/显示无引用贴图")]
    public static void ShowDepends()
    {
        isShowRef = !isShowRef;
        EditorPrefs.SetBool(TEXTUREREF, isShowRef);
        RefreshTextureRefDependencies();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/重置资源引用关系")]
    public static void RefreshTextureRefDependencies()
    {
        RefreshReverseDependencies();
    }

    [InitializeOnLoadMethod]
    private static void InitializeOnLoadMethod()
    {
        EditorApplication.projectChanged += Init;
        //在ProjectWindow中，为每个可见列表项委派OnGUI事件。
        EditorApplication.projectWindowItemOnGUI += OnGUI;
    }

    private static void Init()
    {
        // isShowRef = EditorPrefs.GetBool(TEXTUREREF);
        // RefreshTextureRefDependencies();

    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // RefreshTextureRefDependencies();
    }


    private static void RefreshTextureRefDependencies(bool isForce = false)
    {
        isShowRef = EditorPrefs.GetBool(TEXTUREREF);

        if (isForce)
        {
            textureRefDependencies?.Clear();
            var infoPath = Path.Combine(Application.dataPath, "..","referenceInfos.json");
            if (File.Exists(infoPath))
            {
                var bigFileContent = File.ReadAllText(infoPath);
                textureRefDependencies = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bigFileContent);    
            }
        }
        else
        {
            if (textureRefDependencies == null || textureRefDependencies.Count == 0)
            {
                var infoPath = Path.Combine(Application.dataPath, "..","referenceInfos.json");
                if (File.Exists(infoPath))
                {
                    var bigFileContent = File.ReadAllText(infoPath);
                    textureRefDependencies = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bigFileContent);    
                }
            }
        }
    }

    public static List<string> GetRefDependencies(string path) {
        if (textureRefDependencies == null || textureRefDependencies.Count == 0)
        {
            return null;
        } else {
            if (textureRefDependencies.TryGetValue(path, out var refDependencies))
            {
                return refDependencies;
            } else {
                return null;
            }
        }
    }


    //刷新编辑器ui
    private static void OnGUI(string guid, Rect selectionRect)
    {
        if (textureRefDependencies == null)
        {
            return;
        }
        // var dataPath = Application.dataPath;
        // var startIndex = dataPath.LastIndexOf(REMOVE_STR);
        // var dir = dataPath.Remove(startIndex, mRemoveCount);
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (IsTexture(path) &&!textureRefDependencies.ContainsKey(path))
        {
            var text = "NoRef";

            var label = EditorStyles.label;
            var content = new GUIContent(text);
            var width = label.CalcSize(content).x + 10;

            var pos = selectionRect;
            pos.x = pos.xMax - width;
            pos.width = width;

            EditorGUI.DrawRect(pos, UseDark() ? professionalColor : personaloColor);
            Color defaultC = GUI.color;
            GUI.color = Color.red;
            GUI.Label(pos, text);
            GUI.color = defaultC;
        }
    }
    private static bool UseDark()
    {
        PropertyInfo propertyInfo = typeof(EditorGUIUtility).GetProperty("skinIndex", BindingFlags.Static | BindingFlags.NonPublic);
        bool useDark = (int)propertyInfo.GetValue(null) == 1;
        return useDark;
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        if (textureRefDependencies == null || textureRefDependencies.Count == 0)
        {
            LoadReverseDependencies();
        }

        var odinMenuTree = new OdinMenuTree(false);
        filterPicWindow = new FilterPicWindow(this);
        odinMenuTree.Add("大图集筛选", filterPicWindow);
        filterUIWindow = new FilterUIWindow();
        odinMenuTree.Add("UI预制筛选", filterUIWindow);
        filterUIPicWindow = new FilterUIPicWindow();
        odinMenuTree.Add("UI贴图引用筛选", filterUIPicWindow);
        odinMenuTree.Add("反向引用查找", new FilterDependencyWindow());
        checkFolderWindow = new CheckFolderWindow(this);
        odinMenuTree.Add("查找文件列表", checkFolderWindow);
        return odinMenuTree;
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (checkFolderWindow != null)
        {
            checkFolderWindow.OnDispose();
        }

        if (filterUIWindow != null)
        {
            filterUIWindow.OnDispose();
        }

        if (filterUIPicWindow != null)
        {
            filterUIPicWindow.OnDispose();
        }

        if (filterPicWindow != null)
        {
            filterPicWindow.OnDispose();
        }

        Resources.UnloadUnusedAssets();
    }


    private void LoadReverseDependencies()
    {
        var infoPath = Path.Combine(Application.dataPath, "..","referenceInfos.json");
        if (File.Exists(infoPath))
        {
            var bigFileContent = File.ReadAllText(infoPath);
            textureRefDependencies = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bigFileContent);
        }
    }


    protected override void OnGUI()
    {
        if (GUILayout.Button("更新资源引用信息"))
        {
            RefreshReverseDependencies();
        }
        base.OnGUI();
    }

    private static bool IsTexture(string path)
    {
        return path.EndsWith(".png") || path.EndsWith(".jpg");
    }


    private static void RefreshReverseDependencies() {
        var tmpReverseDependencies = new Dictionary<string, List<string>>() {};
        tmpReverseDependencies.Clear();
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        int prefabCount = 0;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < allAssetPaths.Length; i++)
        {
            var tmpAsset = allAssetPaths[i];
            if (tmpAsset.EndsWith(".prefab") || tmpAsset.EndsWith(".mat"))
            {
                var dependencies = new HashSet<string>();
                var content = File.ReadAllText(tmpAsset);
                string pattern = @"guid: ([0-9a-fA-F]+)";
                MatchCollection matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value != "0")
                    {
                        dependencies.Add(match.Groups[1].Value);
                    }
                }
                foreach (var dependencyAsset in dependencies)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(dependencyAsset);
                    if (!tmpReverseDependencies.TryGetValue(assetPath, out var parentAssets))
                    {
                        parentAssets = new List<string>();
                        tmpReverseDependencies.Add(assetPath, parentAssets);
                    }

                    if (assetPath != tmpAsset && !parentAssets.Contains(tmpAsset))
                    {
                        parentAssets.Add(tmpAsset);
                    }
                }
                prefabCount++;
            }
        }
        stopwatch.Stop();
        Debug.Log("RefreshReverseDependencies:" + prefabCount + "," + stopwatch.ElapsedMilliseconds);

        var referenceInfoPath = Path.Combine(Application.dataPath, "..", "referenceInfos.json");
        File.WriteAllText(referenceInfoPath, JsonConvert.SerializeObject(tmpReverseDependencies));

        RefreshTextureRefDependencies(true);
    }
}