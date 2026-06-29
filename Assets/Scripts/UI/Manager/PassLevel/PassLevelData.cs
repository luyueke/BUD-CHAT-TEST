/// <summary>
/// 通关结算数据保存
/// </summary>
public class PassLevelData
{
    public PassLevelResult Result;
    
    //通过条件的获胜类型 1-拾取旗子 2-收集完星星
    public int PassedType;

    //失败类型： 1-timeout 2-hp <-= 0
    public int FailedType;

    //通关花费时间
    public int CostTime;


    //todo:拓展其他模块数据
}


public enum PassLevelFailedType
{
    Timout = 1,
    HP = 2,
}