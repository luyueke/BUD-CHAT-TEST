using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UnityEngine;

public class ToneInfoEditUtill : MonoBehaviour
{
    private ToneInfo _curCreateToneInfo = new ToneInfo();
    public Dictionary<int, SyllableData> Temp_Syllable_Dict = new Dictionary<int, SyllableData>();
    
    public ToneInfoEditUtill()
    {
        ResetTempDict();
    }
    
    private void ResetTempDict()
    {
        Temp_Syllable_Dict.Clear();
        Temp_Syllable_Dict[(int)SyllableType.High_1] = null;
        Temp_Syllable_Dict[(int)SyllableType.High_2] = null;
        Temp_Syllable_Dict[(int)SyllableType.High_3] = null;
        Temp_Syllable_Dict[(int)SyllableType.High_4] = null;
        Temp_Syllable_Dict[(int)SyllableType.High_5] = null;
        Temp_Syllable_Dict[(int)SyllableType.High_6] = null;
        Temp_Syllable_Dict[(int)SyllableType.High_7] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_1] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_2] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_3] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_4] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_5] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_6] = null;
        Temp_Syllable_Dict[(int)SyllableType.Middle_7] = null;
    }
    
    public void CreateNewInfo()
    {
        ResetTempDict();
        _curCreateToneInfo = new ToneInfo();
        _curCreateToneInfo.Init();
    }

    public void SetToneName(string toneName)
    {
        _curCreateToneInfo.name = toneName;
    }

    public void SetToneId(string toneId)
    {
        _curCreateToneInfo.id = toneId;
    }

    public void SetToneType(ToneType toneType)
    {
        _curCreateToneInfo.SetToneType(toneType);
        RestoreFromeCache();
    }
    
    public void SetUgcSyllable(int id, string url)
    {
        SyllableData data = new SyllableData();
        data.url = url;
        _curCreateToneInfo.toneDict[id] = data;
        SaveSyllableToCache(id, data);
    }
    
    private void SaveSyllableToCache(int id, SyllableData data)
    {
        //记录到缓存当中
        Temp_Syllable_Dict[id] = data;
    }

    private void RestoreFromeCache()
    {
        foreach (var config in Temp_Syllable_Dict)
        {
            _curCreateToneInfo.toneDict[config.Key] = config.Value;
        }
    }

    public ToneInfo GetCurToneInfo()
    {
        return _curCreateToneInfo;
    }
    
    public bool CheckToneInfoIsLegal()
    {
        if (string.IsNullOrEmpty(_curCreateToneInfo.name))
            return false;

        foreach (var value in _curCreateToneInfo.toneDict.Values)
        {
            if (value == null)
                return false;
        }

        return true;
    }
}
