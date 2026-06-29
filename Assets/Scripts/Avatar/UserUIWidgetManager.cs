using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserUIWidgetManager : GlobalInstance<UserUIWidgetManager>
{

    public string _userWidgetAtlas = "Assets/Loadable/UI/UIWidgets/UserInfoViewAtlas/UserInfoView.spriteatlas";
    public string _userHeadAtlas = "Assets/Loadable/UI/UIWidgets/UserInfoHeadView/UserInfoHeadView.spriteatlas";
    public string _userBubbleAtlas = "Assets/Loadable/UI/UIWidgets/UserInfoBubbleView/UserInfoBubbleView.spriteatlas";
    public string _userTitleAtlas = "Assets/Loadable/UI/UIWidgets/Title/UserTitleView.spriteatlas";
    public string _userNicrkNameAtlas = "Assets/Loadable/UI/UIWidgets/Nickname/UserNickNameView.spriteatlas";

    private string _userbubblePath = "Assets/Loadable/UI/UIWidgets/CreatorBubbles";

    private List<HeadCycleData> _headCycleConfig = new List<HeadCycleData>();
    private ProfileThemeConfig _homeSkipConfig = new ProfileThemeConfig();
    private List<GameChatBubbleData> _gameChatBubbleConfig = new List<GameChatBubbleData>();
    private List<NicknameData> _nicknameConfig = new List<NicknameData>();
    private List<TitleData> _titleConfig = new List<TitleData>();
    private string _bubbleEffPath = "Assets/Loadable/UI/UIWidgets/UserInfoView/ChatBubblePrefabs/ChatBubbleEff_";
    public UserUIWidgetManager()
    {
        var refObj = new GameObject("UserUIWidgetManager");
        var textAsset1 = Loader.Load<TextAsset>("Assets/Loadable/UI/UIWidgets/UserInfoView/HeadCycleConfig.json", refObj);
        if (textAsset1 != null)
        {
            _headCycleConfig = JsonConvert.DeserializeObject<List<HeadCycleData>>(textAsset1.text);
        }
        
        var textAsset2 = Loader.Load<TextAsset>("Assets/Loadable/UI/UIWidgets/UserInfoView/ChatBubbleConfig.json", refObj);
        if (textAsset2 != null)
        {
            _gameChatBubbleConfig = JsonConvert.DeserializeObject<List<GameChatBubbleData>>(textAsset2.text);
        }

        var textAsset3 = Loader.Load<TextAsset>("Assets/Loadable/UI/UIPanel/ProfileTheme/ProfileThemeConfig.json", refObj);
        if (textAsset3 != null)
        {
            _homeSkipConfig = JsonConvert.DeserializeObject<ProfileThemeConfig>(textAsset3.text);
        }

        var textAsset4 = Loader.Load<TextAsset>("Assets/Loadable/UI/UIWidgets/Nickname/NicknameConfig.json", refObj);
        if (textAsset4 != null)
        {
            _nicknameConfig = JsonConvert.DeserializeObject<List<NicknameData>>(textAsset4.text);
        }

        var textAsset5 = Loader.Load<TextAsset>("Assets/Loadable/UI/UIWidgets/Title/TitleConfig.json", refObj);
        if (textAsset5 != null)
        {
            _titleConfig = JsonConvert.DeserializeObject<List<TitleData>>(textAsset5.text);
        }
    }

    #region 聊天气泡
    // 加载图片
    public Sprite LoadGameBubbleSprite(string path)
    {
        AssetWrapper<Sprite> wrapper = Loader.Load<Sprite>(path);
        if (wrapper != null && wrapper.request != null && wrapper.request.isDone && wrapper.request.result == xasset.Request.Result.Success)
        {
            Sprite sprite = wrapper.request.asset as Sprite;
            return(sprite);
        }
        else
        {
            LoggerUtils.LogError($"加载Sprite失败: {path}");
            return (null);
        }
    }
    public HallChatBubbleData GetHallChatBubbleData(int bubbleId, GameObject refObj)
    {
        HallChatBubbleData data = new HallChatBubbleData();
        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userBubbleAtlas, "HallChatBubble_" + bubbleId, refObj);
        var configData = _gameChatBubbleConfig.Find(x => x.Id == bubbleId);
        if (configData != null)
        {   
            data.Sp = sp;
            data.Offset = configData.LayoutOffset;
            data.CornerEffectPrefabPaths = configData.CornerEffectPrefabPaths;
        }
        return data;
    }

    public GameChatBubbleData GetChatDataByID(int bubbleId)
    {
        var configData = _gameChatBubbleConfig.Find(x => x.Id == bubbleId);
        return configData;
    }
    public GameChatBubbleData GetGameChatBubbleData(int bubbleId, GameObject refObj)
    {
        var sp = LoadGameBubbleSprite(_userbubblePath + "/" + "GameChatBubble_" + bubbleId + ".png");
        var configData = _gameChatBubbleConfig.Find(x => x.Id == bubbleId);
        if (configData != null)
        {
            configData.Sp = sp;
        }
        return configData;
    }
    public string GetGameChatBubbleEffPath(int bubbleId)
    {
        return _bubbleEffPath + bubbleId.ToString() + "/";
    }
    public Sprite GetChoosePanelBubbleBg(int bubbleId, GameObject refObj)
    {
        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userBubbleAtlas, "ShowBubble_" + bubbleId, refObj);
        if (sp == null)
        {
            sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userBubbleAtlas, "HallChatBubble_" + bubbleId, refObj);
        }
        if (bubbleId == 0)
        {
            sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userBubbleAtlas, "HallChatBubble_Def", refObj);
        }

        return sp;
    }

    public void GetChatBubbleIconByPgcIdAsync(string pgcId, GameObject refObj, Action<Sprite> onComplete)
    {
        var configData = _gameChatBubbleConfig.Find(x => x.PgcId == pgcId);
        if (configData != null)
        {
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(_userBubbleAtlas, configData.IconName, refObj, onComplete);
        }
    }
    
    public Sprite GetChatBubbleIconByType(ChatBubblesType type, GameObject refObj)
    {
        var configData = _gameChatBubbleConfig.Find(x => x.Id == (int)type);
        if (configData != null)
        {
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userBubbleAtlas, configData.IconName, refObj);
        }
        else
        {
            LoggerUtils.LogError(type + "Bub Not Find!");
        }

        return null;
    }

    public bool CheckIsOwnedBubblePgc(string pgcId)
    {
        var configData = _gameChatBubbleConfig.Find(x => x.PgcId == pgcId);
        if (configData != null)
        {
            var bubbleId = configData.Id;
            var bubbleData =
                AccountDataManager.Inst.UserInfo.ownedChatBubblesList.Find(x => x.bubbleId == bubbleId);
            if (bubbleData != null)
                return true;
        }

        return false;
    }

    #endregion

    #region 头像框

    public Sprite GetHeadCycleImg(string spName, GameObject refObj)
    {
        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userHeadAtlas, spName, refObj);
        return sp;
    }

    public int GetAvatarTypeIdByPgcId(string pgcId)
    {
        var data = _headCycleConfig.Find(x => x.PgcId == pgcId);
        if (data != null)
        {
            return data.Id;
        }
        return 0;
    }

    public int GetAvatarTypeIdById(int Id)
    {
        var data = _headCycleConfig.Find(x => x.Id == Id);
        if (data != null)
        {
            return data.Id;
        }
        return 0;
    }

    public int GetChatBubbleTypeIdByPgcId(string pgcId)
    {
        var data = _gameChatBubbleConfig.Find(x => x.PgcId == pgcId);
        if (data != null)
        {
            return data.Id;
        }
        return 0;
    }

    public void GetHeadCycleImgByPgcIdAsync(string pgcId, GameObject refObj, Action<Sprite> onComplete)
    {
        var data = _headCycleConfig.Find(x => x.PgcId == pgcId);
        if (data != null)
        {
            if (data.PreviewSpriteName != null)
            {
                XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(_userHeadAtlas, data.PreviewSpriteName, refObj, onComplete);
            }
            else
            {
                XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(_userHeadAtlas, data.SpriteName, refObj, onComplete);
            }
        }
        else
        {
            LoggerUtils.LogError(pgcId + "Cycle Not Find!");
        }
    }
    public void GetTitleImgByPgcIdAsync(string pgcId, GameObject refObj, Action<Sprite> onComplete)
    {
        var data = _titleConfig.Find(x => x.PgcId == pgcId);
        if (data != null)
        {
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(_userTitleAtlas, data.SpriteName, refObj, onComplete);
        }
        else
        {
            LoggerUtils.LogError(pgcId + "Cycle Not Find!");
        }
    }
    public TitleData GetTitleDataByPgcId(string pgcId)
    {
        return _titleConfig.Find(x => x.PgcId == pgcId);
    }
    public void GetNicknameBgByPgcIdAsync(string pgcId, GameObject refObj, Action<Sprite> onComplete)
    {
        var data = _nicknameConfig.Find(x => x.PgcId == pgcId);
        if (data != null)
        {
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(_userNicrkNameAtlas, data.SpriteName, refObj, onComplete);
        }
        else
        {
            LoggerUtils.LogError(pgcId + "Cycle Not Find!");
        }
    }
    public NicknameData GetNicknameByPgcId(string pgcId)
    {
        var data = _nicknameConfig.Find(x => x.PgcId == pgcId);
        return data;
    }

    public AssetWrapper<GameObject> GetHeadCycleEffect(string effectName, GameObject refObj)
    {
        var path = "Assets/Loadable/UI/UIWidgets/UserInfoView/HeadCyclePrefabs/";
        var effectWrapper = Loader.Load<GameObject>(path + effectName);
        return effectWrapper;
    }

    public HeadCycleData GetHeadCycleDataById(int frameId)
    {
        var data = _headCycleConfig.Find(x => x.Id == frameId);
        return data;
    }
    public HeadCycleData GetHeadCycleData(int frameId, GameObject refObj)
    {
        var data = _headCycleConfig.Find(x => x.Id == frameId);
        if (data == null)
        {
            data = _headCycleConfig[0];
        }
        if (!string.IsNullOrEmpty(data.SpriteName))
        {
            data.Sp_HeadCycle = GetHeadCycleImg(data.SpriteName, refObj);
        }
        
        if (!string.IsNullOrEmpty(data.PreviewSpriteName))
        {
            data.Sp_PreviewCycle = GetHeadCycleImg(data.PreviewSpriteName, refObj);
        }
        else
        {
            data.Sp_PreviewCycle = GetHeadCycleImg(data.SpriteName, refObj);
        }

        if (!string.IsNullOrEmpty(data.BottomEffectName))
        {
            data.Effect_Bottom = GetHeadCycleEffect(data.BottomEffectName, refObj);
        }

        if (!string.IsNullOrEmpty(data.TopEffectName))
        {
            data.Effect_Top = GetHeadCycleEffect(data.TopEffectName, refObj);
        }

        return data;
    }

    public bool CheckIsOwnedAvatarFramePgc(string pgcId)
    {
        var data = _headCycleConfig.Find(x => x.PgcId == pgcId);
        if (data != null)
        {
            var frameId = data.Id;
            var bubbleData = AccountDataManager.Inst.UserInfo.ownedAvatarFrameList.Find(x => x.frameId == frameId);
            if (bubbleData != null)
                return true;
        }
        return false;
    }
    public bool CheckIsOwnedHomeSkipPgc(string pgcId)
    {
        var data = _homeSkipConfig.configs.Find(x => x.themeId == 9);
        if (data != null)
        {
            var skinId = data.themeId;
            var bubbleData = AccountDataManager.Inst.UserInfo.ownedHomepageSkinList.Find(x => x.skinId == skinId);
            if (bubbleData != null)
                return true;
        }
        return false;
    }
    public Sprite GetHeadCycleSp(int cycleId, GameObject refObj)
    {
        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userHeadAtlas, "HeadCycle_" + cycleId + "_Pre", refObj);
        if (sp == null)
        {
            sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userHeadAtlas, "HeadCycle_" + cycleId, refObj);
        }
        if (cycleId == 0)
        {
            sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(_userHeadAtlas, "HeadCycle_0", refObj);
        }

        return sp;
    }
    #endregion

    public NicknameData GetNicknameData(int id) {
        foreach (var item in _nicknameConfig)
        {
            if (item.Id == id || item.PgcId == id.ToString())
            {
                return item;
            }
        }
        return null;
    }

    public Sprite GetNicknameBg(int id, GameObject refObj)
    {
        return Loader.Load<Sprite>(GetNicknameData(id).Icon, refObj);
    }

    public bool CheckIsOwnedNicknamePgc(string pgcId)
    {
        var configData = _nicknameConfig.Find(x => x.PgcId == pgcId);
        if (configData != null)
        {
            var bubbleId = configData.Id;
            var bubbleData =
                AccountDataManager.Inst.UserInfo.ownedNicknameFrameList.Find(x => x.frameId == bubbleId);
            if (bubbleData != null)
                return true;
        }

        return false;
    }

    public TitleData GetTitleData(int id)
    {
        foreach (var item in _titleConfig)
        {
            if (item.Id == id || item.PgcId == id.ToString())
            {
                return item;
            }
        }
        return null;
    }

    public Sprite GetTitleBg(int id, GameObject refObj)
    {
        return Loader.Load<Sprite>(GetTitleData(id).Icon, refObj);
    }

    public bool CheckIsOwnedTitlePgc(string pgcId)
    {
        var configData = _titleConfig.Find(x => x.PgcId == pgcId);
        if (configData != null)
        {
            var bubbleId = configData.Id;
            var bubbleData =
                AccountDataManager.Inst.UserInfo.ownedTitleList.Find(x => x.titleId == bubbleId);
            if (bubbleData != null)
                return true;
        }

        return false;
    }

}

public class ProfileColorInfo
{
    public string titleBgColor; //目前没有什么用，透明度为0了，被图盖住了
    public string profileCardColor; // 目前没有什么用，透明度为0了，被图盖住了,仅是profileCard的头部背景
    public string mainBgColor;//目前没有什么用
    public string cardColor;  //card的背景图颜色值
    public string cardSubColor;  //card的背景图颜色值，目前仅游戏里面使用，建议保持一致
    public string placeHolderColor;//目前没有什么用
    public string textColor;  //card里面的文字颜色
    public string textBorderColor;  //card里面的文字描边颜色 
    public string titleInfoColor;  //称号信息背景颜色 
    public string noTitleColor; //无称号时文字颜色
    public string noTitleBorderColor; //无称号时文字描边颜色
    public string gridColor;
    public string priceColor;
    public string bg_bottom;
    public string info_bg1;
    public string info_bg2;
    public string info_bg3;
    public string info_bg4;
    public string info_bg5;

}
public class ProfileThemeInfo
{
    public int themeId;
    public string themeName = "";
    public string previewTitle;
    public string title;
    public ProfileColorInfo colorInfo;
    public string previewUrl = "";
    public string localPath = "";
    public int hasEffect = 0;
}
public class ProfileThemeConfig
{
    public List<ProfileThemeInfo> configs;
}
public class HallChatBubbleData
{
    public Sprite Sp;
    public RectOffset Offset;
    // 新增字段，用于存储四个角的特效预制件路径
    // 约定顺序: Top-Left, Top-Right, Bottom-Left, Bottom-Right
    public List<string> CornerEffectPrefabPaths { get; set; }
}

public class GameChatBubbleData
{
    public int Id;
    public string Name;
    public RectOffset LayoutOffset;
    public string PgcId;
    public string IconName;
    public Sprite Sp;
    public Vec2 Boarder;
    public Vec2 MiniSize;
    public Color DefColor;
    public Color TxtColor;
    public string Desc;

    // 新增字段，用于存储四个角的特效预制件路径
    // 约定顺序: Top-Left, Top-Right, Bottom-Left, Bottom-Right
    public List<string> CornerEffectPrefabPaths{ get; set; }

    // 可选: 用于在 UserUIWidgetManager 中缓存加载的预制件或实例化的特效
    [JsonIgnore] // 这个字段不需要从JSON反序列化
    public List<AssetWrapper<GameObject>> CornerEffectPrefabs { get; set; }
}

public class NicknameData
{
    public int Id;
    public string PgcId;
    public string Name;
    public string NameColor;
    public string NameOutlineColor;
    public string SpriteName;
    public string Title;
    public string Desc;
    public string Icon;
    public string PreviewPath;
    public string Prefab;
}

public class TitleData
{
    public int Id;
    public string Name;
    public string PgcId;
    public Vec3 Size;
    public string SpriteName;
    public string Desc;
    public string Icon;
    public string PreviewPath;
    public string Prefab;
    public string GamePrefab;
}
public class HeadCycleData
{
    public int Id;
    public string Name;
    public string Desc;
    public string PgcId;
    public string SpriteName;
    public string PreviewSpriteName;
    public string BottomEffectName;
    public string TopEffectName;

    public Sprite Sp_PreviewCycle; // 预览图
    public Sprite Sp_HeadCycle; // 底框图
    public AssetWrapper<GameObject> Effect_Bottom;
    public AssetWrapper<GameObject> Effect_Top;
}


public enum ChatBubblesType {//气泡
    ChatBubblesDefault = 0, //默认的头像框
    ChatBubblesS6 = 1, // s6赛季累充气泡框-冰雪
    ChatBubblesVip = 2, // 未使用
    ChatBubbles2025NewYear = 3, //2025新年
    ChatBubbles2025NewYearLimitedPack = 4, //2025新年限定礼包-金蛇贺岁
    ChatBubblesAIBuddyIntimacy = 5, // ai buddy 亲密度任务 气泡
    ChatBubblesSweetheartParty = 6, // s7扭蛋 甜心舞会聊天气泡
    ChatBubblesS7 = 7, // S7 赛季累充聊天气泡
    ChatBubblesLove = 8, // 情人节礼包
    ChatBubblesS7LevelLightChaserCreator = 9, //s7 逐光创作者聊天气泡
    ChatBubblesS8 = 10, // S8 赛季累充聊天气泡
    ChatBubblesCoralSea = 11, // s9 赛季累充 珊瑚海之约
    ChatBubblesSangSang = 12, // 丧丧护士聊天气泡
    ChatBubblesS9DragonBoatLottery = 13, // s9 粽夏芙瑶聊天气泡
    ChatBubblesS1OMagicBoomBoomBoom = 14 ,//S10魔法砰砰砰头像框
    ChatBubblesScarletElegy = 15,//猩红挽歌聊天气泡
    ChatBubblesAnniversaryYear = 16,//周年庆聊天气泡
    ChatBubblesSweetheartBow = 17,//猩红挽歌聊天气泡
    ChatBubblesS12 = 18,//S12 赛季累充聊天气泡
    ChatBubblesMusicalBuding = 19,//音符布丁气泡
    ChatBubblesMaNian = 20,//马年气泡
    ChatJinSeAnXiangLing = 21,//堇色暗香令气泡
    ChatBubblesUmi = 22,//Umi气泡
    ChatBubblesDhzy = 23,//	华灯织页气泡
    ChatBubblesXdmg = 24,//	心动玫瑰气泡
    ChatBubblesQianqianWanwan = 25,// 千千万万聊天气泡
    ChatBubblesChasingWaves = 30,//逐浪沙沙聊天气泡
}

public enum TitleType
{
    TitleDefault = 0, //默认称号
    TitleQianqianWanwan = 1, // 千千万万称号
}