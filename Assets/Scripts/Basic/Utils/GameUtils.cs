// @Author: YangJie
// @Description:
// @Date:  2023/07/17
// @Modify:

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Basic.Utils
{
    public class GameUtils
    {
        public static long GetTimeStamp()
        {
            var untilNow = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (long)untilNow.TotalSeconds;
        }

        public static string GetTimeDay()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static string GetTimeDayInDot()
        {
            return System.DateTime.Now.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 按字节数截取字符串的方法(比SubString好用)
        /// </summary>
        /// <param name="source">要截取的字符串（可空）</param>
        /// <param name="NumberOfBytes">要截取的字节数</param>
        /// <param name="encoding">System.Text.Encoding</param>
        /// <param name="suffix">结果字符串的后缀（超出部分显示为该后缀）</param>
        /// <returns></returns>
        public static string SubStringByBytes(string source, int NumberOfBytes, System.Text.Encoding encoding = default, string suffix = "...")
        {
            if (encoding == default)
            {
                encoding = Encoding.Unicode;
            }

            if (string.IsNullOrWhiteSpace(source) || source.Length == 0)
                return source;

            if (encoding.GetBytes(source).Length <= NumberOfBytes)
                return source;

            long tempLen = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var c in source)
            {
                Char[] _charArr = new Char[] { c };
                byte[] _charBytes = encoding.GetBytes(_charArr);
                if ((tempLen + _charBytes.Length) > NumberOfBytes)
                {
                    if (!string.IsNullOrWhiteSpace(suffix))
                        sb.Append(suffix);
                    break;
                }
                else
                {
                    tempLen += _charBytes.Length;
                    sb.Append(encoding.GetString(_charBytes));
                }
            }
            return sb.ToString();
        }

        public static bool IsNetUrl(string url)
        {
            if(string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("https://") || url.StartsWith("http://");
        }

        public static List<T> GetBehaviourInFirstLayer<T>(Transform node)
        {
            List<T> behaviours = new List<T>();
            for (int i = 0; i < node.childCount; i++)
            {
                T nBehaviour = node.GetChild(i).GetComponent<T>();
                if (nBehaviour != null)
                {
                    behaviours.Add(nBehaviour);
                }
            }
            return behaviours;
		}
        public static DateTime GetDataTimeStamp(long stamp)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
            return startTime.AddSeconds(stamp);
        }

        public static long GetUnixTimeStamp(DateTime dt)
        {
            try
            {
                DateTime dtStart = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1, 0, 0, 0), TimeZoneInfo.Local);
                long timeStamp = Convert.ToInt32((dt - dtStart).TotalSeconds);
                return timeStamp;
            }
            catch (OverflowException)
            {
                return 0; // 或者根据需要返回其他值
            }
        }

        #if PACKAGE_TYPE_US
        public static string ToBudCommonNumString(int num)
        {
            if (num < 1000)
            {
                return num.ToString();
            }
            else if (num >= 1000 && num <= 999999)
            {
                var format = new System.Globalization.NumberFormatInfo();
                format.NumberDecimalDigits = 1;
                format.NumberDecimalSeparator = ".";
                format.NumberGroupSeparator = ",";
                double result = Math.Floor((double)num / 1000.0 * 10) / 10;
                return result.ToString("N1", format) + "K";
            }
            else
            {
                var format = new System.Globalization.NumberFormatInfo();
                format.NumberDecimalDigits = 1;
                format.NumberDecimalSeparator = ".";
                format.NumberGroupSeparator = ",";
                double result = Math.Floor((double)num / 1000000.0 * 10) / 10;
                return result.ToString("N1", format) + "M";
            }
        }
        #else
        public static string ToBudCommonNumString(int num)
        {
            if (num < 1000)
            {
                return num.ToString();
            }
            else if (num >= 1000 && num <= 9999999)
            {
                var format = new System.Globalization.NumberFormatInfo();
                format.NumberDecimalDigits = 1;
                format.NumberDecimalSeparator = ".";
                format.NumberGroupSeparator = ",";
                double result = Math.Floor((double)num / 1000.0 * 10) / 10;
                return result.ToString("N1", format) + "千";
            }
            else
            {
                var format = new System.Globalization.NumberFormatInfo();
                format.NumberDecimalDigits = 1;
                format.NumberDecimalSeparator = ".";
                format.NumberGroupSeparator = ",";
                double result = Math.Floor((double)num / 10000.0 * 10) / 10;
                return result.ToString("N1", format) + "万";
            }
        }
        #endif


        public static bool IsCurrencyType(int rewardType) {

            int[] rewardTypes = new[] {
                (int)CurrencyType.PinkCoin,
                (int)BUDRewardType.RewardCoin,
                (int)BUDRewardType.RewardGem,
                (int)BUDRewardType.RewardBadge,
                (int)BUDRewardType.RewardPinkCoin,
                (int)CurrencyType.EnergyCoin,
                (int)CurrencyType.YouYouCoin,
                (int)CurrencyType.LuckyCoin,
                (int)CurrencyType.MagicCoin,
                (int)BUDRewardType.RewardMagicCoin,
                (int)BUDRewardType.RewardLuckyCoin,
                (int)BUDRewardType.RewardChristmasCoin,
                (int)BUDRewardType.RewardYouYouCoin,
                (int)BUDRewardType.RewardPurpleDreamCoin,
                (int)BUDRewardType.RewardGiftTicket,
                (int)BUDRewardType.RewardAISeasonCoin,
                (int)BUDRewardType.RewardCreatorCoin,
            };

            return rewardTypes.Contains(rewardType);
        }


        public static CurrencyType ConvertRewardType(int rewardType)
        {
            if (rewardType == (int)BUDRewardType.RewardCoin)
            {
                return CurrencyType.Coin;
            }else if (rewardType == (int)BUDRewardType.RewardLuckyTicket)
            {
                return CurrencyType.LuckyTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardBadge)
            {
                return CurrencyType.Badge;
            }
            else if (rewardType == (int)BUDRewardType.RewardGem)
            {
                return CurrencyType.Gem;
            }
            else if (rewardType == (int)BUDRewardType.RewardPinkCoin)
            {
                return CurrencyType.PinkCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardEnergyCoin)
            {
                return CurrencyType.EnergyCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardChristmasCoin)
            {
                return CurrencyType.ChristmasCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardMagicCoin)
            {
                return CurrencyType.MagicCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardLuckyCoin)
            {
                return CurrencyType.LuckyCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardChristmasTicket)
            {
                return CurrencyType.ChristmasTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardCoinTicket)
            {
                return CurrencyType.CoinTicket;
            } else if (rewardType == (int)BUDRewardType.RewardYouYouCoin) {
                return CurrencyType.YouYouCoin;
            } else if (rewardType == (int)BUDRewardType.RewardPurpleDreamCoin) {
                return CurrencyType.PurpleDreamCoin;
            } else if (rewardType == (int)BUDRewardType.RewardSeasonGachaCoin) {
                return CurrencyType.SeasonGachaCoin;
            }else if (rewardType == (int)BUDRewardType.RewardPopularityTicket)
            {
                return CurrencyType.PopularityTicket;
            } else if (rewardType == (int)BUDRewardType.RewardLuckyStar) {
                return CurrencyType.LuckyStar;
            }
            else if (rewardType == (int)BUDRewardType.RewardGiftTicket)
            {
                return CurrencyType.GiftTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardSweetieTicket)
            {
                return CurrencyType.SweetieTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardPurpleDreamTicket)
            {
                return CurrencyType.PurpleDreamTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardAICore)
            {
                return CurrencyType.AICore;
            }
            else if (rewardType == (int)BUDRewardType.RewardAISeasonCoin)
            {
                return CurrencyType.AISeasonCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardCreatorCoin)
            {
                return CurrencyType.GreenCoin;
            }
             else if (rewardType == (int)BUDRewardType.RewardCommunityInstrumentTicket)
            {
                return CurrencyType.CommunityInstrumentTicket;
            }
             else if (rewardType == (int)BUDRewardType.RewardCommunitySkinTicket)
            {
                return CurrencyType.CommunitySkinTicket;
            } 
             else if (rewardType == (int)BUDRewardType.RewardCommunityAnimationTicket)
            {
                return CurrencyType.CommunityAnimationTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardLuckyKoiTicket)
            {
                return CurrencyType.KoiGachaCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardCollectionTicket)
            {
                return CurrencyType.CollectionTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardSeasonPassCoin)
            {
                return CurrencyType.SeasonPassCoin;
            }
            else if(rewardType == (int)BUDRewardType.RewardTypeSockCoin)
            {
                return CurrencyType.SockCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardTypeSockTailTicket)
            {
                return CurrencyType.SockTailTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardTypeSockYunyunTicket)
            {
                return CurrencyType.SockYunyunTicket;
            }
            else if (rewardType == (int)BUDRewardType.RewardTypeMiaoCoin)
            {
                return CurrencyType.MiaoCoin;
            }
            else if(rewardType == (int)BUDRewardType.RewardCrystal)
            {
                return CurrencyType.Crystal;
            }
            else if(rewardType == (int)BUDRewardType.RewardCrystalShards)
            {
                return CurrencyType.CrystalShards;
            }
            else if(rewardType == (int)BUDRewardType.RewardMusicNoteCrystal)
            {
                return CurrencyType.MusicNoteCrystal;
            }
            else if(rewardType == (int)BUDRewardType.RewardMusicNoteCrystalShards)
            {
                return CurrencyType.MusicNoteCrystalShards;
            }
            else if (rewardType == (int)BUDRewardType.RewardTypeZZZCoin)
            {
                return CurrencyType.ZZZCoin;
            }
            else if (rewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystal)
            {
                return CurrencyType.ZZZPhantomCrystal;
            }
            else if (rewardType == (int)BUDRewardType.RewardTypeZZZPhantomCrystalShards)
            {
                return CurrencyType.ZZZPhantomCrystalShards;
            }

            return CurrencyType.None;
        }

        public static void CovertTimeLineTextFormat(Text txtComp, int curTime)
        {
            int totalSeconds = (int)curTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            txtComp.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }

        public static bool IsListEqual(List<string> list1, List<string> list2)
        {
            if (list1 == null || list2 == null)
                return false;

            // 如果长度不同，直接返回 false
            if (list1.Count != list2.Count)
                return false;

            // 转换为 HashSet 进行比较
            var set1 = new HashSet<string>(list1);
            var set2 = new HashSet<string>(list2);

            return set1.SetEquals(set2);
        }

        /// <summary>
        /// 判断字符串是否属于指定的枚举类型
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="value">要检查的字符串</param>
        /// <returns>如果字符串是合法的枚举值，则返回 true，否则返回 false</returns>
        public static bool IsValidEnumValue<TEnum>(string value) where TEnum : struct, Enum
        {
            // 尝试解析字符串为枚举值，忽略大小写
            return Enum.TryParse<TEnum>(value, true, out var result) && Enum.IsDefined(typeof(TEnum), result);
        }

        /// <summary>
        /// 判断指定的 int 值是否是枚举的有效值
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="value">要检查的 int 值</param>
        /// <returns>如果 int 值是合法的枚举值，则返回 true，否则返回 false</returns>
        public static bool IsValidEnumValue<TEnum>(int value) where TEnum : struct, Enum
        {
            return Enum.IsDefined(typeof(TEnum), value);
        }

        /// <summary>
        /// 尝试将字符串解析为指定的枚举类型
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="value">要解析的字符串</param>
        /// <param name="result">解析成功时返回的枚举值</param>
        /// <returns>如果解析成功，返回 true，否则返回 false</returns>
        public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, true, out var parsedValue) && Enum.IsDefined(typeof(TEnum), parsedValue))
            {
                result = parsedValue;
                return true;
            }

            result = default;
            return false;
        }

        public static T FindComponentByName<T>(GameObject parent, string name) where T : MonoBehaviour
        {
            return FindComponentByName<T>(parent.transform, name);
        }

        public static T FindComponentByName<T>(Transform parent, string name) where T : MonoBehaviour
        {
            Transform child = FindChildByName(parent, name);
            if(child != null)
            {
                return child.GetComponent<T>();
            }
            return null;
        }
        
        public static Transform FindChildByName(Transform parent,string name)
        {
            Transform child = parent.Find(name);
            if(child != null)
            {
                return child;
            }
            for(int i = 0;i<parent.childCount;i++)
            {
                child = FindChildByName(parent.GetChild(i),name);
                if(child !=null)
                {
                    break;
                }
            }
            return child;
        }
        
        // 按照某个长度，截取文本。多余的显示为 ...
        public static string SetText(string val, int textLimit = 20)
        {
            if (string.IsNullOrEmpty(val))
            {
                return val;
            }
            string text = val.Length > textLimit ? val.Substring(0, textLimit) + "..." : val;
            return text;
        }
        
        public static void WriteJsonToFile(string jsonData, string fileName = "")
        {
            // 确定文件路径，这里使用Application.persistentDataPath
            string filePath = Path.Combine(Application.streamingAssetsPath, fileName + "WriteJson_" + GetTimeStamp() + ".json");

            try
            {
                // 将JSON字符串写入文件
                File.WriteAllText(filePath, jsonData);
                Debug.Log("JSON文件写入成功：" + filePath);
            }
            catch (System.Exception e)
            {
                // 若写入过程中出现异常，打印错误信息
                Debug.LogError("写入JSON文件时出错：" + e.Message);
            }
        }

    }
}
