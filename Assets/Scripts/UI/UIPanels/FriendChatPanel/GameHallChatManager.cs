using System.Collections.Generic;

public class GameHallChatManager : GlobalInstance<GameHallChatManager>
{
    private List<OfflineMessageItem> _chatContents = new List<OfflineMessageItem>();
    private BudTimer _chatTimer;
    private const int Chat_Limit_Time = 10;//间隔十秒才能接着法信息\
    public bool canSend = true;

    //本地发送的信息记录，后端发送的本地信息不记录
    public void SetSelfContents(List<OfflineMessageItem> contents)
    {
        for (int i = 0; i < contents.Count; i++)
        {
            _chatContents.Add(contents[i]);
        }
        canSend = false;
        _chatTimer = TimerManager.Inst.RunOnce("_chatTimer",Chat_Limit_Time ,()=>
        {
            canSend = true;
        });
    }
    public override void Release()
    {
        base.Release();
        TimerManager.Inst.Stop(_chatTimer);
    }

}