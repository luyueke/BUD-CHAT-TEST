using GameData.Gashapon;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using GameData.Manager;

public class TokenInfoPanel : BasePanel<TokenInfoPanel>
{
    [SerializeField] private CurrencyType tType = CurrencyType.Coin;
    [SerializeField] private Image icon;
    [SerializeField] private BUD_Text name;
    [SerializeField] private BUD_Text numText;
    [SerializeField] private BUD_Text desc;
    [SerializeField] private CButton btn;
    [SerializeField] private Sprite[] sps;

    private string[] names = {"金币", "徽章", "宝石"};

    private string[] descs =
        {"多多益善的货币，可通过完成任务获取，可用于买皮肤和抽扭蛋。", "珍贵的货币，偶尔可通过赢得比赛或者活动获取，可用于购买皮肤和抽取扭蛋。", "BUD里最珍贵的货币，实打实的硬通货，可以购买稀有的皮肤和抽取扭蛋。"};

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        tType = args[0] is CurrencyType ? (CurrencyType) args[0] : CurrencyType.Coin;
        var num = args[1] is int ? (int) args[1] : 0;
        init(tType, num);
    }

    public override void OnCreate()
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(CloseClick);
    }

    private void CloseClick()
    {
        CloseSelf();
    }

    public void init(CurrencyType t, int num = 0)
    {
        tType = t;
        icon.sprite = sps[(int) tType - 1];
        name.text = names[(int) tType];
        desc.text = descs[(int) tType];
        numText.text = $"现有：{num}";
    }

    private void OnValidate()
    {
        if (Application.isEditor) {
            return;
        }

        init(tType);
    }
}
