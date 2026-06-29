using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class SettingTipsPanel : BasePanel<SettingTipsPanel>
{
    private Transform back;
    private Text content;
    private const float DEFAULT_SCREEN_HEIGHT = 1125;
    private const int OFFSET_DOWN = 50;
    private const int OFFSET_UP = 13;


    public override void OnCreate()
    {
        base.OnCreate();
        back = GameObjectEx.FindChildByName(this.transform, "Back");
        transform.Find("Mask").GetComponent<Button>().onClick.AddListener(CloseSelf);
        content = GameObjectEx.FindChildByName(this.transform, "Txt_Tips").GetComponent<Text>();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        AdjustTipsPosition((string)args[0], (Transform)args[1]);
    }


    public void AdjustTipsPosition(string text,Transform anchor)
    {
        content.SetLocalText(text);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.transform.GetComponent<RectTransform>());
        RectTransform rectTrans = GameObject.Find("Canvas").GetComponent<RectTransform>();
        Camera c = rectTrans.GetComponent<Canvas>().worldCamera;
        float factor = DEFAULT_SCREEN_HEIGHT / Screen.height;
        Vector3 worldToScreenPoint = c.WorldToScreenPoint(anchor.position);
        Vector3 real = worldToScreenPoint * factor;
        back.GetComponent<RectTransform>().anchoredPosition = new Vector2(real.x, real.y) + Vector2.down * OFFSET_DOWN + Vector2.up * OFFSET_UP;
    }
}
