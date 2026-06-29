using System;
using GameData.Gashapon;
using GameData.Manager;
using Message;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TokenCommonItem : MonoBehaviour
{
    [SerializeField] private CurrencyType tType = CurrencyType.Coin;
    [SerializeField] private Image icon;
    [SerializeField] private BUD_Text numText;
    [SerializeField] private CButton btn;
    [SerializeField] private Sprite[] sps;

    private Action clickAction;

    private void Awake()
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Click);
        numText.text = TokenDataManager.Inst.Data.GetToken(tType).ToString();
        MessageHelper.AddListener<TokenData>(MessageName.TokenUpdate, TokenUpdate);
    }

    private void TokenUpdate(TokenData data)
    {
        Init(tType, data.GetToken(tType));
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<TokenData>(MessageName.TokenUpdate, TokenUpdate);
    }

    private void Click()
    {
        clickAction?.Invoke();
    }

    public void Init(CurrencyType t, long num = 0)
    {
        tType = t == CurrencyType.None ? CurrencyType.Coin : t;
        icon.sprite = sps[(int) tType - 1];
        numText.text = num.ToString();
    }

    public void SetClickAct(Action act)
    {
        clickAction = act;
    }

    private void OnValidate()
    {
        Init(tType, 0);
    }

}
