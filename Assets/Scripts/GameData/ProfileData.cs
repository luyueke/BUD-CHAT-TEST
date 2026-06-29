using System;
using System.Collections.Generic;
using GameData.Base;

public class CommonSingleConfirmPanel_Style2Data
{
    public string TopTitleString = "";//顶部标题
    public string ContextString = "";//弹窗内容
    public string ConfirmString = "";//按钮标题
    public Action ConfirmClickAction = null;//按钮事件
    public Action OnCloseAction = null;//关闭事件
    public bool CanClose = true;
}
public class SetPermissionReq
{
    //1 InteractList 2 PublishList
    public int pageType;
    //1 everyone 2 myself
    public int permissionType;
    //仅设置交互列表时传入
    public int interactType;
    //仅设置发布列表时传入
    public int ugcType;
}

public class SearchByIdParams
{
    public string targetId;
}


public class SearchByIdResponse
{
    public AccountUserInfo userInfo;
    public RelationShipInfo relationShipInfo;
    public BanStateInfo banState;
}
public class BanStateInfo
{
    public int isChatBan;
    public int isAudioBan;
}
public class RelationShipInfo
{
    public int relationStatus;
    public int relationShip;
}

public class MyFriendsListReqQuerry
{
    public int relationShip;
    public int relationStatus;
    public string cookie = "";
}

public class ConversationListReqQuery
{
    public string cookie = "";
    public string toUid = "";
    public string searchWord = "";
}


public class MyFriendsListResData
{
    public string cookie;
    public int isEnd;
    public List<MyFriendsInfo> list;
    public RelationAmountInfo relationAmountInfo;
}

public class ConversationListResponse
{
    public string cookie;
    public int isEnd;
    public List<ConversationListItem> list;
}
public class ConversationListItem
{
    public AccountUserInfo userInfo;
    public string uid;
    public string  nickname;
    public string  portraitUrl;
    public string  coversationId;
    public int count;
    public string lastUpdateTime;
    public int isOnline;
    public bool isSelect;
}
public class OfflineMessageResponse
{
    public string cookie;
    public int isEnd;
    public List<OfflineMessageItem> list;
    public int chatBubbles;
    public int avatarFrame;
    public int nicknameFrame;
}

public class SetChatReq
{
    public int setType;//1 -发送聊天信息｜ 2 -阅读聊天信息 | 3- 发起聊天
    public string toUid;//对方好友的uid
    public int msgType;//消息类型，1-文本消息
    public string msgData;//消息数据结构，根据消息类型解析，服务器不关心
}

public class TcpChatData
{
    public int type;
    public string toUid;
    public string data;
    public long timeStamp;
    public long totalUnReadCount;
}

public class OfflineMessageItem
{
    public int type;
    public string  data;
    public string  uid;
    public int timestamp;
    public string nickname;
    public string portraitUrl;
    public int chatBubbles;
    public int avatarFrame;
    public int nicknameFrame;
}

public class RelationAmountInfo
{
    public int relationAmount;
    public int onlineRelationAmount;
}

public class MyFriendsInfo
{
    public AccountUserInfo userInfo;
    public AmountData amount;
    public int isOnline;
    public RoomInfo roomInfo;
    public BaseInteractInfo interactInfo;
}

public class PhotoInfo
{
    public string photoId;
    public string photoUrl;
    public string photoName;
    public string photoTime;
    public string photoPosName;
}

public class RoomInfo
{
    public string mapId;
    public string mapName;
    public string roomCode;
}

public class AmountData
{
    public int followingAmount;
    public int fansAmount;
}

public class SetRealtionParams
{
    public int setType;
    public int relationship;
    public string targetUid;
}

public enum RelationStatusType
{
    None = 0,
    Posi = 1,
    Reve = 2,
    Each = 3
}

public enum RelationShipType
{
    Follow = 1,
    Friend = 2
}


public enum SetRelationType
{
    Set =1 ,
    Cancel =2
}

public class SearchFriendParams
{
    public string searchWord;
    public int relationShip;
    public int relationStatus;
    public string cookie = "";
}

public class BatchInfoReq
{
    public string uidList;
}

public class BatchInfoResponse
{
    public List<SearchByIdResponse> list;
}

public class GiftUserListResponse
{
    public string cookie;
    public int isEnd;
    public List<MyFriendsInfo> list;
}


public class GiftUserListReq
{
    public string giftId;
    public int giftType;
    public int relationShip;
    public string cookie = "";
    public int opType;
    public string searchWord;
    public string seasonPassType;
}




