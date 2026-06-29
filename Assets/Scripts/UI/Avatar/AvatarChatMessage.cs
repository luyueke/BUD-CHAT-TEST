/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-10-08 13:16:40
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-10-09 14:45:48
 * @ Description: 人物头顶聊天气泡
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameData.GameSync;
using Pb.Game;
using UnityEngine;

namespace UI.Avatar
{
    public class AvatarChatMessage : MonoBehaviour
    {
        GameObject rootUI;
        float initY = 2.5f; //聊天气泡初始y值
        float boxMarginY = 0.05f; //聊天气泡间隔
        int maxShowCount = 4; // 最多显示的信息条数
        Queue<AvatarChatBox> boxCachePool = new Queue<AvatarChatBox>();
        Queue<AvatarChatBox> boxAliveQueue = new Queue<AvatarChatBox>();

        int idx = 0;

        private string senderId;
        void Awake()
        {
            idx = 0;
            if (rootUI == null)
            {
                CreateRoot();
            }
        }

        private void CreateRoot()
        {
            rootUI = new GameObject("ChatRoot");
            rootUI.transform.parent = this.transform;
            rootUI.transform.localPosition = new Vector3(0, initY, 0);
            rootUI.transform.localRotation = Quaternion.identity;
        }
        
        public void ShowMessage(int chatBubble, string msg, string senderId = "")
        {
            this.senderId = senderId;
            var chatBox = GetChatBox();
            chatBox.SetBubbleId(chatBubble);
            
            var chatContent = ExtractUserNameFromString(msg, out string atUserName);
            if (!string.IsNullOrEmpty(atUserName))
            {
                atUserName = "<c=E477FF>" +  atUserName  + "</c>";
                chatContent = atUserName + chatContent;
            }
            chatBox.SetText(chatContent);

            var moveY = chatBox.GetHeight() + boxMarginY;
            // 出现动画
            chatBox.Appear();
            // 平移动画
            foreach (var aliveBox in boxAliveQueue)
            {
                aliveBox.Move(moveY);
            }
            // 消失动画
            if (boxAliveQueue.Count >= maxShowCount)
            {
                boxAliveQueue.Dequeue().Disappear();
            }
            
            boxAliveQueue.Enqueue(chatBox);

            Message.MessageHelper.Broadcast(Message.MessageName.ChatMessageLives, senderId, boxAliveQueue.Count);
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


        /// <summary>
        /// 获取一个聊天气泡
        /// </summary>
        AvatarChatBox GetChatBox()
        {
            if (rootUI == null)
            {
                CreateRoot();
            }

            AvatarChatBox box;
            if (boxCachePool.Count>0)
            {
                box = boxCachePool.Dequeue();
            } else {
                var go = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/AvatarChatBox/AvatarChatBox.prefab").Instantiate(rootUI.transform);
                box = go.GetComponent<AvatarChatBox>();
                box.transform.name = "AvatarChatBox" + idx;
                box.AddDisappearListener(OnChatBoxDisappear);
                idx++;
            }
            box.transform.localPosition = Vector3.zero;
            box.gameObject.SetActive(true);
            Debug.Log("AvatarChatBox日志获取 " + box.transform.name);
            return box;
        }

        void OnChatBoxDisappear(AvatarChatBox chatBox)
        {
            Debug.Log("AvatarChatBox日志回收 " + chatBox.transform.name);
            boxCachePool.Enqueue(chatBox);

            if (boxAliveQueue.Peek() == chatBox)
            {
                boxAliveQueue.Dequeue();
            }
            Message.MessageHelper.Broadcast(Message.MessageName.ChatMessageLives, senderId, boxAliveQueue.Count);
        }
    }
}