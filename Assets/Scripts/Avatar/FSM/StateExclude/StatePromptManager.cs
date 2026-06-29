using FSM;
using System.Collections.Generic;

/// <summary>
/// 互斥提示，比如不能牵手的时候不能获取星星的提示等
/// </summary>
public class StatePromptManager : GameInstance<StatePromptManager>
{
    private readonly string PromtConfigPath = "Configs/StatePromptConfig";

    private List<StatePromptData> promptDataList;

    public StatePromptManager()
    {
        //promptDataList = ResManager.Inst.LoadJsonRes<List<StatePromptData>>(PromtConfigPath);
    }

    public string GetPromptContent(PlayerState enterStateID, PlayerState ignoreStateID)
    {
        StateType enterStateType = StateExcludeManager.Inst.GetStateType(enterStateID);
        StateType ignoreStateType = StateExcludeManager.Inst.GetStateType(ignoreStateID);

        if (enterStateType == null || ignoreStateType == null) return null;

        List<object> enterStateTypeList = enterStateType.GetStateType();
        List<object> ignoreStateTypeList = ignoreStateType.GetStateType();

        foreach (var enterType in enterStateTypeList)
        {
            var enterStateDataList = promptDataList.FindAll(data => data.enterType == enterType.ToString());

            foreach (var ignoreType in ignoreStateTypeList)
            {
                StatePromptData promptData = enterStateDataList.Find(data => data.ignoreType == ignoreType.ToString());
                if (promptData != null)
                    return promptData.content;
            }
        }

        return null;
    }

    public override void Release()
    {
        base.Release();

        promptDataList.Clear();
    }
}

public class StatePromptData
{
    public string enterType;
    public string ignoreType;
    public string content;
}