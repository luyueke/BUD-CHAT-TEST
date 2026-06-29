using System.IO;
using UnityEditor;
using UnityEngine;

public class ExtractAnimationClip
{
    private const string FBXExtension = ".FBX";
    private static readonly string[] CopyToFileName = new string[] { "Arts", "AnimationsFBX" };
    private static readonly string[] OriginFileName = new string[] { "Loadable", "Animations" };
    private const string AnimationPath = "Assets/Loadable/Animations";
    private const string FBPXPath = "Assets/Arts/AnimationsFBX";

    [MenuItem("Assets/Animation/ExtractAnimationClip")]
    private static void Extract()
    {
        var assetGUIDs = Selection.assetGUIDs;
        if (assetGUIDs.Length == 0) return;

        foreach (var guid in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.Contains(AnimationPath))
            {
                LoggerUtils.LogError("Choose the correct directory");

                continue;
            }

            if (Directory.Exists(path))
            {
                DirectoryInfo dInfo = new DirectoryInfo(path);

                FileInfo[] fbxFileInfo = dInfo.GetFiles("*" + FBXExtension, SearchOption.AllDirectories);
                foreach (var fbxFile in fbxFileInfo)
                {
                    ExtractAniMoveFbx(ToRelativePath(fbxFile.FullName));
                }
            }
            else
            {
                string fileExtension = Path.GetExtension(path);

                if (FBXExtension.Equals(Path.GetExtension(path)))
                {
                    ExtractAniMoveFbx(path);
                }
            }
        }
        
        AssetDatabase.Refresh();

        LoggerUtils.LogError("Extract AnimationClip Complete");
    }

    static void ExtractAniMoveFbx(string fbxPath)
    {
        AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);
        if (animationClip != null)
        {
            AnimationClip newClip = new AnimationClip();
            EditorUtility.CopySerialized(animationClip, newClip);
            string animName = animationClip.name;
            string fullPath = Path.Combine(Path.GetDirectoryName(fbxPath));
            string animPath = Path.Combine(fullPath, animName + ".anim");

            AssetDatabase.CreateAsset(newClip, animPath);

            var fbxFile = new FileInfo(fbxPath);
            Directory.CreateDirectory(ReplacePath(fbxFile.DirectoryName, OriginFileName, CopyToFileName));

            AssetDatabase.Refresh();

            string movePath = ReplacePath(fbxPath, OriginFileName, CopyToFileName);
            AssetDatabase.MoveAsset(fbxPath, movePath);
        }
    }

    [MenuItem("Assets/Animation/UpdateAnimationClip")]
    private static void Update()
    {
        var assetGUIDs = Selection.assetGUIDs;
        if (assetGUIDs.Length == 0) return;

        foreach (var guid in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.Contains(FBPXPath))
            {
                LoggerUtils.LogError("Choose the correct directory");

                continue;
            }

            if (Directory.Exists(path))
            {
                DirectoryInfo dInfo = new DirectoryInfo(path);

                FileInfo[] fbxFileInfo = dInfo.GetFiles("*" + FBXExtension, SearchOption.AllDirectories);
                foreach (var fbxFile in fbxFileInfo)
                {
                    UpdateAnimationClip(ToRelativePath(fbxFile.FullName));
                }
            }
            else
            {
                string fileExtension = Path.GetExtension(path);

                if (FBXExtension.Equals(Path.GetExtension(path)))
                {
                    UpdateAnimationClip(path);
                }
            }
        }

        AssetDatabase.Refresh();

        LoggerUtils.LogError("Update AnimationClip Complete");
    }

    static void UpdateAnimationClip(string fbxPath)
    {
        AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);

        if (animationClip != null)
        {
            AnimationClip newClip = new AnimationClip();
            EditorUtility.CopySerialized(animationClip, newClip);
            string animName = animationClip.name;
            string fullPath = Path.Combine(Path.GetDirectoryName(fbxPath));
            string animPath = Path.Combine(ReplacePath(fullPath, CopyToFileName, OriginFileName), animName + ".anim");

            AssetDatabase.DeleteAsset(animPath);
            AssetDatabase.CreateAsset(newClip, animPath);
        }
    }

    static string ToRelativePath(string path)
    {
        return path.Substring(path.IndexOf("Assets"));
    }

    static string ReplacePath(string path, string[] originStr, string[] newStr)
    {
        for (int i = 0; i < originStr.Length; i++)
        {
            path = path.Replace(originStr[i], newStr[i]);
        }

        return path;
    }
}
