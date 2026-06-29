using System;

public interface IRedDot
{
    public void AddClickAction();

    public void ClearRedDot();

    public void SetData(LocalRedDotData redDotUI, Action<IRedDot> onRelease);

    public void AddData(LocalRedDotData data);

    public bool RemoveRedDotData(LocalRedDotData data);

    public int InstanceID { get; }
}
