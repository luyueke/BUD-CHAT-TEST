using System.Collections.Generic;
using Game.Avatar;
using Game.Config;
using Game.KinematicCharacter;
using GameData.GameSync;
using Message;
using Pb.Game;

namespace UI.Avatar
{
    public class AcatarFollowUIManager : GameInstance<AcatarFollowUIManager>
    {
        Dictionary<string, AvatarFollowUIMono> cache = new Dictionary<string, AvatarFollowUIMono>();

        public void Init(KinematicCharacterController selfKinematicCharacter)
        {
            RegisterMono(AccountDataManager.Inst.Uid, selfKinematicCharacter);
        }

        public AcatarFollowUIManager()
        {
            NetSyncManager.Inst.AddBroadcastListener(SubCmdType.Chat, OnChatRecv);
            AvatarController.Inst.AddAvatarCreateListener(OnAvatarCreate);
            AvatarController.Inst.AddAvatarRemoveListener(OnAvatarRemove);

            AIBuddyAvatarController.Inst.AddAvatarCreateListener(OnAvatarCreate);
            AIBuddyAvatarController.Inst.AddAvatarRemoveListener(OnAvatarRemove);

            GameAIBuddyChatManager.Inst.AddLocalBuddyChatMsgListener(OnLocalChatRecv);

            MessageHelper.AddListener<string, InteractSyncData>(MessageName.OnBuddyCommandChat, OnBuddyCommandChat);
        }

        public override void Release()
        {
            base.Release();
            NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.Chat, OnChatRecv);
            AvatarController.Inst.RemoveAvatarCreateListener(OnAvatarCreate);
            AvatarController.Inst.RemoveAvatarRemoveListener(OnAvatarRemove);

            AIBuddyAvatarController.Inst.RemoveAvatarCreateListener(OnAvatarCreate);
            AIBuddyAvatarController.Inst.RemoveAvatarRemoveListener(OnAvatarRemove);

            GameAIBuddyChatManager.Inst.RemoveLocalBuddyChatMsgListener(OnLocalChatRecv);

            MessageHelper.RemoveListener<string, InteractSyncData>(MessageName.OnBuddyCommandChat, OnBuddyCommandChat);
        }

        // AI 伙伴口令互动：在 buddy 头顶气泡显示台词
        void OnBuddyCommandChat(string playerId, InteractSyncData data)
        {
            if (string.IsNullOrEmpty(data?.Text)) return;
            var key = GameConsts.AIBuddyTag + playerId;
            if (cache.ContainsKey(key))
            {
                cache[key].ChatMessageModule.ShowMessage(0, data.Text);
            }
        }

        void RegisterMono(string playerId, KinematicCharacterController kinematicCharacter)
        {
            if (!cache.ContainsKey(playerId))
            {
                var mono = kinematicCharacter.gameObject.AddComponent<AvatarFollowUIMono>();
                cache.Add(playerId, mono);
            }
        }

        void OnChatRecv(CommonSyncClientData netData)
        {
            var chatNetData = (ChatNetData)netData.Body;
            var senderId = netData.PalyerId;
            if (string.IsNullOrEmpty(chatNetData.BuddyId))
            {
                if (cache.ContainsKey(senderId))
                {
                    cache[senderId].ChatMessageModule.ShowMessage(chatNetData.ChatBubbles, chatNetData.Content,senderId);
                }
            }
            else
            {
                var key = GameConsts.AIBuddyTag + senderId;
                if (cache.ContainsKey(key))
                {
                    cache[key].ChatMessageModule.ShowMessage(chatNetData.ChatBubbles, chatNetData.Content);
                }
            }
            
        }

        void OnLocalChatRecv(string id, string name, string content)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (cache.ContainsKey(id))
                {
                    var chatBubble = 0;
                    if (id == AccountDataManager.Inst.Uid)
                    {
                        chatBubble = AccountDataManager.Inst.UserInfo.chatBubbles;
                    }
                    cache[id].ChatMessageModule.ShowMessage(chatBubble, content);
                }
            }
            
        }

        void OnAvatarCreate(string playerId, KinematicCharacterController kinematicCharacter)
        {
            RegisterMono(playerId, kinematicCharacter);
        }

        void OnAvatarRemove(string playerId)
        {
            if (cache.ContainsKey(playerId))
            {
                cache.Remove(playerId);
            }
        }
    }
}
