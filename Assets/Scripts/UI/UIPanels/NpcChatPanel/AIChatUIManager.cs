using Game.KinematicCharacter;
using UI.Avatar;

public class AIChatUIManager: GameInstance<AIChatUIManager>
{
    private AvatarFollowUIMono selfUIMono;
    
    public void RegisterMono(KinematicCharacterController kinematicCharacter)
    {
        if (selfUIMono == null)
        {
            selfUIMono = kinematicCharacter.gameObject.GetComponent<AvatarFollowUIMono>();
        }
    }

    
    public void OnChatRecv(string content)
    {
        if (selfUIMono != null)
        {
            selfUIMono.ChatMessageModule.ShowMessage(0,content);
        }
    }
}