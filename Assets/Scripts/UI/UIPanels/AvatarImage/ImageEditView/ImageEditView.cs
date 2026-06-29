using System;
using System.Collections.Generic;
using EasySpreadsheet;
using Es;
using Game.Avatar;
using GameData.Base.Common;
using GameData.PgcData;
using Network.Http;
using Network;
using UI.BaseWidgets;
using UI.UIPanels.AvatarImage.ColorPicker;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

public class ImageEditView : MonoBehaviour
{
    /// <summary>
    /// 获取当前的人物形象数据
    /// </summary>
    public Func<CharacterData> GetCurrentCharterData;

    private CButton bodyBtn;
    private CButton faceBtn;
    private AvatarCategoryBar topBar;
    private AvatarType CurrentAvatarType = AvatarType.Body;

    private RoleColorView colorView;

    /// <summary>
    /// 点击通用Pgc素材
    /// </summary>
    public Action<AvatarMenuType, AvatarSubType, RoleActionType, AvatarRoleItemProtocol> ClickAction;

    /// <summary>
    /// 点击Pgc Skin颜色
    /// </summary>
    public Action<AvatarMenuType, RoleColorAction, string> ClickColorAction;

    public Action<AvatarSubType, RoleActionType, AvatarRoleItemProtocol> ClickBagAction;

    /// <summary>
    /// 点击设子形象
    /// </summary>
    public Action<OcActionType, AvatarOcData> ClickOcAction;

    public Action<AvatarMenuType> MenuDidChangeAction;

    public Action<EAdjustItemType, AvatarMenuType,AvatarCommonData, float> adjustAciton;

    public Action<AvatarSubType, string> changeColorAction;

    public Action<AvatarMenuType> switchHandAction;

    protected List<AdjustItemContext> mAdjustItemContexts = new List<AdjustItemContext>();

    /// <summary>
    /// 二级菜单view, key 为 AvatarMenuType.value
    /// </summary>
    private Dictionary<string, IAvatarRoleItem> roleViewDict = new Dictionary<string, IAvatarRoleItem>();

    private AvatarOcRoleView ocView;
    private AvatarPGCBagView bagView;

    private AvatarMenuType _avatarMenuType;
    private AvatarCommonData _avatarData;
    private string currentItemId;

    /// <summary>
    /// 是否为新用户
    /// </summary>
    private bool _isNewUser = true;

    private bool _isSetColor = false;

    public bool isNewUser
    {
        set
        {
            if (_isNewUser != value)
            {
                _isNewUser = value;
                OnClickAvatarType(CurrentAvatarType);
            }
        }
        get { return _isNewUser; }
    }

    private void Awake()
    {
        InitUI();

        AddListiner();

        OnClickAvatarType(AvatarType.Body);
    }

    private void InitUI()
    {
        bodyBtn = GameObjectEx.FindChildByName(transform, "LeftBar/Body").GetComponent<CButton>();
        faceBtn = GameObjectEx.FindChildByName(transform, "LeftBar/Face").GetComponent<CButton>();

        topBar = GameObjectEx.FindChildByName(transform, "RightView/TopBar").GetComponent<AvatarCategoryBar>();
        var bodyContent = GameObjectEx.FindChildByName(transform, "RightView/BodyContent").gameObject;

        IAvatarRoleItem RoleViewImp =
            GameObjectEx.FindChildByName(bodyContent, "RoleViewImp").GetComponent<IAvatarRoleItem>();

        var allTypes = System.Enum.GetValues(typeof(AvatarMenuType)) as AvatarMenuType[];

        foreach (var element in allTypes)
        {
            if (element == AvatarMenuType.Bag)
            {
                bagView = GameObjectEx.FindChildByName(bodyContent, "BagView").GetComponent<AvatarPGCBagView>();
                bagView.RegisterListener(OnClickBagItem,OnChangeColor);
                continue;
            }
            else if (element == AvatarMenuType.PrefabImage)
            {
                ocView = GameObjectEx.FindChildByName(bodyContent, "OcView").GetComponent<AvatarOcRoleView>();
                ocView.RegisterListener(OnClickOcItem);
                continue;
            }

            IAvatarRoleItem view;
            if (element == AvatarMenuType.Outfit)
            {
                view = GameObjectEx.FindChildByName(bodyContent, "OutfitView").GetComponent<AvatarOutfitView>();
            }
            else if (element == AvatarMenuType.Collect)
            {
                view = GameObjectEx.FindChildByName(bodyContent, "CollectView").GetComponent<AvatarCollectView>();
            }
            else
            {
                view = GameObject.Instantiate(RoleViewImp, bodyContent.transform);
            }

            string key = System.Enum.GetName(typeof(AvatarMenuType), element);
            view.SetData(element);
            view.name = key;
            view.gameObject.SetActive(false);
            view.RegisterListener(OnClickItem, OnChangeColor, OnShowPaltte, OnShowHsv, OnClickColor);
            roleViewDict[key] = view;
        }
    }
    
    
    public void SetSubTabVisible()
    {
        string outfit = System.Enum.GetName(typeof(AvatarMenuType), AvatarMenuType.Outfit);
        if (roleViewDict.ContainsKey(outfit))
        {
            AvatarOutfitView outfitView = roleViewDict[outfit] as AvatarOutfitView;
            outfitView.SetNewUser(isNewUser);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isShow"></param>
    private void BagShowOrHide(bool isShow)
    {
        bagView?.ShowListView(!isShow);
    }


    /// <summary>
    /// 里面调整面板点击返回按钮
    /// </summary>
    /// <param name="isShow"></param>
    private void OnShowOrHide(bool isShow)
    {
        string key = System.Enum.GetName(typeof(AvatarMenuType), _avatarMenuType);
        var editView = roleViewDict[key];
        if (_isSetColor)
        {
            //关闭color view而且需要显示列表
            editView.ShowListView(!isShow);
        }
        else
        {
            editView.ShowListView(!isShow);
        }
    }


    /// <summary>
    /// 里面颜色面板点击返回按钮
    /// </summary>
    /// <param name="isShow"></param>
    private void OnShowPaltte(bool isShow)
    {
        string key = System.Enum.GetName(typeof(AvatarMenuType), _avatarMenuType);
        if (roleViewDict.ContainsKey(key))
        {
            var editView = roleViewDict[key];
            if (_isSetColor)
            {
                editView.ShowListView(false);
            }
            else
            {
                editView.ShowListView(!isShow);
            }
        }
        
    }

    /// <summary>
    /// 里面HSV面板点击返回按钮
    /// </summary>
    /// <param name="isShow"></param>
    private void OnShowHsv(bool isShow)
    {
        string key = System.Enum.GetName(typeof(AvatarMenuType), _avatarMenuType);
        var editView = roleViewDict[key];
        if (_avatarMenuType == AvatarMenuType.Skin)
        {
            //皮肤颜色hsv面板返回需要隐藏colorbar
            editView.ShowColorBar(false);
        }

        if (_isSetColor)
        {
            editView.ShowListView(false);
        }
        else
        {
            editView.ShowListView(!isShow);
        }
    }

    /// <summary>
    /// 调整数据变动回调
    /// </summary>
    /// <param name="itemType"></param>
    /// <param name="value"></param>
    public void AdjustItemValueChanged(EAdjustItemType itemType, float value)
    {
        adjustAciton.Invoke(itemType, _avatarMenuType, _avatarData,
            value);
    }

    private IAvatarRoleItem RoleView(AvatarMenuType type)
    {
        if (type == AvatarMenuType.Bag)
        {
            return null;
        }
        else if (type == AvatarMenuType.PrefabImage)
        {
            return null;
        }

        string key = System.Enum.GetName(typeof(AvatarMenuType), type);
        return roleViewDict[key];
    }

    /// <summary>
    /// 更新人物形象数据
    /// </summary>
    /// <param name="characterData"></param>
    public void UpdateCharacterSelected(CharacterData characterData)
    {
        UpdateSelectedItem(AvatarMenuType.Hair,characterData);
        UpdateSelectedItem(AvatarMenuType.Eyebrow,characterData);
        UpdateSelectedItem(AvatarMenuType.Eye,characterData);
        UpdateSelectedItem(AvatarMenuType.Hand,characterData);
        UpdateSelectedItem(AvatarMenuType.Glove,characterData);
        UpdateSelectedItem(AvatarMenuType.Mouth,characterData);
        UpdateSelectedItem(AvatarMenuType.Shoe,characterData);
        UpdateSelectedItem(AvatarMenuType.Headwear,characterData);
        UpdateSelectedItem(AvatarMenuType.Scarf,characterData);
        UpdateSelectedItem(AvatarMenuType.Nose,characterData);
        UpdateSelectedItem(AvatarMenuType.Blush,characterData);
        UpdateSelectedItem(AvatarMenuType.Glasses,characterData);
        UpdateSelectedItem(AvatarMenuType.Visor,characterData);
        UpdateSelectedItem(AvatarMenuType.FacePainting,characterData);

        var clothesId = characterData.GetPartData(UniqueType.GetAvatar(AvatarSubType.Clothes))?.Id;
        if (string.IsNullOrEmpty(clothesId) || clothesId == "0")
        {
            clothesId = characterData.GetPartData(UniqueType.GetUgcAvatar(AvatarSubType.Clothes))?.UId;
        }
        RoleView(AvatarMenuType.Outfit)?.UpdateSelectedItem(clothesId);

        var partEnum = AvatarConfigTool.PartType(AvatarMenuType.Hair);
        var Cr = characterData.GetPartData(UniqueType.GetAvatar(partEnum))?.Cr;
        RoleView(AvatarMenuType.Skin)?.UpdateSelectedItem(Cr);

        
        var bId = characterData.GetPartData(UniqueType.GetAvatar(AvatarSubType.Backpack))?.Id;
        var cId = characterData.GetPartData(UniqueType.GetAvatar(AvatarSubType.Cape))?.Id;
        var cbId = characterData.GetPartData(UniqueType.GetAvatar(AvatarSubType.Crossbody))?.Id;
        bagView.UpdateSelectedItem(bId, cId, cbId);
        
    }

    public void UpdateSelectedItem(AvatarMenuType menuType,CharacterData characterData)
    {
        var partEnum = AvatarConfigTool.PartType(menuType);
        var id = characterData.GetPartData(UniqueType.GetAvatar(partEnum))?.Id;
        RoleView(menuType)?.UpdateSelectedItem(id);
    }





    /// <summary>
    /// 重置按钮点击
    /// </summary>
    /// <param name="id"></param>
    public void OnAdjustViewResetCallBack()
    {
        ResetAdjustValue();
    }

    /// <summary>
    /// 重置调整状态值
    /// </summary>
    private void ResetAdjustValue()
    {
        if (_avatarData != null)
        {
            if (_avatarMenuType == AvatarMenuType.Bag)
            {
                AvatarSubType partType = AvatarConfigTool.PartType(_avatarMenuType);
                SetAdjustView2Normal(bagView.adjustViewWithColor, _avatarData, partType);
            }
            else
            {
                var editView = RoleView(_avatarMenuType);
                if (editView != null)
                {
                    AvatarSubType partType = (AvatarSubType)_avatarData.SubType;
                    SetAdjustView2Normal(editView.adjustViewWithColor, _avatarData, partType);
                }
            }
        }
    }

    private void AddListiner()
    {
        bodyBtn.onClick.AddListener(() =>
        {
            if (CurrentAvatarType != AvatarType.Body)
            {
                OnClickAvatarType(AvatarType.Body);
            }
        });

        faceBtn.onClick.AddListener(() =>
        {
            if (CurrentAvatarType != AvatarType.Face)
            {
                OnClickAvatarType(AvatarType.Face);
            }
        });
    }

    private void OnClickAvatarType(AvatarType type)
    {
        CurrentAvatarType = type;

        GameObjectEx.FindChildByName(bodyBtn.gameObject, "Selected").gameObject.SetActive(type == AvatarType.Body);
        GameObjectEx.FindChildByName(faceBtn.gameObject, "Selected").gameObject.SetActive(type == AvatarType.Face);

        topBar.SetAvatarData(type, isNewUser, OnClickSecondMenu);
    }

    public void SetDefaultMenu(AvatarMenuType type, AvatarSubType? part)
    {
        topBar.SetDefaultMenu(type);

        if (part != null)
        {
            var editView = RoleView(type);
            if (editView != null)
            {
                editView.SetDefaultTab((AvatarSubType)part);
            }
        }
    }

    private void OnClickSecondMenu(AvatarCategoryData data)
    {
        MenuDidChangeAction?.Invoke(data.CurrentMenuType);
        string key = System.Enum.GetName(typeof(AvatarMenuType), data.CurrentMenuType);
        foreach (var avatarPgcRoleView in roleViewDict)
        {
            bool isShow = avatarPgcRoleView.Key == key;
            avatarPgcRoleView.Value.gameObject.SetActive(isShow);
        }

        bagView.gameObject.SetActive(data.CurrentMenuType == AvatarMenuType.Bag);
        ocView.gameObject.SetActive(data.CurrentMenuType == AvatarMenuType.PrefabImage);
        var editView = RoleView(data.CurrentMenuType);
        bool isSupportChangeColor = AdjustViewUtils.IsSupportChangeColor(data.CurrentMenuType);
        if (isSupportChangeColor)
        {
            editView?.adjustViewWithColor.gameObject.SetActive(isSupportChangeColor);

            if (data.CurrentMenuType == AvatarMenuType.Blush)
            {
                editView?.adjustViewWithColor.ShowOnlyCommonColor();
            }
        }

        _avatarMenuType = data.CurrentMenuType;

        if (editView != null)
        {
            ColorViewUtils.ReloadColorList(data.CurrentMenuType, editView);
        }
    }

    private void OnChangeColor(string color)
    {
        var partType = AvatarConfigTool.PartType(_avatarMenuType);
        changeColorAction?.Invoke(partType, color);
    }

    private void OnClickColor(AvatarMenuType menuType, RoleColorAction actionType, string itemId)
    {
        if (menuType == AvatarMenuType.Skin)
        {
            if (actionType == RoleColorAction.Palette)
            {
                var charterData = GetCurrentCharterData();
                if (charterData == null)
                {
                    LoggerUtils.LogError("not found CharacterData, please check");
                }

                var placeHolders = AvatarConfigTool.ConvertPlaceholderDatas(charterData);
                var partType = AvatarConfigTool.PartType(menuType);
                RolePlaceholderData placeHolderData = placeHolders.Find(x => x.SubType == partType);
                //显示hsv调色面版
                var editView = RoleView(menuType);
                if (editView != null)
                {
                    editView.ShowColorBar(true);
                    editView.ShowListView(false);
                    editView.adjustViewWithColor.ShowHsvView(placeHolderData.color);
                }

                return;
            }
        }

        ClickColorAction?.Invoke(menuType, actionType, itemId);
    }

    public void RefreshOcView()
    {
        ocView.RefreshData();
    }

    private void OnClickOcItem(OcActionType type, AvatarOcData data)
    {
        ClickOcAction?.Invoke(type, data);

        var charterData = GetCurrentCharterData();
        UpdateCharacterSelected(charterData);
    }

    private void OnClickBagItem(AvatarSubType partType, RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        if (actionType == RoleActionType.Adjust)
        {
            //点击背包调整
            _isSetColor = false;
            bagView.adjustViewWithColor.gameObject.SetActive(true);
            bagView.adjustViewWithColor.ShowHand(false);

            var bagData = Es.DataTables.GetAvatarCommonData(itemData.itemId);
            //是否可调整颜色
            _isSetColor = bagData.setColor;
            bagView.SetColor(_isSetColor);
            if (_isSetColor)
            {
                bagView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
            }
            else
            {
                bagView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
            }
       
            bagView.ReloadColorList(AvatarColorManager.BAG_ALL, AvatarColorManager.BAG_COM);

            var charterData = GetCurrentCharterData();
            if (charterData == null)
            {
                LoggerUtils.LogError("not found CharacterData, please check");
            }

            var placeHolders = AvatarConfigTool.ConvertPlaceholderDatas(charterData);
            RolePlaceholderData placeHolderData = placeHolders.Find(x => x.SubType == partType);
            // AdjustViewUtils.CommonAdjustData commonAdjustData =
            //     AdjustViewUtils.ConvertAvatarDataToCommonAdjustData(rowData);
            var commonAdjustData = Es.DataTables.GetAvatarCommonData(itemData.itemId);
            int adjustType = commonAdjustData.adjustType;
            AdjustViewUtils.AdjustType2AdjustItems(adjustType, mAdjustItemContexts);
            bagView.adjustViewWithColor.Init(mAdjustItemContexts, AdjustItemValueChanged,
                OnAdjustViewResetCallBack, BagShowOrHide, OnShowPaltte, OnShowHsv, OnSwitchHand);
            _avatarData = commonAdjustData;
            //优先使用角色数据，如果角色数据为空则使用表数据
            if (placeHolderData != null && placeHolderData.sDef != null && placeHolderData.rDef != null &&
                placeHolderData.pDef != null)
            {
                SetAdjustView2RoleData(bagView.adjustViewWithColor, commonAdjustData, placeHolderData);
            }
            else
            {
                SetAdjustView2Normal(bagView.adjustViewWithColor, commonAdjustData, partType);
            }

            bagView.ShowListView(false);
        }
        else if (actionType == RoleActionType.Item)
        {
            //记住当前选中的Item,重复选中不执行重置逻辑
            if (currentItemId != itemData.itemId)
            {
                currentItemId = itemData.itemId;
                var commonAdjustData = Es.DataTables.GetAvatarCommonData(currentItemId);
                if (commonAdjustData != null)
                {
                    int adjustType = commonAdjustData.adjustType;
                    AdjustViewUtils.AdjustType2AdjustItems(adjustType, mAdjustItemContexts);
                    bagView.adjustViewWithColor.Init(mAdjustItemContexts, AdjustItemValueChanged,
                        OnAdjustViewResetCallBack, OnShowOrHide, OnShowPaltte, OnShowHsv, OnSwitchHand);
                    _avatarData = commonAdjustData;
                    ResetAdjustValue();
                }

                ClickBagAction?.Invoke(partType, actionType, itemData);
            }

        }
        else if (actionType == RoleActionType.Collect)
        {
            CollectItem(AvatarMenuType.Bag, actionType, itemData);
        }
        else if (actionType == RoleActionType.Delete)
        {
            ClickBagAction?.Invoke(partType, actionType, itemData);
        }
    }

    private void OnClick(AvatarMenuType menuType, RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        if (actionType == RoleActionType.Adjust)
        {
            //点击调整
            _isSetColor = false;
            string key = System.Enum.GetName(typeof(AvatarMenuType), menuType);
            var editView = RoleView(menuType);
            if (editView == null)
            {
                return;
            }

            if (menuType == AvatarMenuType.Collect)
            {
                AvatarSubType part;
                if (itemData.isPGCItem)
                {
                    part = (AvatarSubType)Es.DataTables.GetGameResData(itemData.itemId).SubType;
                }
                else
                {
                    part = (AvatarSubType)Es.DataTables.GetGameResData(itemData.templateId).SubType;
                }

                menuType = AvatarConfigTool.MenuType(part);
            }

            editView.adjustViewWithColor.gameObject.SetActive(true);
            editView.adjustViewWithColor.ShowHand(false);
            editView.ShowColorBar(true);
            AvatarCommonData rowData = DataTables.GetAvatarCommonData(itemData.itemId);
            switch (menuType)
            {
                case AvatarMenuType.Eyebrow:
                    {
                        editView.ShowColorView(true);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
                    }
                    break;
                case AvatarMenuType.Nose:
                {
                    editView.ShowColorView(false);
                    editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                }
                    break;
                case AvatarMenuType.Blush:
                {
                    editView.ShowColorView(true);
                    editView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
                    editView.adjustViewWithColor.ShowOnlyCommonColor();
                }
                    break;
                case AvatarMenuType.Eye:
                    {
                        editView.ShowColorView(false);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                    }
                    break;
                case AvatarMenuType.Mouth:
                    {
                        editView.ShowColorView(false);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                    }
                    break;
                case AvatarMenuType.Headwear:
                    {
                        // editView.ShowColorView(false);
                        // rowData = Es.DataTables.GetAvatarDataHats(itemId);
                        // editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);

                        //部分帽子可以调整颜色
                        //是否可调整颜色
                        _isSetColor = rowData.setColor;
                        editView.SetColor(_isSetColor);
                        if (_isSetColor)
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
                        }
                        else
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                        }
                    }
                    break;
                case AvatarMenuType.Earrings:
                    {
                        // editView.ShowColorView(false);
                        // rowData = Es.DataTables.GetAvatarDataEar(itemId);
                        // editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);

                        //是否可调整颜色
                        _isSetColor = rowData.setColor;
                        editView.SetColor(_isSetColor);
                        if (_isSetColor)
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
                        }
                        else
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                        }
                    }
                    break;
                case AvatarMenuType.Visor:
                case AvatarMenuType.Glasses:
                    {
                        //部分眼镜可以调整颜色
                        var glassData = Es.DataTables.GetAvatarCommonData(itemData.itemId);
                        //是否可调整颜色
                        _isSetColor = rowData.setColor;
                        editView.SetColor(_isSetColor);
                        if (_isSetColor)
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
                        }
                        else
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                        }
                    }
                    break;
                case AvatarMenuType.Bag:
                    {
                        // editView.ShowColorView(false);
                        // editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                        //部分眼镜可以调整颜色
                        var bagData = Es.DataTables.GetAvatarCommonData(itemData.itemId);
                        //是否可调整颜色
                        _isSetColor = rowData.setColor;
                        editView.SetColor(_isSetColor);
                        if (_isSetColor)
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.AdjustAndColor);
                        }
                        else
                        {
                            editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                        }
                    }
                    break;
                case AvatarMenuType.Glove:
                case AvatarMenuType.Hand:
                    {
                        editView.ShowColorView(false);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                        int leftRightType = rowData.leftRightType;
                        editView.adjustViewWithColor.ShowHand(leftRightType != (int)AdjustViewUtils.LeftRightType.BothHand);
                    }
                    break;
                case AvatarMenuType.FacePainting:
                    {
                        editView.ShowColorView(false);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                    }
                    break;
                case AvatarMenuType.Effects:
                    {
                        editView.ShowColorView(false);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                    }
                    break;
                case AvatarMenuType.Belt:
                    {
                        editView.ShowColorView(false);
                        editView.adjustViewWithColor.ShowView(RoleAdjustType.Adjust);
                    }
                    break;
                
            }

            var charterData = GetCurrentCharterData();
            if (charterData == null)
            {
                LoggerUtils.LogError("not found CharacterData, please check");
            }

            var placeHolders = AvatarConfigTool.ConvertPlaceholderDatas(charterData);
            var partType = AvatarConfigTool.PartType(menuType);
            RolePlaceholderData placeHolderData = placeHolders.Find(x => x.SubType == partType);
            int adjustType = rowData.adjustType;
            AdjustViewUtils.AdjustType2AdjustItems(adjustType, mAdjustItemContexts);
            editView.adjustViewWithColor.Init(mAdjustItemContexts, AdjustItemValueChanged,
                OnAdjustViewResetCallBack, OnShowOrHide, OnShowPaltte, OnShowHsv, OnSwitchHand);
            _avatarData = rowData;
            //优先使用角色数据，如果角色数据为空则使用表数据
            if (placeHolderData != null && placeHolderData.sDef != null && placeHolderData.rDef != null &&
                placeHolderData.pDef != null)
            {
                SetAdjustView2RoleData(editView.adjustViewWithColor, rowData, placeHolderData);
            }
            else
            {
                SetAdjustView2Normal(editView.adjustViewWithColor, rowData, partType);
            }

            editView.ShowListView(false);
        }
        else if (actionType == RoleActionType.Item)
        {
            //记住当前选中的Item,重复选中不执行重置逻辑
            if (currentItemId != itemData.itemId)
            {
                currentItemId = itemData.itemId;
                var editView = RoleView(menuType);
                if (editView != null)
                {
                    var commonAdjustData = Es.DataTables.GetAvatarCommonData(itemData.itemId);
                    // AdjustViewUtils.CommonAdjustData commonAdjustData =
                    //     AdjustViewUtils.ConvertAvatarDataToCommonAdjustData(AdjustViewUtils.GetEsRowData(menuType, itemId));
                    if (commonAdjustData != null)
                    {
                        int adjustType = commonAdjustData.adjustType;
                        AdjustViewUtils.AdjustType2AdjustItems(adjustType, mAdjustItemContexts);
                        editView.adjustViewWithColor.Init(mAdjustItemContexts, AdjustItemValueChanged,
                            OnAdjustViewResetCallBack, OnShowOrHide, OnShowPaltte, OnShowHsv, OnSwitchHand);
                        _avatarData = commonAdjustData;
                        ResetAdjustValue();
                    }
                }

                if (menuType == AvatarMenuType.Collect)
                {
                    var charterData = GetCurrentCharterData();
                    UpdateCharacterSelected(charterData);
                }
            }
        }
        else if (actionType == RoleActionType.Collect)
        {
            CollectItem(menuType, actionType, itemData);
        }
    }

    /// <summary>
    /// 收藏
    /// </summary>
    /// <param name="actionType"></param>
    /// <param name="itemData"></param>
    private void CollectItem(AvatarMenuType menuType, RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        if(itemData?.itemId == null)
        {
            return;
        }
        int isPgc = itemData.isPGCItem ? 1 : 0;

        var req = new CollectRequest
        {
            id = itemData.itemId,
            setType = itemData.isCollected ? 2 : 1,
            isPgc = isPgc
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setCollect, HttpMethod.POST, JsonConvert.SerializeObject(req), response =>
        {
            bool isCollect = !itemData.isCollected;

            if (menuType == AvatarMenuType.Bag)
            {
                bagView.UpdateCollect(itemData.itemId, isCollect);
            }
            else
            {
                var editView = RoleView(menuType);
                editView.UpdateCollect(itemData, isCollect);
            }

            if (menuType == AvatarMenuType.Collect)
            {
                // 刷新相应栏目上的收藏icon
                UpdateAllCollected(itemData, isCollect);
            }
            else
            {
                // 刷新colloect界面列表
                var collectView = RoleView(AvatarMenuType.Collect);
                collectView.UpdateCollect(itemData, isCollect);
            }
        }, fail =>
        {
            LoggerUtils.LogError($"收藏商品失败 [{itemData.itemId}]:" + fail);
        });
    }

    private AvatarMenuType[] CollectedMenuArray = new AvatarMenuType[] { AvatarMenuType.Hair, AvatarMenuType.Outfit, AvatarMenuType.Headwear, AvatarMenuType.Glasses, AvatarMenuType.Earrings, AvatarMenuType.Shoe, AvatarMenuType.Scarf, AvatarMenuType.Hand, AvatarMenuType.Effects, AvatarMenuType.Belt, AvatarMenuType.Eye, AvatarMenuType.Eyebrow, AvatarMenuType.Nose, AvatarMenuType.Mouth, AvatarMenuType.Blush, AvatarMenuType.FacePainting, AvatarMenuType.Bag };

    private void UpdateAllCollected(AvatarRoleItemProtocol itemData, bool isCollect)
    {
        for (int i = 0; i < CollectedMenuArray.Length; i++)
        {
            UpdateCollectStatus(CollectedMenuArray[i], itemData, isCollect);
        }
    }

    private void UpdateCollectStatus(AvatarMenuType menuType, AvatarRoleItemProtocol itemData, bool isCollect)
    {
        if (menuType == AvatarMenuType.Bag)
        {
            bagView.UpdateCollect(itemData.itemId, isCollect);
        }
        else
        {
            var editView = RoleView(menuType);

            if (editView != null)
            {
                editView.UpdateCollect(itemData, isCollect);
            }
        }
    }

    private void OnClickItem(AvatarMenuType menuType, AvatarSubType part, RoleActionType actionType, AvatarRoleItemProtocol itemData)
    {
        ClickAction?.Invoke(menuType, part, actionType, itemData);
        OnClick(menuType, actionType, itemData);
    }

    private void OnSwitchHand()
    {
        switchHandAction?.Invoke(_avatarMenuType);
    }

    public void SetAdjustView2RoleData(RoleAdjustView adjustView, AvatarCommonData data,
        RolePlaceholderData rolePlaceholderData)
    {
        if (data.scaLimit != null && rolePlaceholderData.sDef != null && data.scaLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Size,
                AdjustViewUtils.GetSliderValue(data.scaLimit, rolePlaceholderData.sDef));
        }

        if (data.hLimit != null && rolePlaceholderData.pDef != null && data.hLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Spacing,
                AdjustViewUtils.GetSliderValue(data.hLimit, rolePlaceholderData.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Spacing, rolePlaceholderData.SubType)));
        }

        if (data.hLimit != null && rolePlaceholderData.pDef != null && data.hLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Left_right,
                AdjustViewUtils.GetSliderValue(data.hLimit, rolePlaceholderData.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Left_right, rolePlaceholderData.SubType)));
        }

        if (data.vLimit != null && rolePlaceholderData.pDef != null && data.vLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Up_down,
                AdjustViewUtils.GetSliderValue(data.vLimit, rolePlaceholderData.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Up_down, rolePlaceholderData.SubType)));
        }

        if (data.fLimit != null && rolePlaceholderData.pDef != null && data.fLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Front_back,
                AdjustViewUtils.GetSliderValue(data.fLimit, rolePlaceholderData.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Front_back, rolePlaceholderData.SubType)));
        }

        if (data.rotateLimit != null && rolePlaceholderData.rDef != null && data.rotateLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Rotation,
                AdjustViewUtils.GetSliderValue(data.rotateLimit, rolePlaceholderData.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Rotation, rolePlaceholderData.SubType)));
        }

        if (data.xrotLimit != null && rolePlaceholderData.rDef != null && data.xrotLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.X_Rotation,
                AdjustViewUtils.GetSliderValue(data.xrotLimit, rolePlaceholderData.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.X_Rotation, rolePlaceholderData.SubType)));
        }

        if (data.yrotLimit != null && rolePlaceholderData.rDef != null && data.yrotLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Y_Rotation,
                AdjustViewUtils.GetSliderValue(data.yrotLimit, rolePlaceholderData.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Y_Rotation, rolePlaceholderData.SubType)));
        }

        if (data.zrotLimit != null && rolePlaceholderData.rDef != null && data.zrotLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Z_Rotation,
                AdjustViewUtils.GetSliderValue(data.zrotLimit, rolePlaceholderData.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Z_Rotation, rolePlaceholderData.SubType)));
        }
        
        if (data.hScaLimit != null && rolePlaceholderData.vhSDef != null &&data.hScaLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.HorizontalStretch,
                AdjustViewUtils.GetSliderValue(data.hScaLimit, rolePlaceholderData.vhSDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.HorizontalStretch, rolePlaceholderData.SubType)));
        }
        
        if (data.vScaLimit != null && rolePlaceholderData.vhSDef != null&&data.vScaLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.VerticalStretch,
                AdjustViewUtils.GetSliderValue(data.vScaLimit, rolePlaceholderData.vhSDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.VerticalStretch, rolePlaceholderData.SubType)));
        }
    }

    public void SetAdjustView2Normal(RoleAdjustView adjustView, AvatarCommonData data,
        AvatarSubType avatarSubType)
    {
        if (data.scaLimit != null && data.sDef != null && data.scaLimit != null && data.scaLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Size,
                AdjustViewUtils.GetSliderValue(data.scaLimit, data.sDef));
        }

        if (data.hLimit != null && data.pDef != null && data.hLimit != null && data.hLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Spacing,
                AdjustViewUtils.GetSliderValue(data.hLimit, data.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Spacing, avatarSubType)));
        }

        if (data.hLimit != null && data.pDef != null && data.hLimit != null && data.hLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Left_right,
                AdjustViewUtils.GetSliderValue(data.hLimit, data.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Left_right, avatarSubType)));
        }


        if (data.vLimit != null && data.pDef != null && data.vLimit != null && data.vLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Up_down,
                AdjustViewUtils.GetSliderValue(data.vLimit, data.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Up_down, avatarSubType)));
        }

        if (data.fLimit != null && data.pDef != null && data.fLimit != null && data.fLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Front_back,
                AdjustViewUtils.GetSliderValue(data.fLimit, data.pDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Front_back, avatarSubType)));
        }

        if (data.rotateLimit != null && data.rDef != null && data.rotateLimit != null && data.rotateLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Rotation,
                AdjustViewUtils.GetSliderValue(data.rotateLimit, data.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Rotation, avatarSubType)));
        }

        if (data.xrotLimit != null && data.rDef != null && data.xrotLimit != null && data.xrotLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.X_Rotation,
                AdjustViewUtils.GetSliderValue(data.xrotLimit, data.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.X_Rotation, avatarSubType)));
        }

        if (data.yrotLimit != null && data.rDef != null && data.yrotLimit != null && data.yrotLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Y_Rotation,
                AdjustViewUtils.GetSliderValue(data.yrotLimit, data.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Y_Rotation, avatarSubType)));
        }

        if (data.zrotLimit != null && data.rDef != null && data.zrotLimit != null && data.zrotLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.Z_Rotation,
                AdjustViewUtils.GetSliderValue(data.zrotLimit, data.rDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.Z_Rotation, avatarSubType)));
        }
        
        if (data.hScaLimit != null && data.vhSDef != null && data.hScaLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.HorizontalStretch,
                AdjustViewUtils.GetSliderValue(data.hScaLimit, data.vhSDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.HorizontalStretch, avatarSubType)));
        }
        
        if (data.vScaLimit != null && data.vhSDef != null && data.vScaLimit.Count != 0)
        {
            adjustView.SetSliderValue(EAdjustItemType.VerticalStretch,
                AdjustViewUtils.GetSliderValue(data.vScaLimit, data.vhSDef,
                    AdjustViewUtils.GetVecAxis(EAdjustItemType.VerticalStretch, avatarSubType)));
        }
    }
}