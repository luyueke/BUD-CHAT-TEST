public class AIParkGuideBase
{
    private S11GuideStep _nextStep = S11GuideStep.None;
    public virtual void RunGuide()
    {
        
    }

    public virtual void TriggerNextStep()
    {
        AIParkGuideMgr.Inst.OnStepChange(_nextStep);
    }

    public virtual void SetNextStep(S11GuideStep step)
    {
        _nextStep = step;
    }

    public virtual void TriggerNext()
    {

    }
}

