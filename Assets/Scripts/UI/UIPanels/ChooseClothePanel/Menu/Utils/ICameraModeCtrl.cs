public interface ICameraModeCtrl
{
    public void OnTrigger();
    public void OnValueChanged(int value);
    public void OnRelease();
    public void OnReset();
    public int GetValue();
    public int GetMaxValue();
    public int GetMinValue();
}