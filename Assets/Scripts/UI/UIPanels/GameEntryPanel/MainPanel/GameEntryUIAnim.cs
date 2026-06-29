using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{

    public class GameEntryUIAnim : MonoBehaviour
    {
        Image Icon;

        Tween t1;
        Tween t2;
        private void Awake()
        {
            Icon = transform.GetComponent<Image>();
            if (Icon != null) {
                t1 = Icon.DOFade(0, 1).OnComplete(() => { t2.Restart(); });
                t1.SetAutoKill(false);
                t2 = Icon.DOFade(1, 1).OnComplete(() => { t1.Restart(); });
                t2.SetAutoKill(false);
                t1.Play();
                t2.Pause();
            }

        }
    }
}