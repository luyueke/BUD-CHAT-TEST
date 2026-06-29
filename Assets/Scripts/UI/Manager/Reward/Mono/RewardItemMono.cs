using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemMono : MonoBehaviour{
    public Image bg;
    public Image icon;
    public Text num;
    public Button previewBtn;
    BUDRewardType _rewardType;
    int _rewardNum;

    public string pgcId;
    public string name;
    public string spriteatlasPath;
    public string color;
    public List<string> icons;

    public void Awake()
    {
        if(previewBtn == null){
            previewBtn = transform.Find("previewBtn")?.GetComponent<Button>();
        }
        previewBtn?.onClick?.AddListener(Preview);
    }

    public void SetData(BUDRewardType rewardType, int rewardNum)
    {
        _rewardType = rewardType;
        _rewardNum = rewardNum;
        icon.sprite = PgcUtils.LoadRewardIcon(rewardType, icon.gameObject);
        num.text = rewardNum.ToString();
    }

    public void SetRewardType(BUDRewardType rewardType)
    {
        _rewardType = rewardType;
    }

    void Preview()
    {
        //预览的扩展
        if(string.IsNullOrEmpty(pgcId)){
            //货币
            // PreviewManager.Inst.ShowCurrencyPreview();
        }
        else
        {
            //动作
            PreviewManager.Inst.ShowPreview(pgcId, name, spriteatlasPath, color, icons);
        }
    }

    public void SetBg(Sprite bgSprite)
    {
        bg.sprite = bgSprite;
    }
}

