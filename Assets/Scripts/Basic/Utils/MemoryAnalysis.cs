using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using xasset;

public class MemoryAnalysis : MonoBehaviour
{
    public bool showDebug;
    public bool showMemoryAnalysis;
    private SceneRequest scene;
    private int selectIndex = 0;

    private float updateInterval = 1f;
    private float _lastUpdateTime;

    string statsText = string.Empty;

    string[] memorys =
    {
        "System Used Memory",
        "Total Used Memory",
        "Total Reserved Memory",
        "GC Used Memory",
        "GC Reserved Memory",
        "Audio Used Memory",
        "Audio Reserved Memory",
        "Video Used Memory",
        "Video Reserved Memory",
        "Profiler Used Memory",
        "Profiler Reserved Memory",
        "Texture Count",
        "Mesh Count",
        "Material Count",
        "AnimationClip Count",
        "GC Allocation In Frame Count",
        "Asset Count",
        "GameObject Count",
        "Scene Object Count",
        "Object Count",
        "Texture Memory",
        "Mesh Memory",
        "Material Memory",
        "AnimationClip Memory",
        "GC Allocated In Frame",
    };
    private ProfilerRecorder[] p;

    private long[] datas;

    
    private ProfilerRecorder callRecorder;
    private ProfilerRecorder trianglesRecorder;
    private ProfilerRecorder batchesRecorder;
    private ProfilerRecorder setPassRecorder;
    
    private float m_LastUpdateShowTime = 0f; //上一次更新帧率的时间;
    private int m_FrameUpdate = 0; //帧数;
    private float m_FPS = 0;
    private long setPass = 0;
    private long drawCall = 0;
    private long triangles = 0;
    private long batches = 0;
    private StringBuilder builder = new StringBuilder(); 
    void OnEnable()
    {
        p = new ProfilerRecorder[memorys.Length];
        datas = new long[memorys.Length];
        for (int i = 0, L = memorys.Length; i < L; i++)
        {
            p[i] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, memorys[i]);
            datas[i] = 0;
        }
        
        callRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
        batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
    }

    void OnDisable()
    {
        callRecorder.Dispose();
        setPassRecorder.Dispose();
        trianglesRecorder.Dispose();
        batchesRecorder.Dispose();
        
        if (p == null || p.Length == 0)
        {
            return;
        }

        for (int i = 0, L = memorys.Length; i < L; i++)
        {
            p[i].Dispose();
        }
    }
    

    void UpdateInfo()
    {
        var sb = new StringBuilder(500);
        if (p[1].Valid)
            sb.AppendLine($"Total Used Memory: {ByteConversionGBMBKB(p[1].LastValue - datas[1])}  Gc: {ByteConversionGBMBKB(p[3].LastValue - datas[3])}  Audio: {ByteConversionGBMBKB(p[5].LastValue - datas[5])}  Video: {ByteConversionGBMBKB(p[7].LastValue - datas[7])}  Profiler: {ByteConversionGBMBKB(p[9].LastValue - datas[9])}");
        if (p[2].Valid)
            sb.AppendLine($"Total Reserved Memory: {ByteConversionGBMBKB(p[2].LastValue - datas[2])}  Gc: {ByteConversionGBMBKB(p[4].LastValue - datas[4])}  Audio: {ByteConversionGBMBKB(p[6].LastValue - datas[6])}  Video: {ByteConversionGBMBKB(p[8].LastValue - datas[8])}  Profiler: {ByteConversionGBMBKB(p[10].LastValue - datas[10])}");
        if (p[0].Valid)
            sb.AppendLine($"System Used Memory: {ByteConversionGBMBKB(p[0].LastValue - datas[0])}");
        if (p[11].Valid && p[12].Valid && p[13].Valid && p[14].Valid && p[15].Valid)
            sb.AppendLine($"Texture: {p[11].LastValue - datas[11]}  Mesh: {p[12].LastValue - datas[12]}  Material: {p[13].LastValue - datas[13]}  AnimationCilp: {p[14].LastValue - datas[14]}  GC: {p[15].LastValue - datas[15]}");
        if (p[20].Valid && p[21].Valid && p[22].Valid && p[23].Valid && p[24].Valid)
            sb.AppendLine($"Texture: {ByteConversionGBMBKB(p[20].LastValue - datas[20])}  Mesh: {ByteConversionGBMBKB(p[21].LastValue - datas[21])}  Material: {ByteConversionGBMBKB(p[22].LastValue - datas[22])}  AnimationCilp: {ByteConversionGBMBKB(p[23].LastValue - datas[23])}  GC: {ByteConversionGBMBKB(p[24].LastValue - datas[24])}");
        if (p[16].Valid && p[18].Valid && p[19].Valid)
            sb.AppendLine($"Asset Count: {p[16].LastValue - datas[16]}  Scene Object Count: {p[18].LastValue - datas[18]}  Object Count: {p[19].LastValue - datas[19]}");
        if (p[17].Valid)
            sb.AppendLine($"GameObject Count: {p[17].LastValue - datas[17]}");
        statsText = sb.ToString();
    }

    private void LateUpdate()
    {
        if (!(Time.realtimeSinceStartup - _lastUpdateTime > updateInterval)) return;
        if (showMemoryAnalysis)
        {
            UpdateInfo();
        }
        _lastUpdateTime = Time.realtimeSinceStartup;
    }


    void Update()
    {
        m_FrameUpdate++;
        
        if (callRecorder.Valid && callRecorder.LastValue != 0)
        {
            drawCall = callRecorder.LastValue;
        }
        if (setPassRecorder.Valid && setPassRecorder.LastValue != 0)
        {
            setPass = setPassRecorder.LastValue;
        }
        if (trianglesRecorder.Valid && trianglesRecorder.LastValue != 0)
        {
            triangles = trianglesRecorder.LastValue;
        }
        if (batchesRecorder.Valid && batchesRecorder.LastValue != 0)
        {
            batches = batchesRecorder.LastValue;
        }
        
        if (m_FrameUpdate >= 60)
        {
            m_FPS = m_FrameUpdate / (Time.realtimeSinceStartup - m_LastUpdateShowTime);
            m_FrameUpdate = 0;
            m_LastUpdateShowTime = Time.realtimeSinceStartup;
        } 
    }
    
    
    const int GB = 1024 * 1024 * 1024;
    const int MB = 1024 * 1024;
    const int KB = 1024;
    public string ByteConversionGBMBKB(Int64 KSize)
    {
        if (Mathf.Abs(KSize / MB) >= 512)
            return (Math.Round(KSize / (float)GB, 2)).ToString() + "GB";
        else if (Mathf.Abs(KSize / KB) >= 512)
            return (Math.Round(KSize / (float)MB, 2)).ToString() + "MB";
        else
            return (Math.Round(KSize / (float)KB, 2)).ToString() + "KB";
    }
    public GUIStyle style;
    private Texture2D tex2D;


    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.textArea);
        }

        var scale = 2;
        GUI.skin.button.fontSize = (int)(18 * scale);
        GUI.skin.textArea.fontSize = (int)(16 * scale);
        GUI.skin.label.fontSize = (int)(18 * scale);

        if (showMemoryAnalysis)
        {
            if (tex2D == null)
            {
                tex2D = new Texture2D(256, 256);
            }

            style.normal.textColor = Color.red;
            style.fontSize = (int) (18 * scale);
            style.normal.background = tex2D;

            builder.Clear();
            builder.Append("FPS:").Append(Mathf.Floor(m_FPS).ToString("f2"))
                .Append("  DrawCall:").Append(drawCall)
                .Append("  setPass:").Append(setPass)
                .Append("  Batches:").Append(batches)
                .Append("  Triangles:").Append(triangles);
            GUI.Label(new Rect(100 * scale, 100 * scale, 716 * scale, 200 * scale), statsText + builder, style);
        }

        if (!showDebug)
        {
            if (GUI.Button(new Rect(300, 0, 100 * scale, 30 * scale), "内存分析"))
            {
                showMemoryAnalysis = !showMemoryAnalysis;
            }
        }
        else
        {
            if (GUI.Button(new Rect(900 * scale, 610 * scale, 200 * scale, 60 * scale), "关闭"))
            {
                showDebug = false;
            }
        }
    }
}

