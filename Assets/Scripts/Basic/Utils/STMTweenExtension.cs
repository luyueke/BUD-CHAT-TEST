using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public static class STMTweenExtension
{

    public static TweenerCore<string, string, StringOptions> DOText(this SuperTextMesh target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
    {
        if (endValue == null)
        {
            if (Debugger.logPriority > 0) Debugger.LogWarning("You can't pass a NULL string to DOText: an empty string will be used instead to avoid errors");
            endValue = "";
        }
        TweenerCore<string, string, StringOptions> t = DOTween.To(() => target?.text, x =>
        {
            if (target != null)
                target.text = x;
        }, endValue, duration);
        t.SetOptions(richTextEnabled, scrambleMode, scrambleChars)
            .SetTarget(target);
        return t;
    }
}