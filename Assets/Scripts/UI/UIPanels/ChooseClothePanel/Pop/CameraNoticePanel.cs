using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;

/// <summary>
/// 相册艾特
/// </summary>
public class CameraNoticePanel : BasePanel<CameraNoticePanel>
{
    public CButton CloseBtn;

    public CameraNoticeAdpter Adpter;

    public PullToRefreshBehaviour PullToRefreshBehaviour;

    public CButton Btn_InputGameName;
    public InputField Txt_InputName;
    public CButton EnterBtn;

    private KeyBoardInfo _nameKBInfo;

    private readonly List<CameraNoticeCellData> datas = new List<CameraNoticeCellData>();
    private readonly MyFriendsListReqQuerry _friendListReq = new MyFriendsListReqQuerry();
    private readonly SearchFriendParams _searchFriendReq = new SearchFriendParams();
    private string _cookie = "";
    private bool _isEnd;
    private string _searchWord = "";
    private readonly List<atListItem> _selectedUids = new List<atListItem>();
    private CameraImagePack _packInfo;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(OnCloseBtnClick);
        PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
        Adpter.OnItemSelected = OnItemSelected;
        Adpter.Data = new LazyDataHelper<CameraNoticeCellData>(Adpter, GetInfo);

        Btn_InputGameName.onClick.AddListener(OnBtnInputGameNameClick);
        EnterBtn.onClick.AddListener(OnEnterBtnClick);

        _nameKBInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "请输入名称",
            inputMode = 0,
            maxLength = 7,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        _selectedUids.Clear();
        if (args != null && args.Length > 0)
        {
            _packInfo = args[0] as CameraImagePack;
            if (_packInfo.atList != null && _packInfo.atList.Count > 0)
            {
                for (int i = 0; i < _packInfo.atList.Count; i++)
                {
                    _selectedUids.Add(_packInfo.atList[i]);
                }
            }
        }

        RequestFirstPageFriendList();
    }

    private CameraNoticeCellData GetInfo(int index)
    {
        return datas[index];
    }

    private void OnItemSelected(object info)
    {
        if (info is CameraNoticeCellData cellData && cellData.friendInfo != null && cellData.friendInfo.userInfo != null)
        {
            var userInfo = cellData.friendInfo.userInfo;
            atListItem atItem = new atListItem();
            atItem.uid = userInfo.uid;
            atItem.username = userInfo.nickname;
            if (cellData.isSelected)
            {
                _selectedUids.Add(atItem);
            }
            else
            {
                foreach(var item in _selectedUids)
                {
                    if(item.uid == atItem.uid)
                    {
                        _selectedUids.Remove(item);
                        break;
                    }
                }
            }

            if (cellData.isSelected)
            {
                Txt_InputName.text = cellData.friendInfo.userInfo.nickname;
            }
        }

    }

    private void OnPullRefresh()
    {
        RequestNextPageFriendList();
    }

    private void OnEnterBtnClick()
    {
        CloseSelf();
        if(_packInfo.atList == null)
        {
            _packInfo.atList = new List<atListItem>();
        }
        else{
            _packInfo.atList.Clear();
        }
        _packInfo.atList.AddRange(_selectedUids);
        CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(_packInfo, data =>
        {
            AlbumRequestCtrl.Inst.PublicPhotoInfo(new List<CameraAlbumInfo>() { data }, 2, (list, opt) =>
            {
                CameraImgDataUtils.Inst.PersistUploadOrReuploadToLocal(list, opt);
            });
        }, err =>
        {
            TipPanel.ShowToast("上传失败，请重试");
            LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
        });
    }

    private void OnBtnInputGameNameClick()
    {
        _nameKBInfo.defaultText = Txt_InputName.text;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
    }

    private void OnGetNameFormNative(string value)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        Txt_InputName.text = value;
        _searchWord = value ?? "";
        RequestFirstPageFriendList();
    }

    private void RequestFirstPageFriendList()
    {
        _isEnd = false;
        _cookie = "";
        datas.Clear();
        RequestFriendListPage();
    }

    private void RequestNextPageFriendList()
    {
        RequestFriendListPage();
    }

    private void AppendFriends(List<MyFriendsInfo> list)
    {
        if (list == null || list.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var friendInfo = list[i];
            atListItem atItem = new atListItem();
            atItem.uid = friendInfo.userInfo.uid;
            atItem.username = friendInfo.userInfo.nickname;
            var isSelect = false;
            if(_selectedUids == null || _selectedUids.Count == 0)
            {
                isSelect = false;
            }
            else
            {
                foreach(var item in _selectedUids)
                {
                    if(item.uid == atItem.uid)
                    {
                        isSelect = true;
                        break;
                    }
                }
            }
            datas.Add(new CameraNoticeCellData
            {
                friendInfo = friendInfo,
                isSelected = isSelect
            });
        }
    }

    private void RequestFriendListPage()
    {
        if (_isEnd)
        {
            return;
        }

        // 面板/对象可能被销毁，避免回调 NRE
        if (this == null || gameObject == null || Adpter == null || Adpter.Data == null)
        {
            return;
        }

        // 有输入内容则走搜索接口；否则拉取好友列表
        bool isSearch = !string.IsNullOrEmpty(_searchWord);
        string url = isSearch ? HttpUrlDefine.searchFriend : HttpUrlDefine.friendList;
        string reqJson;

        if (isSearch)
        {
            _searchFriendReq.searchWord = _searchWord;
            _searchFriendReq.cookie = _cookie;
            _searchFriendReq.relationShip = (int)RelationShipType.Friend;
            _searchFriendReq.relationStatus = (int)RelationStatusType.Each;
            reqJson = JsonConvert.SerializeObject(_searchFriendReq);
        }
        else
        {
            _friendListReq.relationShip = (int)RelationShipType.Friend;
            _friendListReq.relationStatus = (int)RelationStatusType.Each;
            _friendListReq.cookie = _cookie;
            reqJson = JsonConvert.SerializeObject(_friendListReq);
        }

        NetworkManager.Inst.SendHttpRequest(url,
            HttpMethod.GET,
            reqJson,
            onReceive: msg =>
            {
                if (this == null || gameObject == null || Adpter == null || Adpter.Data == null)
                {
                    return;
                }

                MyFriendsListResData resourceInfo = null;
                try
                {
                    resourceInfo = JsonConvert.DeserializeObject<MyFriendsListResData>(msg);
                }
                catch
                {
                    // 解析失败时，当作空列表处理
                }

                _isEnd = resourceInfo != null && resourceInfo.isEnd == 1;
                _cookie = resourceInfo?.cookie ?? _cookie;

                var list = resourceInfo?.list;
                if (list != null && list.Count > 0)
                {
                    AppendFriends(list);
                }
                Adpter.Data.ResetItems(datas.Count);
                Adpter.Refresh();
            },
            onFail: _ =>
            {
                if (this == null || gameObject == null || Adpter == null || Adpter.Data == null)
                {
                    return;
                }
                Adpter.Data.ResetItems(datas.Count);
                Adpter.Refresh();
            });
    }

    private void OnCloseBtnClick()
    {
        CloseSelf();
        // if(_packInfo.atList == null)
        // {
        //     _packInfo.atList = new List<atListItem>();
        // }
        // else{
        //     _packInfo.atList.Clear();
        // }
        // _packInfo.atList.AddRange(_selectedUids);
        // CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(_packInfo, data =>
        // {
        //     AlbumRequestCtrl.Inst.PublicPhotoInfo(new List<CameraAlbumInfo>() { data }, 2);
        // }, err =>
        // {
        //     TipPanel.ShowToast("上传失败，请重试");
        //     LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
        // });
    }
}

[Serializable]
public class CameraNoticeCellData
{
    public MyFriendsInfo friendInfo;
    public bool isSelected;
}