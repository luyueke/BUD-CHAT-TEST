using System;
using System.Collections;
using System.IO;
using Es;
using EventTracking;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;



public class TheatreCreaterActorPanel : BasePanel<TheatreCreaterActorPanel>
{
    private Button btn_Close;
    private Button btn_go_avate;
    private Button btn_set_def;
    public Toggle tog_materials;
    public Toggle tog_character;
    public Toggle tog_wardrobe;
    public Toggle tog_actor_card;

    public GameObject MaterialsView;
    public GameObject CharacterView;
    public GameObject WardrobeView;
    public GameObject ActorCardView;
    public GameObject ActorCardInfo;

    private CharacterWrap characterWrap;
    private bool _isNewActor;
    private OTCAvatarClothes _selectedClothes;
    [SerializeField] private AvatarCameraController cameraController;
    public Transform CharacterRoot;
    public GameObject AvatarRoot;
    

    public override void OnCreate()
    {
        base.OnCreate();
        CharacterView.gameObject.SetActive(false);
        WardrobeView.gameObject.SetActive(false);
        ActorCardView.gameObject.SetActive(false);
        
        ShowCharacter();
        
        btn_Close = transform.Find("btn_close").GetComponent<Button>();
        btn_Close.onClick.AddListener(SaveToDraftAndClose);
        
        btn_go_avate = transform.Find("btn_go_avate").GetComponent<Button>();
        btn_set_def = transform.Find("btn_set_def").GetComponent<Button>();
        btn_set_def.onClick.AddListener(() =>
        {
            if (_selectedClothes == null)
            {
                TipPanel.ShowToast("请先选择一套衣柜");
                return;
            }
            OCTheatreActorEditorDataManager.Inst.SetDefault(_selectedClothes);
            TipPanel.ShowToast("设置成功");
        });
        MessageHelper.AddListener<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, OnWardrobeItemSelected);
        MessageHelper.AddListener<OTCAvatarClothes>(MessageName.ActorWardrobeItemAdded, OnWardrobeItemAdded);
        btn_go_avate.onClick.AddListener(() =>
        {
            var selectedItem = WardrobeView.GetComponent<WardrobeView>().SelectedItem;
            UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            if (selectedItem != null)
                MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeViewOpenFittingRoomPanel, selectedItem);
            else
                MessageHelper.Broadcast<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeViewOpenFittingRoomPanel, null);
        });
        
        tog_materials.onValueChanged.AddListener((isOn =>
        {
            if (!isOn) return;
            CharacterView.SetActive(false);
            WardrobeView.SetActive(false);
            ActorCardView.SetActive(false);
            ActorCardInfo.SetActive(false);
            
            MaterialsView.SetActive(true);
            AvatarRoot.SetActive(true);
            btn_go_avate.gameObject.SetActive(false);
            btn_set_def.gameObject.SetActive(false);
            CharacterRoot.gameObject.SetActive(true);
        }));
        tog_character.onValueChanged.AddListener((isOn =>
        {
            if (!isOn) return;
            MaterialsView.SetActive(false);
            WardrobeView.SetActive(false);
            ActorCardView.SetActive(false);
            ActorCardInfo.SetActive(false);
            
            CharacterView.SetActive(true);
            AvatarRoot.SetActive(true);
            btn_go_avate.gameObject.SetActive(false);
            btn_set_def.gameObject.SetActive(false);
            CharacterRoot.gameObject.SetActive(true);
           
        }));
        tog_wardrobe.onValueChanged.AddListener((isOn =>
        {
            if (!isOn) return;
            
            MaterialsView.SetActive(false);
            CharacterView.SetActive(false);
            ActorCardView.SetActive(false);
            ActorCardInfo.SetActive(false);
            
            WardrobeView.SetActive(true);
            AvatarRoot.SetActive(true);
            btn_go_avate.gameObject.SetActive(true);
            btn_set_def.gameObject.SetActive(true);
            CharacterRoot.gameObject.SetActive(true);
        }));
        tog_actor_card.onValueChanged.AddListener((isOn =>
        {
            if(!isOn) return;
            MaterialsView.SetActive(false);
            CharacterView.SetActive(false);
            ActorCardView.SetActive(false);
            WardrobeView.SetActive(false);
            AvatarRoot.SetActive(false);
             btn_go_avate.gameObject.SetActive(false);
             btn_set_def.gameObject.SetActive(false);
            CharacterRoot.gameObject.SetActive(false);
            
            ActorCardView.SetActive(true);
            ActorCardInfo.SetActive(true);
        }));
        
        tog_materials.onValueChanged.Invoke(true);
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0 && args[0] is OCTheatreAvatarInfo existingActor)
        {
            OCTheatreActorEditorDataManager.Inst.SetCurActor(existingActor);
            _isNewActor = false;
        }
        else if (args != null && args.Length > 0)
        {
            // 有参数但不是已有演员，说明是新建演员
            _isNewActor = true;
            OCTheatreActorEditorDataManager.Inst.CreateNewActor();
        }
        // args 为空 = 从隐藏状态恢复（如 FittingRoomPanel 关闭后），不重置演员数据
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }

    private void OnWardrobeItemSelected(WardrobeViewWardrobelistItem item)
    {
        if (item != null)
        {
            _selectedClothes = item.ClothesData;
            ApplyClothesToCharacter(item.ClothesData.clothesJson);
        }
    }

    private void OnWardrobeItemAdded(OTCAvatarClothes clothes)
    {
        ApplyClothesToCharacter(clothes?.clothesJson);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<WardrobeViewWardrobelistItem>(MessageName.ActorWardrobeItemSelected, OnWardrobeItemSelected);
        MessageHelper.RemoveListener<OTCAvatarClothes>(MessageName.ActorWardrobeItemAdded, OnWardrobeItemAdded);
        if (characterWrap != null && characterWrap.Avatar != null)
        {
            Destroy(characterWrap.Avatar.gameObject);
            characterWrap = null;
        }
    }

    private void SaveToDraftAndClose()
    {
        var actor = OCTheatreActorEditorDataManager.Inst.GetCurActor();
        if (actor == null || string.IsNullOrEmpty(actor.cover))
        {
            CommonConfirmPanel confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            confirmPanel.SetLocalText("提示", "添加 -常态立绘- 后才能保存演员，确认不保存直接退出吗？", "退出", "取消");
            confirmPanel.SetOnClickAction(() => CloseSelf(), null);
            return;
        }

        
        var setType = _isNewActor ? (int)SetType.Create : (int)SetType.Edit;
        var req = new SetActorInfoReq
        {
            actorInfo = actor,
            setType = setType
        };
        
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActorSet, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            (_) =>
            {
                 TipPanel.ShowToast("草稿已保存");
                MessageHelper.Broadcast(MessageName.OnActorStudioDraftListChange);
                CloseSelf();
            },
            (arg0) => { 
                 // 处理失败响应
                    HttpResponseRawData httpResponseRawData = JsonConvert.DeserializeObject<HttpResponseRawData>(arg0);
                    if (httpResponseRawData == null)
                    {
                      
                        return;
                    }

                    // 显示错误提示
                    string rmsg = httpResponseRawData.rmsg;
                    TipPanel.ShowToast(rmsg);
                    CloseSelf();
                });
    }
  


    private void ShowCharacter()
    {
        var defaultClothes = OCTheatreActorEditorDataManager.Inst.GetDefaultClothes();
        CharacterData characterData = null;

        if (defaultClothes != null && !string.IsNullOrEmpty(defaultClothes.clothesJson))
            characterData = CharacterData.DeserializeObject(defaultClothes.clothesJson);

        if (characterData == null)
            characterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();

        if (characterData != null)
        {
            if (characterWrap != null && characterWrap.Avatar != null)
                Destroy(characterWrap.Avatar.gameObject);

            characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
            characterWrap.SetParent(CharacterRoot, true);
            cameraController.RotateTarget = CharacterRoot;
            cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
        }
    }

    private void ApplyClothesToCharacter(string clothesJson)
    {
        CharacterData characterData = null;
        if (!string.IsNullOrEmpty(clothesJson))
            characterData = CharacterData.DeserializeObject(clothesJson);
        if (characterData == null)
            characterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();

        if (characterWrap != null && characterWrap.Avatar != null)
            Destroy(characterWrap.Avatar.gameObject);

        characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
        characterWrap.SetParent(CharacterRoot, true);
        cameraController.RotateTarget = CharacterRoot;
        cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
    }
    
}
