
using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using GameData.BaseInfo;
using Message;
using UnityEngine;
using UnityEngine.UI;

public class ActorCardInfoView : MonoBehaviour
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

    private Text name;
    private RemoteImageBehaviour icon;
    private Text txt_title;

    private Image bg_color;
    private Image actorCardInfoTopTog;

    private Transform bg;
    private RemoteImageBehaviour r_bg;

    [SerializeField] private AvatarCameraController cameraController;
    public Transform CharacterRoot;
    public GameObject AvatarRoot;

    private RenderTexture _wardrobeRenderTexture;
    private CharacterWrap _wardrobeCharacterWrap;
    private string _originalIconUrl;

    private void Awake()
    {
        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, ActorCardInfoUpdate);
        bg =  GameObjectEx.FindChildByName(transform, "bg");
        r_bg = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform,"bg1");
        tog_materials = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_materials");
        tog_character = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_character");
        tog_wardrobe = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_wardrobe");
        MaterialsInfo = GameObjectEx.FindComponentByName<Transform>(transform, "MaterialsInfo");
        CharacterInfo = GameObjectEx.FindComponentByName<Transform>(transform, "CharacterInfo");
        WardrobeInfo = GameObjectEx.FindComponentByName<Transform>(transform, "WardrobeInfo");
        name = GameObjectEx.FindComponentByName<Text>(transform, "name");
        icon = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "icon");
        sex1 = GameObjectEx.FindComponentByName<Transform>(transform, "sex1");
        sex2 = GameObjectEx.FindComponentByName<Transform>(transform, "sex2");
        sex0 = GameObjectEx.FindComponentByName<Transform>(transform, "sex0");
        txt_title = GameObjectEx.FindComponentByName<Text>(transform, "txt_title");
        bg_color = GameObjectEx.FindComponentByName<Image>(transform, "bg_color");
        actorCardInfoTopTog = GameObjectEx.FindComponentByName<Image>(transform, "actorCardInfoTopTog");

        MessageHelper.AddListener<object[]>(MessageName.OnSliderValueChanged, OnSliderValueChanged);
        MessageHelper.AddListener<OTCAvatarClothes>(MessageName.ActorCardWardrobeItemClicked, OnActorCardWardrobeItemClicked);

        tog_materials.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_materials, MaterialsInfo); });
        tog_character.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_character, CharacterInfo); });
        tog_wardrobe.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_wardrobe, WardrobeInfo); });
        tog_wardrobe.onValueChanged.Invoke(true);
    }

    private void ChangeBg(OCTheatreAvatarInfo _actor)
    {
        OCTheatreAvatarInfo actor;
        if(_actor == null)
        {
            actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        }
        else
        {
            actor = _actor;
        }
        string url = actor.bgUrl;
        if(string.IsNullOrEmpty(url))
        {
            r_bg.ResetRawImage();
            r_bg.gameObject.SetActive(false);
            bg.gameObject.SetActive(true);
            return;
        }
        SetBgUrl(url);
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

        if (activeInfo == WardrobeInfo)
            OnWardrobeTabActivated();
        else
            OnOtherTabActivated();
    }

    private void OnWardrobeTabActivated()
    {
        if (cameraController == null || CharacterRoot == null) return;
        var curActor = OCTheatreActorEditorDataManager.Inst?.GetCurActor();
        if (curActor == null) return;

        var defaultClothes = OCTheatreActorEditorDataManager.Inst?.GetDefaultClothes();
        ShowAvatarWithClothes(defaultClothes?.clothesJson);

        if (_wardrobeRenderTexture == null)
            _wardrobeRenderTexture = new RenderTexture(256, 256, 24);

        cameraController.roleCamera.targetTexture = _wardrobeRenderTexture;
        icon.RawImage.texture = _wardrobeRenderTexture;

        if (AvatarRoot != null) AvatarRoot.SetActive(true);
    }

    private void OnOtherTabActivated()
    {
        if (cameraController != null && cameraController.roleCamera != null)
            cameraController.roleCamera.targetTexture = null;

        icon.ResetRawImage();
        if (!string.IsNullOrEmpty(_originalIconUrl))
            icon.Load(_originalIconUrl);

        if (AvatarRoot != null) AvatarRoot.SetActive(false);
    }

    private void ShowAvatarWithClothes(string clothesJson)
    {
        CharacterData characterData = null;
        if (!string.IsNullOrEmpty(clothesJson))
            characterData = CharacterData.DeserializeObject(clothesJson);
        if (characterData == null)
            characterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();

        if (_wardrobeCharacterWrap != null && _wardrobeCharacterWrap.Avatar != null)
            Destroy(_wardrobeCharacterWrap.Avatar.gameObject);

        _wardrobeCharacterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
        _wardrobeCharacterWrap.SetParent(CharacterRoot, true);
        cameraController.RotateTarget = CharacterRoot;
        cameraController.ZoomCustom(Vector3.zero, cameraController.ZoomWholeCameraSize * 0.6f);
    }

    private void OnActorCardWardrobeItemClicked(OTCAvatarClothes clothes)
    {
        if (!tog_wardrobe.isOn || cameraController == null) return;
        ShowAvatarWithClothes(clothes?.clothesJson);
    }

    private void OnSliderValueChanged(object[] args)
    {
        int type = args[0] as int? ?? 1;
        Color m_color = args[1] as Color? ?? default;
        switch (type)
        {
            case 1:
                Debug.Log("dddd type 1");
                actorCardInfoTopTog.color = m_color;
                break;
            case 2:
                tog_materials.transform.Find("Background/Checkmark").gameObject.GetComponent<Image>().color = m_color;
                tog_character.transform.Find("Background/Checkmark").gameObject.GetComponent<Image>().color = m_color;
                tog_wardrobe.transform.Find("Background/Checkmark").gameObject.GetComponent<Image>().color = m_color;
                break;
            case 3:
                // txt_title.color = m_color;
                // tog_materials.transform.Find("Background/Checkmark/Label").gameObject.GetComponent<Text>().color = m_color;
                // tog_materials.transform.Find("Background/Label").gameObject.GetComponent<Text>().color = m_color;
                // tog_character.transform.Find("Background/Checkmark/Label").gameObject.GetComponent<Text>().color = m_color;
                // tog_character.transform.Find("Background/Label").gameObject.GetComponent<Text>().color = m_color;
                // tog_wardrobe.transform.Find("Background/Checkmark/Label").gameObject.GetComponent<Text>().color = m_color;
                // tog_wardrobe.transform.Find("Background/Label").gameObject.GetComponent<Text>().color = m_color;
                break;
            case 4:
                bg_color.color = m_color;
                break;
            default:
                break;
        }
    }

    private void ActorCardInfoUpdate(OCTheatreAvatarInfo actor)
    {
        OCTheatreAvatarInfo m_info;
        if (actor != null)
            m_info = actor;
        else
            m_info = OCTheatreActorEditorDataManager.Inst.GetCurActor();

        name.text = m_info.name;
        _originalIconUrl = m_info.cover;

        if (!tog_wardrobe.isOn)
        {
            icon.ResetRawImage();
            if (!string.IsNullOrEmpty(m_info.cover))
            {
                icon.gameObject.SetActive(true);
                icon.Load(m_info.cover);
            }
            else
            {
                icon.gameObject.SetActive(false);
            }
        }

        // sex0.gameObject.SetActive(m_info.gender == 0);
        // sex1.gameObject.SetActive(m_info.gender == 1);
        // sex2.gameObject.SetActive(m_info.gender == 2);
        if (!string.IsNullOrEmpty(m_info.bgColor) && ColorUtility.TryParseHtmlString("#" + m_info.bgColor, out var c1))
            bg_color.color = c1;

        ChangeBg(m_info);
    }

    //改变强调色  -toggle按钮颜色
    private void ChangeToggleColor()
    {
    }

    //改变文字颜色
    private void ChangeTextColor()
    {
    }

    private void ChangeToggleBgColor()
    {
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<object[]>(MessageName.OnSliderValueChanged, OnSliderValueChanged);
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, ActorCardInfoUpdate);
        MessageHelper.RemoveListener<OTCAvatarClothes>(MessageName.ActorCardWardrobeItemClicked, OnActorCardWardrobeItemClicked);

        if (_wardrobeCharacterWrap != null && _wardrobeCharacterWrap.Avatar != null)
        {
            Destroy(_wardrobeCharacterWrap.Avatar.gameObject);
            _wardrobeCharacterWrap = null;
        }
        if (_wardrobeRenderTexture != null)
        {
            if (cameraController != null && cameraController.roleCamera != null)
                cameraController.roleCamera.targetTexture = null;
            _wardrobeRenderTexture.Release();
            Destroy(_wardrobeRenderTexture);
            _wardrobeRenderTexture = null;
        }
    }
}
