using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using UI.Base;
using UI.BaseWidgets;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using GameData.PgcData;
using Message;

public class AIHospitalStorePanel : BasePanel<AIHospitalStorePanel>
{
    [SerializeField] private GameObject _item;
    [SerializeField] private CButton _closeBtn;

    [SerializeField] private GameObject _avatarParentObj;
    [SerializeField] private AvatarCameraController _clickArea;
    private CharacterWrap _characterWrap;
    private PlayerIdleBehaviour _idleBehaviour;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        AddListener();
        AccountDataManager.Inst.RequestAvatarFrameOrChat();
        InitUI();
    }

    private void InitUI()
    {
        var AllData = AIHospitalStoreManager.Inst.GetAllStoreItem();
        for (int i = 0; i < AllData.Count; i++)
        {
            var obj = Instantiate(_item,_item.transform.parent);
            obj.SetActive(true);
            if (AllData.TryGetValue(i+1,out AIHospitalStoreItemData data))
            {
                AIHospitalStoreGoodsItem goodsItem = obj.GetComponent<AIHospitalStoreGoodsItem>();
                goodsItem?.Init(data);
            }
        }
        _clickArea.RotateTarget = _avatarParentObj.transform;
    }

    private void AddListener()  
    {
        _closeBtn.onClick.AddListener(OnCloseBtnClick);
        MessageHelper.AddListener<AIHospitalStoreItemData>(MessageName.OnS9StoreItemClick, OnDrawAvatar);
    }

    private void RemoveListener()
    {
        _closeBtn.onClick.RemoveListener(OnCloseBtnClick);
        MessageHelper.RemoveListener<AIHospitalStoreItemData>(MessageName.OnS9StoreItemClick, OnDrawAvatar);
    }

    private void OnCloseBtnClick()
    {
        CloseSelf();
    }

    private void OnDrawAvatar(AIHospitalStoreItemData data)
    {
        if ((EGoodsType)data.nGoodsType == EGoodsType.Clothes)
        {
            InitCharacter();
            HideCharacter(false);
            WearClothes(AIHospitalStoreManager.Inst.GetClothesData(data.ID));
        }
        else
            HideCharacter(true);
    }

    #region 加载avatar
    public void InitCharacter()
    {
        if (_characterWrap != null)
        {
            return;
        }
        // 先隐藏当前显示的角色
        // HideCharacter();

        // 创建新角色
        PlayerAnimationCtrl avatarAnimCtrl = null;
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (avatarInfo != null)
        {
            //AvatarDataManager.Inst.SelfCharacterData = avatarInfo;

            if (_characterWrap == null)
            {
                var wrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo, _avatarParentObj.transform);
                _characterWrap = wrap;

                avatarAnimCtrl = wrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarAnimCtrl.CheckAndOverrideSpecialAnim();

                _idleBehaviour = wrap.Avatar.AddComponent<PlayerIdleBehaviour>();
                _idleBehaviour.Init(avatarAnimCtrl);

                var ugcBehaivour = wrap.Avatar.AddComponent<UgcIdleBehaviour>();
                var ikController = wrap.Avatar.GetComponent<AnimIKController>();
                ugcBehaivour.Init(ikController);
            }
            else
            {
                // 如果已经有角色，只更新数据
                _characterWrap.Avatar.gameObject.SetActive(true);
                // TODO: 更新角色数据的逻辑
            }

            LoggerUtils.Log($"成功加载/更新avatar");
        }
        else
        {
            LoggerUtils.LogError($"加载avatar失败，无效的数据");
        }
    }

    public void HideCharacter(bool value)
    {
        if (_characterWrap != null && _characterWrap.Avatar != null)
        {
            _characterWrap.Avatar.gameObject.SetActive(!value);
            LoggerUtils.Log("隐藏角色");
        }
    }

    public void DestroyCharacter()
    {
        if (_characterWrap != null)
        {
            // 清除引用的组件
            if (_idleBehaviour != null)
            {
                Destroy(_idleBehaviour);
                _idleBehaviour = null;
            }

            // 销毁整个角色包装器
            Destroy(_characterWrap.Avatar.gameObject);
            _characterWrap = null;

            LoggerUtils.Log("销毁角色");
        }
    }

    private void WearClothes(List<string> pgcIDList)
    {
        _characterWrap?.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo);
        foreach (var item in pgcIDList)
        {
            WearClothes(item);
        }
    }

    private void WearClothes(string pgcId)
    {
        //var characterWrap = AvatarController.Inst.SelfWrap;
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        _characterWrap.ChangePart(classType, pgcId);
        _characterWrap.ChangeColor(classType, config.defaultColor);
        _characterWrap.Move(classType, config.pDef);
        _characterWrap.Rotate(classType, config.rDef);
        _characterWrap.Scale(classType, config.sDef);
        _characterWrap.HVScale(classType, config.vhSDef);
        _characterWrap.SetLeftOrRight(classType, config.leftRightType);
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        RemoveListener();
        DestroyCharacter();
    }
}
