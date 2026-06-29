
using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using GameData.BaseInfo;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class ActorCardInfoPanel : BasePanel<ActorCardInfoPanel>
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
    private Image ActorListImg;
    private Image actorCardInfoTopTog;

    private Transform bg;
    private RemoteImageBehaviour r_bg;
    private Button bg_close;
    private Transform ActorList;
    private Transform ActorListItem;
     private Transform ActorListItemParent;

     
    [SerializeField] private AvatarCameraController cameraController;
    public Transform CharacterRoot;
    public GameObject AvatarRoot;

    private RenderTexture _wardrobeRenderTexture;
    private CharacterWrap _wardrobeCharacterWrap;
    private string _originalIconUrl;

    public override void OnCreate()
    {
        base.OnCreate();
        bg_close = GameObjectEx.FindComponentByName<Button>(transform,"bg_close");
        //MessageHelper.AddListener(MessageName.ActorCardInfoUpdate,ActorCardInfoUpdate);
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
        ActorListImg = GameObjectEx.FindComponentByName<Image>(transform, "ActorListImg");
        actorCardInfoTopTog = GameObjectEx.FindComponentByName<Image>(transform, "actorCardInfoTopTog");
        ActorList = GameObjectEx.FindComponentByName<Transform>(transform, "ActorList");
        ActorListItem = GameObjectEx.FindComponentByName<Transform>(transform, "ActorListItem");
        ActorListItemParent = GameObjectEx.FindComponentByName<Transform>(transform, "ActorListItemParent");
        ActorList.gameObject.SetActive(false);
        
        MessageHelper.AddListener<object[]>(MessageName.OnSliderValueChanged,OnSliderValueChanged);
        MessageHelper.AddListener<OTCAvatarClothes>(MessageName.ActorCardWardrobeItemClicked, OnActorCardWardrobeItemClicked);

         tog_materials.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_materials, MaterialsInfo); });
         tog_character.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_character, CharacterInfo); });
         tog_wardrobe.onValueChanged.AddListener(isOn => { if (isOn) SwitchTab(tog_wardrobe, WardrobeInfo); });
         tog_wardrobe.onValueChanged.Invoke(true);
         bg_close.onClick.AddListener(CloseSelf);

    }
    OCTheatreAvatarInfo actor;
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
         ActorList.gameObject.SetActive(false);
        if(args != null && args.Length == 2)
        {
            bool isShowList = args[1] as bool? ?? false;

            if(isShowList)
            {
                System.Collections.Generic.List<OCTheatreAvatarOc> actorList;
                if (args[0] is System.Collections.Generic.List<OCTheatreAvatarOc> explicitActors && explicitActors.Count > 0)
                {
                    // 调用方（如 Room 模式）直接传入剧本原始演员列表，跳过 PlayerPrefs
                    actorList = explicitActors;
                }
                else
                {
                    actorList = OCTheatreGameController.Current?.GetDefaultActorList();

                    string theatreID = OCTheatreGameController.Current?.TheatreID;
                    if (!string.IsNullOrEmpty(theatreID))
                    {
                        string json = PlayerPrefs.GetString("OCTheatreActorLineup_" + theatreID, string.Empty);
                        if (!string.IsNullOrEmpty(json))
                        {
                            try
                            {
                                var result = JsonConvert.DeserializeObject<TheatreGameSetPanelSaveData>(json);
                                if (result?.currentPanelActors != null)
                                {
                                    var converted = new System.Collections.Generic.List<OCTheatreAvatarOc>();
                                    foreach (var s in result.currentPanelActors)
                                        converted.Add(new OCTheatreAvatarOc { playerId = s.playerId, avatarName = s.avatarName, avatarURL = s.avatarURL, clothesIndex = s.clothesIndex });
                                    actorList = converted;
                                }
                            }
                            catch
                            {
                                actorList = OCTheatreGameController.Current?.GetDefaultActorList();
                            }
                        }
                    }
                }
                //actor = OCTheatreActorEditorDataManager.Inst.GetActorForId(actorList[0].playerId);
                if(actorList != null)
                {
                    ActorList.gameObject.SetActive(true);
                    for (int i = 0; i < actorList.Count; i++)
                    {
                        var item = Instantiate(ActorListItem, ActorListItemParent);
                        item.gameObject.SetActive(true);
                        var index = i;
                        item.GetComponent<Toggle>().onValueChanged.AddListener((isOn) =>
                        {
                            if (!isOn) return;
                            OCTheatreActorEditorDataManager.Inst.GetActorForId(actorList[index].playerId, selectedActor =>
                            {
                                if (selectedActor == null) return;
                                actor = selectedActor;
                                Init(actor);
                            });
                        });
                        var icon = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(item, "icon");
                        if (!string.IsNullOrEmpty(actorList[i].avatarURL))
                        {
                            icon.Load(actorList[i].avatarURL);
                        }
                        var name = GameObjectEx.FindComponentByName<Text>(item, "name");
                        name.text = actorList[i].avatarName;
                        if(i == 0)
                            item.GetComponent<Toggle>().onValueChanged.Invoke(true);
                    }
                }
                return;
            }
        }
        if(args != null && args.Length == 1)
        {
            actor = args[0] as OCTheatreAvatarInfo;
            Init(actor);
        }
    }
    void Init(OCTheatreAvatarInfo actor)
    {
        InitColor(actor);
        InitBg();
        ActorCardInfoUpdate();
        tog_materials.onValueChanged.Invoke(true);
        tog_materials.isOn = true;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, actor);
    }

    void InitBg()
    {
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

//横条背景默认颜色
    private string defColor1 = "3D4760";
    //强调默认颜色
  private string defColor2 = "7FF6BF";
  //文本默认颜色
    private string defColor3 = "1D253C";
    //背景默认颜色
      private string defColor4 = "9A62FF";

     private void InitColor(OCTheatreAvatarInfo _actor)
    {
        OCTheatreAvatarInfo actor;
        if(_actor != null)
            actor = _actor;
        else
            actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        if(string.IsNullOrEmpty(actor.barColor))
            actor.barColor = defColor1;
        Color m_color1 = ColorUtility.TryParseHtmlString($"#{actor.barColor}", out var color1) ? color1 : Color.white;
      
        if(string.IsNullOrEmpty(actor.mainColor))
            actor.mainColor = defColor2;
        Color m_color2 = ColorUtility.TryParseHtmlString($"#{actor.mainColor}", out var color2) ? color2 : Color.white;
      
        if(string.IsNullOrEmpty(actor.textColor))
            actor.textColor = defColor3;
        Color m_color3 = ColorUtility.TryParseHtmlString($"#{actor.textColor}", out var color3) ? color3 : Color.white;
       
          if(string.IsNullOrEmpty(actor.bgColor))
            actor.bgColor = defColor4;
        Color m_color4 = ColorUtility.TryParseHtmlString($"#{actor.bgColor}", out var color4) ? color4 : Color.white;
       
        object[] m_args = new object[2];
        m_args[0] = 1;
        m_args[1] = m_color1;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        m_args[0] = 2;
        m_args[1] = m_color2;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        m_args[0] = 3;
        m_args[1] = m_color3;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
        m_args[0] = 4;
        m_args[1] = m_color4;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
    }
    public void SetBgUrl(string url)
    {
        r_bg.Load(url);
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
        if (actor == null || cameraController == null || CharacterRoot == null) return;

        var clothes = actor.avatarClothes;
        var defaultClothes = clothes?.Find(c => c.isDef == 1) ?? (clothes?.Count > 0 ? clothes[0] : null);
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
                ActorListImg.color = m_color;
                 break;
                break;
            default:
                
                break;
        }
    }


    private void ActorCardInfoUpdate()
    {
        name.text = actor.name;
        _originalIconUrl = actor.cover;
        if (!tog_wardrobe.isOn)
        {
            icon.ResetRawImage();
            if (!string.IsNullOrEmpty(actor.cover))
                icon.Load(actor.cover);
        }
        // sex0.gameObject.SetActive(actor.gender == 0);
        // sex1.gameObject.SetActive(actor.gender == 1);
        // sex2.gameObject.SetActive(actor.gender == 2);
        //ChangeTitleBgColor(actor.barColor);
        if (!string.IsNullOrEmpty(actor.bgColor) && ColorUtility.TryParseHtmlString("#" + actor.bgColor, out var c1))
        {
            bg_color.color = c1;
            ActorListImg.color = c1;
        }
        // if (!string.IsNullOrEmpty(actor.textColor) && ColorUtility.TryParseHtmlString("#" + actor.textColor, out var c2))
        //     txt_title.color = c2;
    }

    //改变横条背景颜色
    // private void ChangeTitleBgColor(string color)
    // {
    //     if (!string.IsNullOrEmpty(color) && ColorUtility.TryParseHtmlString("#" + color, out var c))
    //         txt_title.color = c;
    // }
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



    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener<object[]>(MessageName.OnSliderValueChanged,OnSliderValueChanged);
        MessageHelper.RemoveListener<OTCAvatarClothes>(MessageName.ActorCardWardrobeItemClicked, OnActorCardWardrobeItemClicked);
        //MessageHelper.RemoveListener(MessageName.ActorCardInfoUpdate,ActorCardInfoUpdate);

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