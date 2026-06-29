using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TheatreGameSetPanelItem : MonoBehaviour
{
    private Transform bg_my;
    private Transform bg_def;
    private Transform bg_select;
    private RemoteImageBehaviour r_image;
    private Transform img_def;
    private Transform img_my;
    private Transform bg_null;
    private Text txt_name; 
    private OCTheatreAvatarOc _actorData;
    private UnityEngine.Events.UnityAction _onClickAction;
    private int _currentClothesIndex = -1;

    public OCTheatreAvatarOc ActorData => _actorData;
    public int CurrentClothesIndex => _currentClothesIndex;

    void Awake() => EnsureInitialized();

    private void EnsureInitialized()
    {
        if (bg_null != null) return;
        bg_my = GameObjectEx.FindChildByName(transform, "bg_my");
        bg_def = GameObjectEx.FindChildByName(transform, "bg_def");
        bg_select = GameObjectEx.FindChildByName(transform, "bg_select");
        r_image = GameObjectEx.FindComponentByName<RemoteImageBehaviour>(transform, "r_image");
        img_def = GameObjectEx.FindChildByName(transform, "img_def");
        img_my = GameObjectEx.FindChildByName(transform, "img_my");
        bg_null = GameObjectEx.FindChildByName(transform, "bg_null");
        txt_name = GameObjectEx.FindComponentByName<Text>(transform, "txt_name");
    }

    public void SetData(OCTheatreAvatarOc actorData)
    {
        EnsureInitialized();
        _actorData = actorData;
        _currentClothesIndex = actorData.clothesIndex;
        txt_name.text = actorData.avatarName;
        bool hasUrl = !string.IsNullOrEmpty(actorData.avatarURL);
        r_image.gameObject.SetActive(hasUrl);
        img_def.gameObject.SetActive(!hasUrl);
        img_my.gameObject.SetActive(false);
        if (hasUrl)
            r_image.Load(actorData.avatarURL);
        bg_def.gameObject.SetActive(true);
        bg_my.gameObject.SetActive(false);
        bg_select.gameObject.SetActive(false);
        bg_null.gameObject.SetActive(false);
       
    }

    public void SetMyData(OCTheatreAvatarInfo actorInfo)
    {
        EnsureInitialized();
        _actorData = new OCTheatreAvatarOc
        {
            playerId = actorInfo.id,
            avatarName = actorInfo.name,
            avatarURL = actorInfo.cover,
            clothesIndex = -1
        };
        txt_name.text = actorInfo.name;
        bool hasUrl = !string.IsNullOrEmpty(actorInfo.cover);
        r_image.gameObject.SetActive(hasUrl);
        img_my.gameObject.SetActive(!hasUrl);
        img_def.gameObject.SetActive(false);
        if (hasUrl)
            r_image.Load(actorInfo.cover);
        bg_my.gameObject.SetActive(true);
        bg_def.gameObject.SetActive(false);
        bg_select.gameObject.SetActive(false);
        bg_null.gameObject.SetActive(false);
     
    }

    public void SetDataByOrigin(OCTheatreAvatarOc actorData, int origin)
    {
        EnsureInitialized();
        bool isMy = origin == 1 || origin == 2;
        _actorData = actorData;
        _currentClothesIndex = actorData.clothesIndex;
        txt_name.text = actorData.avatarName;
        bool hasUrl = !string.IsNullOrEmpty(actorData.avatarURL);
        r_image.gameObject.SetActive(hasUrl);
        img_def.gameObject.SetActive(!hasUrl && !isMy);
        img_my.gameObject.SetActive(!hasUrl && isMy);
        if (hasUrl)
            r_image.Load(actorData.avatarURL);
        bg_def.gameObject.SetActive(!isMy);
        bg_my.gameObject.SetActive(isMy);
        bg_select.gameObject.SetActive(false);
        bg_null.gameObject.SetActive(false);
    }

    public void SetMyActorData(OCTheatreAvatarOc actorData)
    {
        EnsureInitialized();
        _actorData = actorData;
        _currentClothesIndex = actorData.clothesIndex;
        txt_name.text = actorData.avatarName;
        bool hasUrl = !string.IsNullOrEmpty(actorData.avatarURL);
        r_image.gameObject.SetActive(hasUrl);
        img_my.gameObject.SetActive(!hasUrl);
        img_def.gameObject.SetActive(false);
        if (hasUrl)
            r_image.Load(actorData.avatarURL);
        bg_my.gameObject.SetActive(true);
        bg_def.gameObject.SetActive(false);
        bg_select.gameObject.SetActive(false);
        bg_null.gameObject.SetActive(false);
    }

    public void SetNullData()
    {
        EnsureInitialized();
        bg_null.gameObject.SetActive(true);
        bg_def.gameObject.SetActive(false);
        bg_my.gameObject.SetActive(false);
        bg_select.gameObject.SetActive(false);
        r_image.gameObject.SetActive(false);
        img_def.gameObject.SetActive(false);
        img_my.gameObject.SetActive(false);
       
    }

    public void ApplyClothes(OCTheatreAvatarOc actorData, OTCAvatarClothes clothes)
    {
        EnsureInitialized();
        bool wasSelected = bg_select.gameObject.activeSelf;
        _actorData = new OCTheatreAvatarOc
        {
            playerId = actorData.playerId,
            avatarName = actorData.avatarName,
            avatarURL = actorData.avatarURL,
            clothesIndex = clothes.clothesIndex
        };
        _currentClothesIndex = clothes.clothesIndex;
        txt_name.text = _actorData.avatarName;
        bool hasUrl = !string.IsNullOrEmpty(clothes.clothesURL);
        r_image.gameObject.SetActive(hasUrl);
        img_def.gameObject.SetActive(!hasUrl);
        img_my.gameObject.SetActive(false);
        if (hasUrl)
            r_image.Load(clothes.clothesURL);
        bg_def.gameObject.SetActive(true);
        bg_my.gameObject.SetActive(false);
        bg_select.gameObject.SetActive(wasSelected);
        bg_null.gameObject.SetActive(false);
    }

    public void SetWardrobeData(OTCAvatarClothes clothes)
    {
        EnsureInitialized();
        _currentClothesIndex = clothes.clothesIndex;
        txt_name.text = clothes.clothesName;
        bool hasUrl = !string.IsNullOrEmpty(clothes.clothesURL);
        r_image.gameObject.SetActive(hasUrl);
        img_def.gameObject.SetActive(!hasUrl);
        img_my.gameObject.SetActive(false);
        if (hasUrl)
            r_image.Load(clothes.clothesURL);
        bg_def.gameObject.SetActive(true);
        bg_my.gameObject.SetActive(false);
        bg_select.gameObject.SetActive(false);
        bg_null.gameObject.SetActive(false);
    }

    public void LoadClothesImage(string url, int clothesIndex = -1)
    {
        EnsureInitialized();
        if (clothesIndex >= 0) _currentClothesIndex = clothesIndex;
        bool hasUrl = !string.IsNullOrEmpty(url);
        r_image.gameObject.SetActive(hasUrl);
        img_def.gameObject.SetActive(!hasUrl);
        if (hasUrl) r_image.Load(url);
    }

    public void SetClickCallback(UnityEngine.Events.UnityAction callback)
    {
         var btn = GetComponent<CButton>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ItemSelectedOnClick);
        _onClickAction = callback;
    }

    public void SetSelected(bool isSelected)
    {
        EnsureInitialized();
        bg_select.gameObject.SetActive(isSelected);
    }

    public void ItemSelectedOnClick()
    {
        EnsureInitialized();
        //Debug.LogError("ItemSelectedOnClick " + txt_name.text);
        bg_select.gameObject.SetActive(true);
         _onClickAction?.Invoke();
    }
}
