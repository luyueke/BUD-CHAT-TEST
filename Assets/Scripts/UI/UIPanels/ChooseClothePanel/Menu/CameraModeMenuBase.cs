using DG.Tweening;
using UnityEngine;

public abstract class CameraModeMenuBase : MonoBehaviour
{
    protected bool isShowAnim = true;
    private bool isInit = false;
    private Tweener _scaleTweener;
    public virtual void Init()
    {
        if(!isInit){
            isInit = true;
        }
        OnInit();
    }

    protected abstract void OnInit();
    protected abstract void OnShow();
    protected abstract void OnHide();


    protected T GetComponentByName<T>(string name) where T : Component
    {
        return GameObjectEx.FindComponentByName<T>(transform, name);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        if(isShowAnim){
            transform.localScale = Vector3.zero;
            _scaleTweener?.Kill();
            _scaleTweener = transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
        }
        OnShow();
    }

    public virtual void Hide(bool isAnim = false)
    {
        if(isAnim){
            if(isShowAnim){
                _scaleTweener.onComplete = null;
                _scaleTweener?.Kill();
                _scaleTweener = transform.DOScale(0, 0.3f).SetEase(Ease.InBack);
                _scaleTweener.onComplete = () => {
                    gameObject.SetActive(false);
                };
            }
        }else{
            gameObject.SetActive(false);
        }
        OnHide();
    }
}