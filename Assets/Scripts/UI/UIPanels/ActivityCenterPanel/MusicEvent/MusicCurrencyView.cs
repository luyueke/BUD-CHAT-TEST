using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class MusicCurrencyView : MonoBehaviour
{
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Text currencyNum;
    [SerializeField] private CButton exchangeBtn;
    [SerializeField] private CButton previewBtn;
    [SerializeField] private Image priceIcon;

    public Action<int> balanceChangeAction;

    public int balance;
    private ActivityId _activityId;
    private string currencyName = "钢琴活动币";
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";

    void Start()
    {
        previewBtn?.onClick.AddListener(OnShowPreview);
        exchangeBtn?.onClick.AddListener(OnExchange);
    }

    public void SetData(ActivityId activityId)
    {
        _activityId = activityId;
        string iconName = "icn_common_piano_big";
        if (_activityId == ActivityId.PetStudio)
        {
            iconName = "icn_common_frog_big";
            currencyName = "青蛙活动币";
        } else if (_activityId == ActivityId.AnimationStudio) {
            iconName = "icn_reward_movie_big";
            currencyName = "电影币";
        }
        priceIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, iconName, gameObject);
        iconSprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, iconName, gameObject);
    }

    public void UpdateCurrency(int num)
    {
        balance = num;
        currencyNum.text = num.ToString();
    }

    private void OnShowPreview()
    {
        if (iconSprite == null)
        {
            return;
        }
       var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
       string desc = "DIY乐器上线活动币，活动结束后自动失效，可通过完成活动任务领取，也可通过钻石兑换。";
       if (_activityId == ActivityId.PetStudio)
       {
           desc = "青蛙活动币，活动结束后自动失效，可通过完成活动任务领取，也可通过钻石兑换。";
       } else if (_activityId == ActivityId.AnimationStudio) {
           desc = "动画工作室活动币，活动结束后自动失效，可通过完成活动任务领取，也可通过钻石兑换。";
       }
       panel.UpdateUI(iconSprite, currencyName, currencyNum.text, desc);
    }

    public void OnExchange()
    {
        ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
        exchangeCoinPanel.SetPianoCoinEventUI(iconSprite, _activityId.ToString(), i =>
        {
            if (this == null)
            {
                return;
            }
            balanceChangeAction?.Invoke(i);
            UpdateCurrency(i);
        }, currencyName);
    }

}
