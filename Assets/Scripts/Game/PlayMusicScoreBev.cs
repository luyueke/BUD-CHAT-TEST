using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using Pb.Base;
using Pb.Game;
using UnityEngine;

public class PlayMusicScoreBev : MonoBehaviour
{
    public enum PlayType
    {
        Loop,
        Once,
    }

    public MusicScoreInfo DefaultMS = new MusicScoreInfo()
    {
        toneType = 1,
        bpm = 56,
        partList = new()
        {
            new MusicScorePartInfo()
            {
                syllableInfosList = new()
                {
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 9 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 10 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 11 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 12 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 13 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 14 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 15 }
                    },
                    new MusicScoreSyllableInfo()
                    {
                        syllableType = 0,
                        syllablesList = new() { 1 }
                    }
                }
            }
        }
    };

    private Dictionary<int,List<SyllablePlayData>> curInfoDic = new Dictionary<int, List<SyllablePlayData>>();
    private int curSyllableId;
    private float timeSpace = 0.54f;
    private Action<List<SyllablePlayData>> playSingleAciton;

    private PlayType playType = PlayType.Loop;

    public void ChangePlayType(PlayType type)
    {
        playType = type;
    }

    public void StartPLay(MusicScoreInfo msInfo, Action<List<SyllablePlayData>> playSingleData)
    {
        curInfoDic.Clear();
        curInfoDic = GetSyllablePlayDataList(msInfo);
        curSyllableId = 0;
        this.playSingleAciton = playSingleData;
        if (msInfo.bpm > 0)
        {
            timeSpace = (60 / (float)msInfo.bpm)/2;
        }
    }
    public void StopPLay()
    {
        curInfoDic.Clear();
        curSyllableId = 0;
        this.playSingleAciton = null;
    }
    private Dictionary<int,List<SyllablePlayData>> GetSyllablePlayDataList(MusicScoreInfo msInfo)
    {
        var musicScoreInfo = new Dictionary<int,List<SyllablePlayData>>();
        if (msInfo==null||msInfo.partList==null)
        {
            return musicScoreInfo;
        }

        int syIndex = 0;
        for (int i = 0; i < msInfo.partList.Count; i++)
        {
            int partIndex = i;
            if (msInfo.partList[i]!=null&&msInfo.partList[i].syllableInfosList!=null)
            {
                for (int j = 0; j < msInfo.partList[i].syllableInfosList.Count; j++)
                {
                    
                    var syllableInfo = msInfo.partList[i].syllableInfosList[j];
                    if (syllableInfo.syllableType != (int)MusicScoreSyllableType.Syllables||syllableInfo.syllablesList==null||syllableInfo.syllablesList.Count==0)
                    {
                        if (syllableInfo.syllableType == (int)MusicScoreSyllableType.Empty)
                        {
                            musicScoreInfo.Add(syIndex,new List<SyllablePlayData>()
                            {
                                new SyllablePlayData()
                                {
                                    SyllId = 0
                                }
                            });
                        }
                        else
                        {
                            musicScoreInfo.Add(syIndex,null);
                        }
                        syIndex++;
                    }
                    else if (syllableInfo.syllableType == (int)MusicScoreSyllableType.Syllables&&syllableInfo.syllablesList!=null&&syllableInfo.syllablesList.Count>0)
                    {
                        var musicScoreSyllables = new List<SyllablePlayData>();
                        
                        float length = GetLength(msInfo.partList,partIndex,j+1);
                        for (int k = 0; k < syllableInfo.syllablesList.Count; k++)
                        {
                            var sData = new SyllablePlayData();
                            sData.SyllId = syllableInfo.syllablesList[k];
                            sData.Length = length;
                            musicScoreSyllables.Add(sData);
                        }
                        musicScoreInfo.Add(syIndex,musicScoreSyllables);
                        syIndex++;
                    }
                }
            }
        }
        return musicScoreInfo;
    }

    //获取是否有拖音以及拖音长度
    private float GetLength(List<MusicScorePartInfo> info,int partIndex ,int index)
    {
        //先检测本章节有没有拖音
        float length = 0;
        if (info[partIndex].syllableInfosList.Count>index)
        {
            for (int i = index; i < info[partIndex].syllableInfosList.Count; i++)
            {
                if (info[partIndex].syllableInfosList[i].syllableType != (int)MusicScoreSyllableType.Long)
                {
                    return length;
                }
                else
                {
                    length += timeSpace;
                }
            }
        }
        //如果本章节拖音到结尾则看后面的章节开头有没有拖音
        if (info.Count<=partIndex+1)
        {
            return length;
        }
        for (int i = partIndex+1; i < info.Count; i++)
        {
            for (int j = 0; j < info[i].syllableInfosList.Count; j++)
            {
                if (info[i].syllableInfosList[j].syllableType != (int)MusicScoreSyllableType.Long)
                {
                    return length;
                }
                else
                {
                    length += timeSpace;
                }
            }
        }
        return length;
    }
    private float timeLine = 0;
    private void Update()
    {
        timeLine += Time.deltaTime;
        if (timeLine>=timeSpace)
        {
            timeLine -= timeSpace;
            PlaySyllable();
        }
        
    }

    private void PlaySyllable()
    {
        if (curInfoDic!=null&&curInfoDic.Count>0)
        {
            if (curInfoDic.ContainsKey(curSyllableId))
            {
                if (curInfoDic[curSyllableId]!=null)
                {
                    for (int i = 0; i < curInfoDic[curSyllableId].Count; i++)
                    {
                        playSingleAciton?.Invoke(curInfoDic[curSyllableId]);
                    }
                }
                curSyllableId++;
            }
            else
            {
                // 单次播放停止
                if (playType == PlayType.Once)
                {
                    StopPLay();
                    return;
                }
                curSyllableId = 0;
            }
           
        }
    }

    
}
