using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace xasset.editor
{
    public class SpriteAtlasFinder : EditorWindow
    {
        private List<SpriteAtlas> allAtlases = new List<SpriteAtlas>();
        private Vector2 atlasScrollPos;
        private Vector2 resultScrollPos;
        private Sprite targetSprite;
        private Object targetTexture;
        private string searchResult = "";
        private bool showAllAtlases = true;

        [MenuItem("Window/xasset/Sprite Atlas Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteAtlasFinder>("图集查找工具");
            window.minSize = new Vector2(400, 300);
            window.RefreshAllAtlases();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 刷新所有图集按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新所有图集", GUILayout.Height(30)))
            {
                RefreshAllAtlases();
            }
            EditorGUILayout.LabelField($"找到 {allAtlases.Count} 个图集", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 显示所有图集列表
            if (showAllAtlases)
            {
                EditorGUILayout.LabelField("所有图集列表:", EditorStyles.boldLabel);
                atlasScrollPos = EditorGUILayout.BeginScrollView(atlasScrollPos, GUILayout.Height(200));
                foreach (var atlas in allAtlases)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(atlas.name, EditorStyles.linkLabel))
                    {
                        Selection.activeObject = atlas;
                        EditorGUIUtility.PingObject(atlas);
                    }
                    EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(atlas), EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 查找指定图片所属图集
            EditorGUILayout.LabelField("查找图片所属图集:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 支持选择Sprite或Texture2D
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标图片:", GUILayout.Width(80));
            targetSprite = EditorGUILayout.ObjectField(targetSprite, typeof(Sprite), false) as Sprite;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("或Texture2D:", GUILayout.Width(80));
            targetTexture = EditorGUILayout.ObjectField(targetTexture, typeof(Texture2D), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("查找", GUILayout.Height(30)))
            {
                FindSpriteInAtlas();
            }
            if (GUILayout.Button("清空结果", GUILayout.Height(30)))
            {
                searchResult = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 显示查找结果
            if (!string.IsNullOrEmpty(searchResult))
            {
                EditorGUILayout.LabelField("查找结果:", EditorStyles.boldLabel);
                resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos, GUILayout.Height(150));
                EditorGUILayout.TextArea(searchResult, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void RefreshAllAtlases()
        {
            allAtlases.Clear();
            var guids = AssetDatabase.FindAssets("t:SpriteAtlas");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas != null)
                {
                    allAtlases.Add(atlas);
                }
            }
            allAtlases = allAtlases.OrderBy(a => a.name).ToList();
            Debug.Log($"找到 {allAtlases.Count} 个图集");
        }

        private void FindSpriteInAtlas()
        {
            searchResult = "";
            var foundAtlases = new List<SpriteAtlas>();
            string targetPath = "";

            // 如果选择了Sprite
            if (targetSprite != null)
            {
                targetPath = AssetDatabase.GetAssetPath(targetSprite);
                searchResult += $"查找图片: {targetSprite.name}\n";
                searchResult += $"路径: {targetPath}\n";
                searchResult += "----------------------------------------\n\n";

                // 获取Sprite对应的纹理路径
                var spriteImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (spriteImporter != null)
                {
                    // 检查每个图集
                    foreach (var atlas in allAtlases)
                    {
                        var objects = atlas.GetPackables();
                        foreach (var obj in objects)
                        {
                            var objPath = AssetDatabase.GetAssetPath(obj);
                            // 检查是否是同一个资源或同一个纹理
                            if (objPath == targetPath)
                            {
                                if (!foundAtlases.Contains(atlas))
                                    foundAtlases.Add(atlas);
                                break;
                            }
                            
                            // 如果是文件夹，检查是否包含目标路径
                            if (obj is DefaultAsset)
                            {
                                var folderPath = objPath;
                                if (targetPath.StartsWith(folderPath))
                                {
                                    if (!foundAtlases.Contains(atlas))
                                        foundAtlases.Add(atlas);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // 如果选择了Texture2D
            else if (targetTexture != null && targetTexture is Texture2D)
            {
                targetPath = AssetDatabase.GetAssetPath(targetTexture);
                searchResult += $"查找图片: {targetTexture.name}\n";
                searchResult += $"路径: {targetPath}\n";
                searchResult += "----------------------------------------\n\n";

                foreach (var atlas in allAtlases)
                {
                    var objects = atlas.GetPackables();
                    foreach (var obj in objects)
                    {
                        var objPath = AssetDatabase.GetAssetPath(obj);
                        if (objPath == targetPath)
                        {
                            if (!foundAtlases.Contains(atlas))
                                foundAtlases.Add(atlas);
                            break;
                        }
                        
                        // 如果是文件夹，检查是否包含目标路径
                        if (obj is DefaultAsset)
                        {
                            var folderPath = objPath;
                            if (targetPath.StartsWith(folderPath))
                            {
                                if (!foundAtlases.Contains(atlas))
                                    foundAtlases.Add(atlas);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                searchResult = "请先选择一个Sprite或Texture2D资源";
                return;
            }

            // 显示结果
            if (foundAtlases.Count > 0)
            {
                searchResult += $"找到 {foundAtlases.Count} 个包含该图片的图集:\n\n";
                for (int i = 0; i < foundAtlases.Count; i++)
                {
                    var atlas = foundAtlases[i];
                    var atlasPath = AssetDatabase.GetAssetPath(atlas);
                    searchResult += $"{i + 1}. {atlas.name}\n";
                    searchResult += $"   路径: {atlasPath}\n\n";
                }
            }
            else
            {
                searchResult += "未找到包含该图片的图集";
            }
        }
    }
}

