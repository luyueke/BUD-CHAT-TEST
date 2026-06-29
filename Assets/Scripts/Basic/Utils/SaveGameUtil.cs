using System.Collections.Generic;
using BayatGames.SaveGameFree;
using UnityEngine;
namespace Basic.Utils
{
    public class SaveGameUtil : InstMonoBehaviour<SaveGameUtil>
    {
        public static string CommonFolder = "SaveGame";
        
        public static string TestKey = CommonFolder + "/文件夹名/文件名.后缀";

        public static string gAccountKey = CommonFolder + "/user/account.json";

        public static string UIQuailityTipPopUp = "UIQuailityTipPopUp";
        
        public static string NewAnimeTag = "NewAnimeTag";//二次元风格Tag

        public static string GameEntry = "GameEntry";//新游戏大厅入口

        public static string GameParkRecord = "GameParkRecord";//乐园游玩存储

        public static string GameParkGuild = "GameParkGuild";//乐园引导存储  0 ，1，2，3 就是有引导步骤  -1 引导已结束

        public static string GameParkEntry = "GameParkEntry";//乐园入口引导存储

        public static string GameParkStart = "GameParkStart";//乐园入口开始游戏引导存储

        public static string GameParkRed = "GameParkRed";//乐园入口红点存储

        public static string TimeLimitGift = "TimeLimitGift";//限时礼包

        public static string TimeLimitGiftCooling = "TimeLimitGiftCooling";//限时礼包冷却时间

        public static string TimeLimitGiftBuy = "TimeLimitGiftBuy";//限时礼包动作礼包是否购买过

        public static string HallRemoteBtns = "HallRemoteBtns";

        public static string NewDayTime = "NewDayTime";

        //简单的示例代码
        public void Example()
        {
            //保存示例 也可用于保存自定义类
            Save<Dictionary<string, Vector3>>(TestKey, new Dictionary<string, Vector3>()
            {
                ["test1"] = new Vector3(1,2,3),
                ["test2"] = new Vector3(3,4,5)
            });
            
            //读取示例 若不存在返回 null
            var e = Load<Dictionary<string, Vector3>>(TestKey);
        }

        public void Init()
        {
#if UNITY_EDITOR
            //编辑器下默认保存到工程Assets下 方便调试
            SaveGame.SavePath = SaveGamePath.DataPath;
            //Example();
#else
            SaveGame.SavePath = SaveGamePath.PersistentDataPath;
#endif
            gameObject.DontDestroy();
        }

        public void Save<T>(string identifier, T obj)
        {
            SaveGame.Save(identifier, obj);
        }
        
        public void Save<T>(string identifier, T obj,bool isEncode)
        {
            SaveGame.Save(identifier, obj,isEncode);
        }
        
        public T Load<T>(string identifier, T defaultValue = default(T))
        {
            return SaveGame.Load<T>(identifier, defaultValue);
        }
        
        public T Load<T>(string identifier,bool isEncode,T defaultValue= default(T))
        {
            return SaveGame.Load<T>(identifier, defaultValue,isEncode);
        }
        
        public bool Exists(string identifier)
        {
            return SaveGame.Exists(identifier);
        }
        
        public void Delete(string identifier)
        {
            SaveGame.Delete(identifier);
        }

        public void SetIntByPlayerPrefs(string key,int value)
        {
            PlayerPrefs.SetInt(key,value);
            PlayerPrefs.Save();
        }

        public int GetIntByPlayerPrefs(string key)
        {
            if (HasKeyByPlayerPrefs(key))
            {
                return PlayerPrefs.GetInt(key);
            }

            return 0;
        }

        public void SetStrByPlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public string GetStrByPlayerPrefs(string key)
        {
            if (HasKeyByPlayerPrefs(key))
            {
                return PlayerPrefs.GetString(key);
            }

            return string.Empty;
        }

        public bool HasKeyByPlayerPrefs(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}