using Basic.Utils;
using Game.Store;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FlowerCarriageRewardItem : MonoBehaviour
{
    private CButton Btn_Click;
    private Image Img_Sprite;
    private GameObject Go_UnGetted;
    private GameObject Go_Getted;
    private string _pgcId;

    public void InitData(string pgcId, UnityAction onClick)
    {
        _pgcId = pgcId;
        Btn_Click = this.GetComponent<CButton>();
        Img_Sprite = GameUtils.FindChildByName(this.transform, "Img_Icon").GetComponent<Image>();
        Go_UnGetted = GameUtils.FindChildByName(this.transform, "Go_UnGetted").gameObject;
        Go_Getted = GameUtils.FindChildByName(this.transform, "Go_Getted").gameObject;
        if (pgcId != "40900489")
        {
            Img_Sprite.sprite = PgcUtils.GetIconSpriteByPgcId(_pgcId, gameObject);
        }
        Btn_Click.onClick.AddListener(onClick);
        Refresh();
    }

    public void Refresh()
    {
        var isGetted = AssetsDataManager.IsOwned(_pgcId);
        Go_UnGetted.SetActive(!isGetted);
        Go_Getted.SetActive(isGetted);
    }
}
