using System;

#if GRAVITY_BYTEDANCE_GAME_MODE
using StarkSDKSpace;
#elif GRAVITY_BYTEDANCE_TT_GAME_MODE
using TTSDK;
#elif GRAVITY_OPPO_GAME_MODE
using QGMiniGame;
#elif GRAVITY_HUAWEI_GAME_MODE
using HWWASM;
#elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
using KSWASM;
#endif

namespace GravitySDK.PC.Storage
{
    public class GravitySDKFile
    {
        private static string GetKey(string key)
        {
            return "gravity_" + key;
        }
        
        public static void SaveData(string key, object value)
        {
            if (value == null)
            {
                return;
            }
            key = GetKey(key);// modified by gravity
            if (!string.IsNullOrEmpty(key))
            {
#if GRAVITY_WECHAT_GAME_MODE || GRAVITY_BILIBILI_GAME_MODE || GRAVITY_MEITUAN_GAME_MODE
                if (value.GetType() == typeof(int))
                {
                    PlayerPrefs.SetInt(key, (int)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    PlayerPrefs.SetFloat(key, (float)value);
                }
                else if (value.GetType() == typeof(string))
                {
                    PlayerPrefs.SetString(key, (string)value);
                }
                PlayerPrefs.Save();
#elif GRAVITY_BYTEDANCE_GAME_MODE
                if (value.GetType() == typeof(int))
                {
                    StarkSDK.API.PlayerPrefs.SetInt(key, (int) value);
                }
                else if (value.GetType() == typeof(float))
                {
                    StarkSDK.API.PlayerPrefs.SetFloat(key, (float) value);
                }
                else if (value.GetType() == typeof(string))
                {
                    StarkSDK.API.PlayerPrefs.SetString(key, (string) value);
                }

                StarkSDK.API.PlayerPrefs.Save();
#elif GRAVITY_BYTEDANCE_TT_GAME_MODE
                if (value.GetType() == typeof(int))
                {
                    TT.PlayerPrefs.SetInt(key, (int) value);
                }
                else if (value.GetType() == typeof(float))
                {
                    TT.PlayerPrefs.SetFloat(key, (float) value);
                }
                else if (value.GetType() == typeof(string))
                {
                    TT.PlayerPrefs.SetString(key, (string) value);
                }

                TT.PlayerPrefs.Save();
#elif GRAVITY_OPPO_GAME_MODE
                QG.StorageSetItem(key, (string)value);
#elif GRAVITY_HUAWEI_GAME_MODE
                QG.LocalStorage.SetItem(key, value.ToString());
#elif GRAVITY_XIAOMI_GAME_MODE
                if (value.GetType() == typeof(int))
                {
                    MiBridge.Instance.SetKVInt(key, (int)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    MiBridge.Instance.SetKVFloat(key, (float)value);
                }
                else if (value.GetType() == typeof(string))
                {
                    MiBridge.Instance.SetKVString(key, (string)value);
                }
#elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
                if (value.GetType() == typeof(int))
                {
                    KS.StorageSetIntSync(key, (int)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    KS.StorageSetFloatSync(key, (float)value);
                }
                else if (value.GetType() == typeof(string))
                {
                    KS.StorageSetStringSync(key, (string)value);
                }
#else
                if (value.GetType() == typeof(int))
                {
                    UnityEngine.PlayerPrefs.SetInt(key, (int)value);
                }
                else if (value.GetType() == typeof(float))
                {
                    UnityEngine.PlayerPrefs.SetFloat(key, (float)value);
                }
                else if (value.GetType() == typeof(string))
                {
                    UnityEngine.PlayerPrefs.SetString(key, (string)value);
                }
                UnityEngine.PlayerPrefs.Save();
#endif
            }
        }

        public static object GetData(string key, Type type)
        {
            key = GetKey(key);// modified by gravity
#if GRAVITY_WECHAT_GAME_MODE || GRAVITY_BILIBILI_GAME_MODE || GRAVITY_MEITUAN_GAME_MODE
            if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key))
            {
                if (type == typeof(int))
                {
                    return PlayerPrefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    return PlayerPrefs.GetFloat(key);
                }
                else if (type == typeof(string))
                {
                    return PlayerPrefs.GetString(key);
                }
                PlayerPrefs.Save();
            }
#elif GRAVITY_BYTEDANCE_GAME_MODE
            if (!string.IsNullOrEmpty(key) && StarkSDK.API.PlayerPrefs.HasKey(key))
            {
                if (type == typeof(int))
                {
                    return StarkSDK.API.PlayerPrefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    return StarkSDK.API.PlayerPrefs.GetFloat(key);
                }
                else if (type == typeof(string))
                {
                    return StarkSDK.API.PlayerPrefs.GetString(key);
                }

                StarkSDK.API.PlayerPrefs.Save();
            }
#elif GRAVITY_BYTEDANCE_TT_GAME_MODE
            if (!string.IsNullOrEmpty(key) && TT.PlayerPrefs.HasKey(key))
            {
                if (type == typeof(int))
                {
                    return TT.PlayerPrefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    return TT.PlayerPrefs.GetFloat(key);
                }
                else if (type == typeof(string))
                {
                    return TT.PlayerPrefs.GetString(key);
                }

                TT.PlayerPrefs.Save();
            }
#elif GRAVITY_OPPO_GAME_MODE
            if (!string.IsNullOrEmpty(key))
            {
                var v = QG.StorageGetItem(key);
                if (type == typeof(string))
                {
                    return v;
                }

                if (string.IsNullOrEmpty(v))
                {
                    return null;
                }

                if (type == typeof(int))
                {
                    return int.Parse(v);
                }
                
                if (type == typeof(double))
                {
                    return double.Parse(v);
                }
                
                if (type == typeof(float))
                {
                    return float.Parse(v);
                }
                
                if (type == typeof(bool))
                {
                    return bool.Parse(v);
                }
                return v;
            }
#elif GRAVITY_HUAWEI_GAME_MODE
            if (!string.IsNullOrEmpty(key))
            {
                return QG.LocalStorage.GetItem(key);
            }
#elif GRAVITY_XIAOMI_GAME_MODE
            if (!string.IsNullOrEmpty(key) && MiBridge.Instance.HasKV(key))
            {
                if (type == typeof(int))
                {
                    return MiBridge.Instance.GetKVInt(key, 0);
                }
                else if (type == typeof(float))
                {
                    return MiBridge.Instance.GetKVFloat(key, 0);
                }
                else if (type == typeof(string))
                {
                    return MiBridge.Instance.GetKVString(key, "");
                }
            }
#elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
            if (!string.IsNullOrEmpty(key) && KS.StorageHasKeySync(key))
            {
                if (type == typeof(int))
                {
                    return KS.StorageGetIntSync(key, 0);
                }
                else if (type == typeof(float))
                {
                    return KS.StorageGetFloatSync(key, 0);
                }
                else if (type == typeof(string))
                {
                    return KS.StorageGetStringSync(key, "");
                }
            }
#else
            if (!string.IsNullOrEmpty(key) && UnityEngine.PlayerPrefs.HasKey(key))
            {
                if (type == typeof(int))
                {
                    return UnityEngine.PlayerPrefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    return UnityEngine.PlayerPrefs.GetFloat(key);
                }
                else if (type == typeof(string))
                {
                    return UnityEngine.PlayerPrefs.GetString(key);
                }
                UnityEngine.PlayerPrefs.Save();
            }
#endif
            return null;
        }

        public static void DeleteData(string key)
        {
            key = GetKey(key);// modified by gravity
            if (!string.IsNullOrEmpty(key))
            {
#if GRAVITY_WECHAT_GAME_MODE || GRAVITY_BILIBILI_GAME_MODE || GRAVITY_MEITUAN_GAME_MODE
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
#elif GRAVITY_BYTEDANCE_GAME_MODE
                if (StarkSDK.API.PlayerPrefs.HasKey(key))
                {
                    StarkSDK.API.PlayerPrefs.DeleteKey(key);
                }
#elif GRAVITY_BYTEDANCE_TT_GAME_MODE
                if (TT.PlayerPrefs.HasKey(key))
                {
                    TT.PlayerPrefs.DeleteKey(key);
                }
#elif GRAVITY_OPPO_GAME_MODE
                QG.StorageRemoveItem(key);
#elif GRAVITY_HUAWEI_GAME_MODE
                QG.LocalStorage.RemoveItem(key);
#elif GRAVITY_XIAOMI_GAME_MODE
                MiBridge.Instance.DeleteKV(key);
#elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
                if (KS.StorageHasKeySync(key))
                {
                    KS.StorageDeleteKeySync(key);
                }
#else
                if (UnityEngine.PlayerPrefs.HasKey(key))
                {
                    UnityEngine.PlayerPrefs.DeleteKey(key);
                }
#endif
            }
        }
        
        public static bool HasKey(string key)
        {
            key = GetKey(key);// modified by gravity
#if GRAVITY_WECHAT_GAME_MODE || GRAVITY_BILIBILI_GAME_MODE || GRAVITY_MEITUAN_GAME_MODE
            return PlayerPrefs.HasKey(key);
#elif GRAVITY_BYTEDANCE_GAME_MODE
            return StarkSDK.API.PlayerPrefs.HasKey(key);
#elif GRAVITY_BYTEDANCE_TT_GAME_MODE
            return TT.PlayerPrefs.HasKey(key);
#elif GRAVITY_OPPO_GAME_MODE
            return true;
#elif GRAVITY_HUAWEI_GAME_MODE
            return true;
#elif GRAVITY_XIAOMI_GAME_MODE
            return MiBridge.Instance.HasKV(key);
#elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
            return KS.StorageHasKeySync(key);
#else
            return UnityEngine.PlayerPrefs.HasKey(key);
#endif
        }
    }
}