using System;
using GameData.GameSync;

namespace Game.Utils
{
    public class ChatDispatcherUtils:GameInstance<ChatDispatcherUtils>
    {
        Action<string, ChatType, string> eventAction;

        public void Dispatch(string msg, ChatType chatType, string playerId = "")
        {
            eventAction?.Invoke(msg, chatType, playerId);
        }

        public void AddChatListener(Action<string, ChatType, string> action)
        {
            eventAction += action;
        }

        public void RemoveChatListener(Action<string, ChatType, string> action)
        {
            eventAction -= action;
        }
    }
}