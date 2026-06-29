using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ChangeShaderAttribute
{
    public delegate Shader FindShaderDelegate(string name);
    
    public static Dictionary<string, string> attrNames = new Dictionary<string, string>()
    {
        {"_BaseMap","_MainTex"}
        ,{"_BaseColor","_BaseColor"}
        ,{"_BumpMap","_Normal"}
        ,{"_BumpScale","_Normal_int"}
        ,{"_MraMap","_mra_tex"}
        ,{"_Metallic","_metalic"}
        ,{"_Smoothness","_Smoothness"}
        ,{"_OcclusionStrength","_AO"}
        ,{"_EmissionMap","_Emisson"}
        ,{"_EmissionColor","_Emisson_Color"}
        ,{"_Cutoff","_AlphaClip2"}
        ,{"_CullMode","_CullMode"}
        ,{"_Matcap","_Matcap"}
    };

    private static string shaderPath = "Assets/Arts/Shader";

    [MenuItem("Assets/切换Shader属性名称")]
    public static void ChangeShader()
    {
        string[] guids = Selection.assetGUIDs;
        List<Material> allMats = new List<Material>();
        List<Shader> allShaders = new List<Shader>();
        foreach(var guid in guids)
        {
            // 将 GUID 转换为 路径
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (File.Exists(assetPath))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                allMats.Add(mat);
            }
            else
            {
                GetAllFiles(assetPath,allMats);
            }
        }
        GetAllFiles(shaderPath,allShaders);
        ReplaceShader(allMats,allShaders);
    }

    private static void GetAllFiles<T>(string path,List<T> res) where T : UnityEngine.Object
    {
        DirectoryInfo direction = new DirectoryInfo(path);
        FileInfo[] files = direction.GetFiles("*", SearchOption.TopDirectoryOnly);
        DirectoryInfo[] folders = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
        for(int i = 0; i < folders.Length; i++)
        {
            GetAllFiles(folders[i].FullName,res);
        }
        for(int i = 0; i < files.Length; i++)
        {
            if (typeof(T).Name.Contains("Material") && files[i].Name.EndsWith(".mat"))
            {
                string orgPath = files[i].FullName.Replace('\\','/');
                string matPath = "Assets" + orgPath.Replace(Application.dataPath, ""); 
                var mat = AssetDatabase.LoadAssetAtPath<T>(matPath);
                res.Add(mat);
            }
            if (typeof(T).Name.Contains("Shader") && files[i].Name.EndsWith(".shader"))
            {
                string orgPath = files[i].FullName.Replace('\\','/');
                string matPath = "Assets" + orgPath.Replace(Application.dataPath, ""); 
                var mat = AssetDatabase.LoadAssetAtPath<T>(matPath);
                res.Add(mat);
            }
        }
    }


    public static void ReplaceShader(List<Material> mats,List<Shader> shaders)
    {
        if(mats.Count == 0 || shaders.Count== 0)
            return;
        FindShaderDelegate findShader = (name =>
        {
            for (var i = 0; i < shaders.Count; i++)
            {
                if (shaders[i].name.Contains(name + "_1"))
                {
                    return shaders[i];
                }
            }
            return null;
        });

      
        for (var i = 0; i < mats.Count; i++)
        {
            Dictionary<string, Texture> allTex = new Dictionary<string, Texture>();
            Dictionary<string, float> allFloats = new Dictionary<string, float>();
            Dictionary<string, Color> allColor = new Dictionary<string, Color>();
            var org = mats[i].shader;
            var newShader = findShader(org.name);
            if (newShader != null)
            {
                foreach (var val in attrNames)
                {
                    if (mats[i].HasTexture(val.Value))
                    {
                        allTex.Add(val.Value,mats[i].GetTexture(val.Value));
                    }
                    if (mats[i].HasFloat(val.Value))
                    {
                        allFloats.Add(val.Value,mats[i].GetFloat(val.Value));
                    }
                    if (mats[i].HasColor(val.Value))
                    {
                        allColor.Add(val.Value,mats[i].GetColor(val.Value));
                    }
                }
                mats[i].shader = newShader;
                foreach (var val in attrNames)
                {
                    string oldName = attrNames[val.Key];

                    if (mats[i].HasTexture(val.Key) && allTex.ContainsKey(oldName))
                    {
                        mats[i].SetTexture(val.Key,allTex[oldName]);
                    }
                    if (mats[i].HasFloat(val.Key) && allFloats.ContainsKey(oldName))
                    {
                        mats[i].SetFloat(val.Key,allFloats[oldName]);
                    }
                    if (mats[i].HasColor(val.Key) && allColor.ContainsKey(oldName))
                    {
                        mats[i].SetColor(val.Key,allColor[oldName]);
                    }
                }
            }


        }
    }
}
