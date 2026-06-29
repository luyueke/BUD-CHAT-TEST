using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.MapSetting;
using GameData;
using Pb.Base;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.Events;
using UnityEngine.U2D;

public class EmoteSelectPanel:BasePanel<EmoteSelectPanel>
{
   
    [SerializeField] private SpriteAtlas bgAtlas; 
    [SerializeField] private CButton doneBtn;
    [SerializeField] private CButton backBtn;
    [SerializeField] private EmoteSelectView emoteSelectView;
    [SerializeField] private Transform characterRoot; //3D形象根节点
    [SerializeField] private UIDragUtil dragUtil; //3D形象手势
    
    [Header("光照")]
    [SerializeField] private Color ambientSkyColor;
    [SerializeField] private Color ambientEquatorColor;
    [SerializeField] private Color ambientGroundColor;
    [SerializeField] private Color dirLight;
    [SerializeField] private float reflectionIntensity;
    [SerializeField] private float lightIntensity;
    private Color _ambientSkyColor;
    private Color _ambientEquatorColor;
    private Color _ambientGroundColor;
    private Color _dirLightColor;
    private float _reflectionIntensity;
    private float _lightIntensity;
    private LightShadows _shadow;
    
    
    private CharacterWrap characterWrap;
    private PlayerAnimationCtrl _playerAnimationCtrl;
    
    private List<string> legalEmotes;
    private HashSet<string> ownedSingles;
    private string idleEmo = "";

    public static string DefalutEmoName = "站立";
    public UnityEvent<string> DoneListener;
    
    
    public override void OnCreate()
    {
        base.OnCreate();
        InitRoleHandler();
       
        emoteSelectView.SetOnClick(OnSelectEmoteItem);
        emoteSelectView.DefalutName = DefalutEmoName;//默认第一个的名字

        ownedSingles = new HashSet<string>();

        backBtn.onClick.AddListener(OnBackBtnClick);
        doneBtn.onClick.AddListener(OnDoneBtnClick);

        RefreshEmoList();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0)
        {
            string emoteId = args[0] is string ? (string) args[0] : "";
            idleEmo = emoteId;
        }
        ShowDefault(idleEmo);
    }

    private void InitRoleHandler()
    {
        var avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        characterWrap.SetParent(characterRoot, true);
        dragUtil.RotateTarget = characterWrap.Avatar.transform;
        
        _playerAnimationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        
    }

    public void ShowDefault(string emoteId)
    {
        idleEmo = emoteId;
        RefreshEmoList();
    }

    
    private void RefreshEmoList()
    {
        legalEmotes = GetLegalEmotes();
        emoteSelectView.Refresh(legalEmotes);
        emoteSelectView.SetSingleSelect(idleEmo);
    }

    private List<string> GetLegalEmotes()
    {
        var emoteDataList = Es.DataTables.GetEmoUIConfigList().FindAll((emoData) => emoData.emoAniType == (int)EmoteType.SingleLoop);
        //TODO:待过滤自己拥有的表情

        List<string> legalEmote = new List<string>();
        legalEmote.Add("");//默认加入站立
        foreach (var config in emoteDataList)
        {
            legalEmote.Add(config.pgcId);
        }
        return legalEmote;
    }
    
    private void OnSelectEmoteItem(EmoteSelectItem item)
    {
        idleEmo = item.EmoteId;
        if (item.EmoteId == "")
        {
            _playerAnimationCtrl.ResetEmoteForUICharacter();
        }
        else
        {
            _playerAnimationCtrl.PlaySingleEmoteForUICharacter(idleEmo);
        }
    }


    private void OnDoneBtnClick()
    {
        DoneListener?.Invoke(idleEmo);
        CloseSelf();
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }
    
    #region 光照控制

    public void SetEnvColor()
    {
        _ambientSkyColor = RenderSettings.ambientSkyColor;
        _ambientEquatorColor = RenderSettings.ambientEquatorColor;
        _ambientGroundColor = RenderSettings.ambientGroundColor;
        _reflectionIntensity = RenderSettings.reflectionIntensity;

        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.reflectionIntensity = reflectionIntensity;


        var curLight = DirLightManager.Inst.GetGlobalDirLight();
        _dirLightColor = curLight.color;
        _lightIntensity = curLight.intensity;
        _shadow = curLight.shadows;

        curLight.color = dirLight;
        curLight.intensity = lightIntensity;
        curLight.shadows = LightShadows.None;
    }

    public void ResetEnvColor()
    {
        RenderSettings.ambientSkyColor = _ambientSkyColor;
        RenderSettings.ambientEquatorColor = _ambientEquatorColor;
        RenderSettings.ambientGroundColor = _ambientGroundColor;
        RenderSettings.reflectionIntensity = _reflectionIntensity;
        
        var curLight = DirLightManager.Inst.GetGlobalDirLight();
        curLight.color = _dirLightColor;
        curLight.intensity = _lightIntensity;
        curLight.shadows = _shadow;
    }

    #endregion
   
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    
    
    public override void OnHidden()
    {
        base.OnHidden();
        // loopPlayer?.Stop();
    }
}
