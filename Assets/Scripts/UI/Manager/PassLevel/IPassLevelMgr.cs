using Game.Props.PropsComponents;

public interface IPassLevelMgr
{
    public void OnParseComponentData(PassLevelComponent component);
    
    public void OnPassLevelStart();
    public void OnPassLevelStop();
    public void OnPassLevelPause();
    public void OnPassLevelContinue();

    /// <summary>
    /// 计算通关结果
    /// </summary>
    /// <returns></returns>
    public PassLevelResult CountPassLevelResult();

    /// <summary>
    /// 保存通关后数据到passLevelData
    /// </summary>
    /// <returns></returns>
    public void SavePassLevelData(ref PassLevelData passLevelData);
}