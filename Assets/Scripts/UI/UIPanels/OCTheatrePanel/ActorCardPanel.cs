using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class ActorCardPanel : BasePanel<ActorCardPanel>
{
    private Toggle tog_materials;
    private Toggle tog_character;
    private Toggle tog_wardrobe;

    private Transform MaterialsInfo;
    private Transform CharacterInfo;
    private Transform WardrobeInfo;

    private Transform sex1;
    private Transform sex2;
    private Transform sex0;

    private Text actorName;
    private RemoteImageBehaviour icon;
    private Text txt_title;

    private Image bg_color;
    private Image actorCardInfoTopTog;

    private Transform bg;
    private RemoteImageBehaviour r_bg;
    private Button bg_close;

    private OCTheatreAvatarInfo actor;

    // 默认颜色
    private string defColor1 = "3D4760";
    private string defColor2 = "7FF6BF";
    private string defColor3 = "1D253C";
    private string defColor4 = "9A62FF";

    public override void OnCreate()
    {
        base.OnCreate();
        bg_close = GameObjectEx.FindComponentByName<Button>(transform, "CloseBtn");
        bg = GameObjectEx.FindChildByName(transform, "bg");
        r_bg = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "bg1");
        tog_materials = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_materials");
        tog_character = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_character");
        tog_wardrobe = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_wardrobe");
        MaterialsInfo = GameObjectEx.FindComponentByName<Transform>(transform, "MaterialsInfo");
        CharacterInfo = GameObjectEx.FindComponentByName<Transform>(transform, "CharacterInfo");
        WardrobeInfo = GameObjectEx.FindComponentByName<Transform>(transform, "WardrobeInfo");
        actorName = GameObjectEx.FindComponentByName<Text>(transform, "name");
        icon = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "icon");
        sex1 = GameObjectEx.FindComponentByName<Transform>(transform, "sex1");
        sex2 = GameObjectEx.FindComponentByName<Transform>(transform, "sex2");
        sex0 = GameObjectEx.FindComponentByName<Transform>(transform, "sex0");
        txt_title = GameObjectEx.FindComponentByName<Text>(transform, "txt_title");
        bg_color = GameObjectEx.FindComponentByName<Image>(transform, "bg_color");
        actorCardInfoTopTog = GameObjectEx.FindComponentByName<Image>(transform, "actorCardInfoTopTog");

        MessageHelper.AddListener<object[]>(MessageName.OnSliderValueChanged, OnSliderValueChanged);

        tog_materials.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_materials, MaterialsInfo); });
        tog_character.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_character, CharacterInfo); });
        tog_wardrobe.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_wardrobe, WardrobeInfo); });
        tog_wardrobe.onValueChanged.Invoke(true);
        bg_close.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        actor = args[0] as OCTheatreAvatarInfo;
        InitColor(actor);
        InitBg();
        ActorCardInfoUpdate();
        tog_materials.onValueChanged.Invoke(true);
        tog_materials.isOn = true;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, actor);
    }

    private void InitBg()
    {
        string url = actor.bgUrl;
        if (string.IsNullOrEmpty(url))
        {
            r_bg.ResetRawImage();
            r_bg.gameObject.SetActive(false);
            bg.gameObject.SetActive(true);
            return;
        }
        SetBgUrl(url);
    }

    private void InitColor(OCTheatreAvatarInfo _actor)
    {
        OCTheatreAvatarInfo a;
        if (_actor != null)
            a = _actor;
        else
            a = OCTheatreActorEditorDataManager.Inst.GetCurActor();

        if (string.IsNullOrEmpty(a.barColor)) a.barColor = defColor1;
        Color m_color1 = ColorUtility.TryParseHtmlString($"#{a.barColor}", out var color1) ? color1 : Color.white;

        if (string.IsNullOrEmpty(a.mainColor)) a.mainColor = defColor2;
        Color m_color2 = ColorUtility.TryParseHtmlString($"#{a.mainColor}", out var color2) ? color2 : Color.white;

        if (string.IsNullOrEmpty(a.textColor)) a.textColor = defColor3;
        Color m_color3 = ColorUtility.TryParseHtmlString($"#{a.textColor}", out var color3) ? color3 : Color.white;

        if (string.IsNullOrEmpty(a.bgColor)) a.bgColor = defColor4;
        Color m_color4 = ColorUtility.TryParseHtmlString($"#{a.bgColor}", out var color4) ? color4 : Color.white;

        object[] m_args = new object[2];
        m_args[0] = 1; m_args[1] = m_color1;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged, m_args);
        m_args[0] = 2; m_args[1] = m_color2;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged, m_args);
        m_args[0] = 3; m_args[1] = m_color3;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged, m_args);
        m_args[0] = 4; m_args[1] = m_color4;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged, m_args);
    }

    public void SetBgUrl(string url)
    {
        r_bg.Load(url);
        r_bg.gameObject.SetActive(true);
        bg.gameObject.SetActive(false);
    }

    private void SwitchTab(Toggle activeToggle, Transform activeInfo)
    {
        MaterialsInfo.gameObject.SetActive(activeInfo == MaterialsInfo);
        CharacterInfo.gameObject.SetActive(activeInfo == CharacterInfo);
        WardrobeInfo.gameObject.SetActive(activeInfo == WardrobeInfo);
        GameObjectEx.FindChildByName(tog_materials.transform, "Label1").gameObject.SetActive(activeToggle != tog_materials);
        GameObjectEx.FindChildByName(tog_character.transform, "Label1").gameObject.SetActive(activeToggle != tog_character);
        GameObjectEx.FindChildByName(tog_wardrobe.transform, "Label1").gameObject.SetActive(activeToggle != tog_wardrobe);
    }

    private void OnSliderValueChanged(object[] args)
    {
        int type = args[0] as int? ?? 1;
        Color m_color = args[1] as Color? ?? default;
        switch (type)
        {
            case 1:
                actorCardInfoTopTog.color = m_color;
                break;
            case 2:
                tog_materials.transform.Find("Background/Checkmark").gameObject.GetComponent<Image>().color = m_color;
                tog_character.transform.Find("Background/Checkmark").gameObject.GetComponent<Image>().color = m_color;
                tog_wardrobe.transform.Find("Background/Checkmark").gameObject.GetComponent<Image>().color = m_color;
                break;
            case 3:
                break;
            case 4:
                bg_color.color = m_color;
                break;
        }
    }

    private void ActorCardInfoUpdate()
    {
        actorName.text = actor.name;
        icon.ResetRawImage();
        if (!string.IsNullOrEmpty(actor.cover))
        {
            icon.Load(actor.cover);
        }
        sex0.gameObject.SetActive(actor.gender == 0);
        sex1.gameObject.SetActive(actor.gender == 1);
        sex2.gameObject.SetActive(actor.gender == 2);
        ChangeTitleBgColor(actor.barColor);
        if (!string.IsNullOrEmpty(actor.bgColor) && ColorUtility.TryParseHtmlString("#" + actor.bgColor, out var c1))
            bg_color.color = c1;
        if (!string.IsNullOrEmpty(actor.textColor) && ColorUtility.TryParseHtmlString("#" + actor.textColor, out var c2))
            txt_title.color = c2;
    }

    private void ChangeTitleBgColor(string color)
    {
        if (!string.IsNullOrEmpty(color) && ColorUtility.TryParseHtmlString("#" + color, out var c))
            txt_title.color = c;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<object[]>(MessageName.OnSliderValueChanged, OnSliderValueChanged);
    }
}
