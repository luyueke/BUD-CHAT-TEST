using UnityEditor;
using UnityEngine;

/// <summary>
/// Author: shaocheng
/// Description: 控制运行时游戏速度
/// Date: 2022-9-13 21:16:26
/// </summary>
public class BudRunningTimeControl : EditorWindow
{
    
    [MenuItem("BudTools/游戏速度/2倍速")]
    public static void BudRunningTimeControlDouble()
    {
        Time.timeScale = 2f;
    }

    [MenuItem("BudTools/游戏速度/正常1倍速")]
    public static void BudRunningTimeControlNormal()
    {
        Time.timeScale = 1f;
    }
    
    [MenuItem("BudTools/游戏速度/0.5倍速")]
    public static void BudRunningTimeControlHalf()
    {
        Time.timeScale = 0.5f;
    }
    
    [MenuItem("BudTools/游戏速度/0.25倍速")]
    public static void BudRunningTimeControlQuart()
    {
        Time.timeScale = 0.25f;
    }
}