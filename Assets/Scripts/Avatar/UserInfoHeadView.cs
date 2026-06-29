using System;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData.BaseInfo;
using GameData.PgcData;
using Pb.Base;
using UnityEngine;

public class UserInfoHeadView : MonoBehaviour {
    [SerializeField] private SuperTextMesh nickName;


    [SerializeField] private SpriteRenderer verify;
    [SerializeField] private float verifyOffset = 0.65f;


    [SerializeField] private SpriteRenderer voice;

    private GameObject titleObj;

    private Camera mainCamera;
    private string layerName = "Default";

    //private PlayerInfo PlayerInfo;
    private string uid;
    private void Start()
    {
        nickName.gameObject.layer =  LayerMask.NameToLayer(layerName);
        verify.gameObject.layer =  LayerMask.NameToLayer(layerName);
        voice.gameObject.layer =  LayerMask.NameToLayer(layerName);


    }

     void OnEnable()
    {
        Message.MessageHelper.AddListener<string, int>(Message.MessageName.ChatMessageLives, ReceiveChatMessage);
        AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
    }

     void OnDisable()
    {
        Message.MessageHelper.RemoveListener<string, int>(Message.MessageName.ChatMessageLives, ReceiveChatMessage);
        AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
    }
    private void ReceiveChatMessage(string senderId, int count)
    {
        if(senderId == uid)
        {
            titleObj?.SetActive(count == 0);
        }
    }

    private void OnUserInfoChange(AccountUserInfo accountUserInfo)
    {
        SetUserTitleId(accountUserInfo);
    }
    private void SetUserTitleId(AccountUserInfo userInfo)
    {
        if (string.IsNullOrEmpty(uid) || uid != userInfo.uid)
        {
            return;
        }
        if (titleObj != null)
        {
            GameObject.DestroyImmediate(titleObj.gameObject);
            titleObj = null;
        }
 
        if (userInfo.titleId > 0)
        {
            var config = UserUIWidgetManager.Inst.GetTitleData(userInfo.titleId);

            if (config != null )
            {
                CreateTitle(config);
            }
        }
    


    }
    public void SetTitleInfo(PlayerInfo playerInfo)
    {
        if(titleObj != null)
        {
            GameObject.DestroyImmediate(titleObj.gameObject);
            titleObj = null;
        }
        uid = playerInfo.Uid;

        //Debug.LogError($"SetTitleInfo playerInfo.Uid={playerInfo.Uid},myid={ AccountDataManager.Inst.UserInfo.uid},myTitleid={AccountDataManager.Inst.UserInfo.titleId}" );
        if (playerInfo.Uid == AccountDataManager.Inst.UserInfo.uid)
        {
            if (AccountDataManager.Inst.UserInfo.titleId > 0)
            {
                var config = UserUIWidgetManager.Inst.GetTitleData(AccountDataManager.Inst.UserInfo.titleId);
                if (config != null)
                {
                    CreateTitle(config);
                }
            }
            return;
        }
        if (playerInfo != null && !string.IsNullOrEmpty(playerInfo.AvatarJson))
        {
            var saveCharacterData = CharacterData.DeserializeObject(playerInfo.AvatarJson);
            if(saveCharacterData.featureItems?.Count>0)
            {
                for (int i=0;i< saveCharacterData.featureItems.Count;i++)
                {
                    if(saveCharacterData.featureItems[i].type ==2)
                    {
                        var titleId = saveCharacterData.featureItems[i].id;
                        if (!string.IsNullOrEmpty(titleId) && int.TryParse(titleId,out int id))
                        {
                            var config = UserUIWidgetManager.Inst.GetTitleData(id);
                            CreateTitle(config);
                            break;
                        }
    
                    }
                }
            }

        }
    }

    private void CreateTitle(TitleData config)
    {

        var o = Loader.Load<GameObject>(config.GamePrefab, gameObject);
        titleObj = GameObject.Instantiate(o, transform);
        titleObj.transform.localPosition = new Vector3(0, 0.26f, 0);
        titleObj.transform.localScale = new Vector3(config.Size.x, config.Size.y, config.Size.z);
    }

    public void SetPlayerInfo(PlayerInfo playerInfo) {
        //PlayerInfo = playerInfo;
        nickName.text = playerInfo.Name;
        var verifyPos = verify.transform.localPosition;
        verifyPos.x = nickName.preferredWidth / 2 + verifyOffset;
        verify.transform.localPosition = verifyPos;
        verify.gameObject.SetActive(false);
        SetTitleInfo(playerInfo);
    }

    public void ResetPos(SkinType skinType, PlayerInfo playerInfo) {
        switch (skinType)
        {
            case SkinType.Pet:
                transform.localPosition = new Vector3(0, 0.9f, 0);
                transform.localEulerAngles = new Vector3(0, 180, 0);
                transform.localScale = new Vector3(-1, 1, 1);
                break;
            
            default:
            case SkinType.Avatar:
                var f = 1.276f;
                if (playerInfo != null) {
                    var saveCharacterData = CharacterData.DeserializeObject(playerInfo.AvatarJson);
                    if (saveCharacterData != null)
                    {
                        var part = saveCharacterData.GetPartData(UniqueType.GetAvatar(AvatarSubType.SpecialSkin));
                        if (part != null && part.Id == "10900505") 
                        {
                            f = 1.376f;
                        }

                    }
                }
                transform.localPosition = new Vector3(0, f, 0);
                transform.localEulerAngles = new Vector3(0, 180, 0);
                transform.localScale = new Vector3(-1, 1, 1);
                break;
        }
    }

    private void Update() {
        SetLookDir();
    }

    private void SetLookDir() {
        if (mainCamera == null) {
            var mainCameraObj = GameObject.Find("GlobalMainCamera");
            mainCamera = mainCameraObj.GetComponent<Camera>();
        }

        if (mainCamera != null) {
            var lootAt = mainCamera.transform.position;
            lootAt.y = transform.position.y;
            transform.LookAt(lootAt);
        }
    }

    public static void Load(GameObject rootObj, PlayerInfo playerInfo, SkinType skinType = SkinType.Avatar) {
        if (rootObj == null) {
            LoggerUtils.LogError("rootObj == null");
            return;
        }

        var headView = rootObj.GetComponentInChildren<UserInfoHeadView>(true);
        if (headView == null) {
            var kinematicCharacterController = rootObj.GetComponent<KinematicCharacterController>();
            if (kinematicCharacterController == null) {
                LoggerUtils.LogError("kinematicCharacterController == null");
                return;
            }
            var assetWrapper = Loader.Load<GameObject>("Assets/Loadable/UI/UIWidgets/UserInfoView/UserInfoHeadView.prefab");
            if (assetWrapper == null) {
                LoggerUtils.LogError("assetWrapper == null");
                return;
            }
            headView = assetWrapper.Instantiate(kinematicCharacterController.PlayerAnimCtrl.transform).GetComponent<UserInfoHeadView>();
            // lod
            kinematicCharacterController?.PlayerAnimCtrl?.transform?.GetComponent<CharacterLOD>()?.SetLodMesh(headView.gameObject);
        }
        headView.SetPlayerInfo(playerInfo);
        headView.ResetPos(skinType, playerInfo);
    }


}
