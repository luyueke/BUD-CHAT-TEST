using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Game.Config;
using GameData.GameSync;
using GameSync.Manager;
using Pb.Base;
using Pb.Game;
/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-08 21:28:10
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-10-09 18:29:54
 * @ Description:
 */

using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.UIPanels
{
    public class GameChatOSAEntry : MonoBehaviour
    {
        [SerializeField]private GameChatOSAAdapter osAdpater;

        private Dictionary<string, PlayerInfo> uidNames = new Dictionary<string, PlayerInfo>();
        
        private Dictionary<string, string> userColorDic = new Dictionary<string, string>();
        private readonly string[] UserNameColor = new string[]
        {
            "<c=B5ABFF>", "<c=FF8989>", "<c=00ADFF>", "<c=02E880>"
        };
        


        /// <summary>
        /// 带名称的消息
        /// </summary>
        public void AddPlayerMessage(string msg, string playerId, ChatType chatType)
        {
            var pInfo = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoById(playerId);

            if (pInfo != null)
            {
                uidNames.TryAdd(playerId, pInfo);
            }
            else
            {
                uidNames.TryGetValue(playerId, out pInfo);
            }

            if (pInfo == null) {
                LoggerUtils.Log(playerId,"playerId 不在房间内");
                return;
            }
            
            var chatContent = ExtractUserNameFromString(msg, out string atUserName);
            if (!string.IsNullOrEmpty(atUserName))
            {
                atUserName = "<c=E477FF>" +  atUserName  + "</c>";
                chatContent = atUserName + chatContent;
            }
            
            string content = $"{SetNameColor(pInfo.Name, playerId)}{chatContent}";
            
            ChatUIData chatUIData = new ChatUIData()
            {
                Text = content,
                PlayerName = pInfo.Name,
                ChatType = chatType,
            };

            osAdpater.Data.InsertOneAtEnd(chatUIData);
            osAdpater.MoveToEnd();
        }
        
        public static string ExtractUserNameFromString(string input, out string atUserName)
        {
            // 定义正则表达式，匹配@后面的用户名（直到遇到空格）
            string pattern = @"@\S+";

            // 使用正则表达式查找匹配的字符串
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                // 获取用户名
                atUserName = match.Value;

                // 从原字符串中移除用户名
                string result = Regex.Replace(input, pattern, "").Trim();

                return result;
            }

            // 如果没有找到匹配，返回原始字符串
            atUserName = string.Empty;
            return input;
        }
        
        public void AddBuddyMessage(string senderId, ChatNetData chatNetData)
        {
            var msg = chatNetData.Content;
            var buddyName = chatNetData.BuddyName;
            
            var chatContent = ExtractUserNameFromString(msg, out string atUserName);
            if (!string.IsNullOrEmpty(atUserName))
            {
                atUserName = "<c=E477FF>" +  atUserName  + "</c>";
                chatContent = atUserName + chatContent;
            }
            
            string content = $"{SetNameColor(chatNetData.BuddyName, GameConsts.AIBuddyTag + senderId)}{chatContent}";
            
            ChatUIData chatUIData = new ChatUIData()
            {
                Text = content,
                PlayerName = buddyName,
                ChatType = ChatType.Chat,
            };

            osAdpater.Data.InsertOneAtEnd(chatUIData);
            osAdpater.MoveToEnd();
        }

        /// <summary>
        /// AI 伙伴口令互动的 Emote 分类消息，格式：[伙伴名]: 动作名
        /// </summary>
        public void AddBuddyEmoteMessage(string senderId, string buddyName, string emoteName)
        {
            string content = $"{SetNameColor(buddyName, GameConsts.AIBuddyTag + senderId)}{emoteName}";

            ChatUIData chatUIData = new ChatUIData()
            {
                Text = content,
                PlayerName = buddyName,
                ChatType = ChatType.Emote,
            };

            osAdpater.Data.InsertOneAtEnd(chatUIData);
            osAdpater.MoveToEnd();
        }

        public void AddMessage(string msg, ChatType chatType)
        {
            ChatUIData chatUIData = new ChatUIData()
            {
                Text = msg,
                ChatType = chatType,
            };

            osAdpater.Data.InsertOneAtEnd(chatUIData);
            osAdpater.MoveToEnd();
        }

        public void FilterData(ChatType chatType)
        {
            Predicate<ChatUIData> target = (t) => t.ChatType == chatType;

            osAdpater.Data.FilteringCriteria = target;

            osAdpater.MoveToEnd();
        }
        
        public string SetNameColor(string name, string id)
        {
            if (!userColorDic.ContainsKey(id))
            {
                // 随机4种颜色
                int randomIndex = Random.Range(0, UserNameColor.Length);

                string colorStr = UserNameColor[randomIndex];
                userColorDic.Add(id, colorStr);
            }

            name = userColorDic[id] + "[" + name + "]: " + "</c>";

            return name;
        }
    }
    
}
