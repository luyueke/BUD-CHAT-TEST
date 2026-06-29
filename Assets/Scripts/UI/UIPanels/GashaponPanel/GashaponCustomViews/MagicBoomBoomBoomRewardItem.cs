using Basic.Utils;
using Game.Store;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MagicBoomBoomBoomRewardItem : MonoBehaviour
{
    private CButton Btn_Click;
    public GameObject overImage;
    private string _pgcId;

    public void InitData(string pgcId, UnityAction onClick)
    {
        _pgcId = pgcId;
        Btn_Click = this.GetComponent<CButton>();
        Btn_Click.onClick.AddListener(onClick);
        Refresh();
    }

    public void Refresh()
    {
        var isGetted = AssetsDataManager.IsOwned(_pgcId);
        overImage.SetActive(isGetted);
    }
}
