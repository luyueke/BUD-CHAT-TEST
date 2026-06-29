using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class TextureQualityData : ScriptableObject
{
    public List<QualitySetting> qualitySettings = new List<QualitySetting>();
}

[Serializable]
public class QualitySetting
{
    public string path;
    public TextureSize highSize;
    public TextureSize lowSize;
    public List<Object> specialSettings { get; set; } = new List<Object>();
}

public enum TextureSize
{
    _None = 0,
    _32 = 32,
    _64 = 64,
    _128 = 128,
    _256 = 256,
    _512 = 512,
    _1024 = 1024,
    _2048 = 2048,
    _4096 = 4096,
    _8192 = 8192,
    _16384 = 16384
}