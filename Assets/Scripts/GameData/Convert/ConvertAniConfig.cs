using Es;
using System.Collections.Generic;
using UnityEngine;

public static class ConvertAniConfig
{
    public static PlayerAniConfig ConvertToAniConfig(this EmoAniConfig emoAniConfig)
    {
        var playerAniConfig = new PlayerAniConfig();
        playerAniConfig.aniId = emoAniConfig.emoId;
        playerAniConfig.aniType = emoAniConfig.aniType;
        playerAniConfig.bodyPath = emoAniConfig.bodyPath;
        playerAniConfig.facePath = emoAniConfig.facePath;
        playerAniConfig.faceDelayTime = emoAniConfig.faceDelayTime;
        playerAniConfig.faceEndTime = emoAniConfig.faceEndTime;
        playerAniConfig.effectPath = emoAniConfig.effectPath;
        playerAniConfig.randomCount = emoAniConfig.randomCount;
        playerAniConfig.randomTexPath = emoAniConfig.randomTexPath;
        playerAniConfig.bandId = emoAniConfig.bandId;
        playerAniConfig.bandNode = emoAniConfig.bandNode;
        playerAniConfig.r = emoAniConfig.r;
        playerAniConfig.s = emoAniConfig.s;
        playerAniConfig.p = emoAniConfig.p;
        playerAniConfig.effectAnimName = emoAniConfig.effectAnimName;
        playerAniConfig.interactPos = emoAniConfig.interactPos;
        playerAniConfig.interactRot = emoAniConfig.interactRot;
        playerAniConfig.soundName = emoAniConfig.soundName;
        playerAniConfig.soundVersion = emoAniConfig.soundVersion;

        return playerAniConfig;
    }

    public static List<PlayerAniConfig> ConvertToAniConfig(this List<EmoAniConfig> emoAniConfigList)
    {
        var playerAniConfigList = new List<PlayerAniConfig>(emoAniConfigList.Count);

        for (int i = 0; i < emoAniConfigList.Count; i++)
        {
            playerAniConfigList.Add(ConvertToAniConfig(emoAniConfigList[i]));
        }

        return playerAniConfigList;
    }

    public static PlayerAniConfig ConvertToAniConfig(this FeatAniConfig featAniConfig)
    {
        var playerAniConfig = new PlayerAniConfig();
        playerAniConfig.aniId = featAniConfig.id + "";
        playerAniConfig.aniType = featAniConfig.aniType;
        playerAniConfig.bodyPath = featAniConfig.bodyPath;
        playerAniConfig.facePath = featAniConfig.facePath;
        playerAniConfig.faceDelayTime = featAniConfig.faceDelayTime;
        playerAniConfig.faceEndTime = featAniConfig.faceEndTime;
        playerAniConfig.effectPath = featAniConfig.effectPath;
        playerAniConfig.randomCount = featAniConfig.randomCount;
        playerAniConfig.bandId = featAniConfig.bandId;
        playerAniConfig.bandNode = featAniConfig.bandNode;
        playerAniConfig.r = featAniConfig.r;
        playerAniConfig.s = featAniConfig.s;
        playerAniConfig.p = featAniConfig.p;
        playerAniConfig.effectAnimName = featAniConfig.effectAnimName;
        playerAniConfig.soundName = featAniConfig.soundName;
        return playerAniConfig;
    }
    
    public static List<PlayerAniConfig> ConvertToAniConfig(this List<FeatAniConfig> featAniConfigList)
    {
        var playerAniConfigList = new List<PlayerAniConfig>(featAniConfigList.Count);

        for (int i = 0; i < featAniConfigList.Count; i++)
        {
            playerAniConfigList.Add(ConvertToAniConfig(featAniConfigList[i]));
        }

        return playerAniConfigList;
    }
    
    public static PlayerAniConfig ConvertToAniConfig(this InstrumentAniConfig instrumentAniConfig)
    {
        var playerAniConfig = new PlayerAniConfig();
        playerAniConfig.aniId = instrumentAniConfig.emoId;
        playerAniConfig.aniType = instrumentAniConfig.aniType;
        playerAniConfig.bodyPath = instrumentAniConfig.bodyPath;
        playerAniConfig.facePath = instrumentAniConfig.facePath;
        playerAniConfig.faceDelayTime = instrumentAniConfig.faceDelayTime;
        playerAniConfig.faceEndTime = instrumentAniConfig.faceEndTime;
        playerAniConfig.effectPath = instrumentAniConfig.effectPath;
        playerAniConfig.randomCount = instrumentAniConfig.randomCount;
        playerAniConfig.bandId = instrumentAniConfig.bandId;
        playerAniConfig.bandNode = instrumentAniConfig.bandNode;
        playerAniConfig.r = instrumentAniConfig.r;
        playerAniConfig.s = instrumentAniConfig.s;
        playerAniConfig.p = instrumentAniConfig.p;
        playerAniConfig.effectAnimName = instrumentAniConfig.effectAnimName;
        playerAniConfig.soundName = instrumentAniConfig.soundName;

        return playerAniConfig;
    }
    
    public static List<PlayerAniConfig> ConvertToAniConfig(this List<InstrumentAniConfig> emoAniConfigList)
    {
        var playerAniConfigList = new List<PlayerAniConfig>(emoAniConfigList.Count);

        for (int i = 0; i < emoAniConfigList.Count; i++)
        {
            playerAniConfigList.Add(ConvertToAniConfig(emoAniConfigList[i]));
        }

        return playerAniConfigList;
    }
}

public class PlayerAniConfig
{
    public string aniId;
    public string aniType;
    public string bodyPath;
    public string facePath;
    public float faceDelayTime;
    public float faceEndTime;
    public List<string> effectPath;
    public int randomCount;
    public string randomTexPath;
    public List<int> bandId;
    public List<int> bandNode;
    public List<Vector3> r;
    public List<Vector3> s;
    public List<Vector3> p;
    public string effectAnimName;
    public Vector3 interactPos;
    public Vector3 interactRot;
    public string soundName;
    public string soundVersion;
}

public enum PlayAniType
{
    SingleOneTime = 1,
    SingleLoopStart = 2,
    SingleLooping = 3,
    SingleLoopEnd = 4,
    DoubleStart = 5,
    DoubleLoop = 6,
    DoubleEnd = 7,
    DoublePlayerA = 8,
    DoublePlayerB = 9,
    DoubleLoopPlayerAStart = 10,
    DoubleLoopPlayerBStart = 11,
    DoubleLoopPlayerALoop = 12,
    DoubleLoopPlayerBLoop = 13,
    DoubleLoopPlayerAEnd = 14,
    DoubleLoopPlayerBEnd = 15,
    PetSingleOneTime = 16,
    PetSingleLoopStart = 17,
    PetSingleLooping = 18,
    PetSingleLoopEnd = 19,
    PetWithPlayerPetA = 20,
    PetWithPlayerPlayerA = 21,
    PetWithPlayerLoopPetAStart = 22,
    PetWithPlayerLoopPlayerAStart = 23,
    PetWithPlayerLoopPetALoop = 24,
    PetWithPlayerLoopPlayerALoop = 25,
    PetWithPlayerLoopPetAEnd = 26,
    PetWithPlayerLoopPlayerAEnd = 27,
    LinkIdleA = 28,
    LinkIdleB = 29,
    LinkRunA = 30,
    LinkRunB = 31,
    LinkRunFastA = 32,
    LinkRunFastB = 33,
    LinkJumpA = 34,
    LinkJumpB = 35,
    LinkLandA = 36,
    LinkLandB = 37,
    PreviewJumpA = 38,
    PreviewJumpB = 39,

}