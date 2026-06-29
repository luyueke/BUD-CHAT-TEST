using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AIGame.Base;
using Basic;
using Basic.Utils;
using BUD.AnimPose;
using BUD.MailBox;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.AIResData;
using Game.Audio;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.Account;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Base;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
/// <summary>
/// Author:
/// Desc:
/// Date:23-07-17 17:13:59
/// </summary>
public class TestIncubationCabinPanel : BasePanel<TestIncubationCabinPanel>
{
    [SerializeField] private Transform AvatarParent;
    [SerializeField] private Camera AvatarCamera;
    [SerializeField] private UIDragUtil DragUtil;




    [SerializeField] private Button backBtn;
    [SerializeField] private Button hideAllBtn;
    [SerializeField] private Button printBtn;
    [SerializeField] private Text printText;


    [SerializeField] private GameObject hideAllRoot;

    [SerializeField] private Slider slider1;
    [SerializeField] private Slider slider2;
    [SerializeField] private Slider slider3;
    [SerializeField] private Slider slider4;
    [SerializeField] private Slider slider5;
    [SerializeField] private Slider slider6; //顶光角度x
    [SerializeField] private Slider slider7;
    [SerializeField] private Slider slider8; //顶光角度y
    [SerializeField] private Dropdown dropdown;


    private Text userName;
    private Image chatReddot;
    private Text chatReddotText;

    private CharacterWrap _characterWrap;
    private PlayerIdleBehaviour idleBehaviour;
    private PetWrap _petWrap;
    private PetPlayerIdleBehaviour petIdleBehaviour;
    private CharacterWrap _npcWrap;
    private NpcPlayerIdleBehaviour npcIdleBehaviour;

    string key = "firsOpenGame" + AccountDataManager.Inst.UserInfo.uid;

    private bool isSetBodyPos = false;

    private Vector3 originalAvatarParentPos;

    private enum PreRequestType
    {
        None = 0,
        Pre = 1,
        Next = 2
    }

    GameObject leftGo;
    GameObject rightGo;
    GameObject upGo;
    GameObject downGo;

    Vector3 originalLeftPos;
    Vector3 originalRightPos;
    Vector3 originalUpPos;
    Vector3 originalDownPos;

    Light light;
    float originalLightIntensity;
    float lightOriginalAngleY;
    float lightOriginalAngleX;

    public override void OnCreate()
    {
        QualityManager.Inst.SetTargetQualityShadow(true);

        AvatarCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        var aiCharacterRootGo = GameObject.Find("AICharacter (1)");
        var cam = AvatarCamera;
        if (cam != null)
        {
            AvatarParent.transform.SetParent(null);
            AvatarParent.transform.position = aiCharacterRootGo.transform.position;
            AvatarParent.transform.localScale = new Vector3(1, 1, 1);
            AvatarParent.transform.eulerAngles = new Vector3(0, 180, 0);

            aiCharacterRootGo.transform.localScale = Vector3.zero;

            cam.transform.eulerAngles = new Vector3(0, 0, -90);
            var cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            GlobalCameraManager.Inst.InsertFirst(cam);
        }
        backBtn.onClick.AddListener(OnBackBtnClick);
        hideAllBtn.onClick.AddListener(OnHideAllBtnClick);
        printBtn.onClick.AddListener(OnPrintBtnClick);

        var Scence = GameObject.Find("Scence");
        leftGo = Scence.transform.Find("left").gameObject;
        rightGo = Scence.transform.Find("right").gameObject;
        upGo = Scence.transform.Find("up").gameObject;
        downGo = Scence.transform.Find("down").gameObject;
        originalLeftPos = leftGo.transform.position;
        originalRightPos = rightGo.transform.position;
        originalUpPos = upGo.transform.position;
        originalDownPos = downGo.transform.position;


        light = GameObject.Find("PreviewPanelDirectionalLight2").GetComponent<Light>();
        originalLightIntensity = light.intensity;
        lightOriginalAngleY = light.transform.eulerAngles.y;
        lightOriginalAngleX = light.transform.eulerAngles.x;
        slider1.value = 0.5f;
        slider2.value = 0.5f;
        slider3.value = 0.5f;
        slider4.value = 0.5f;
        slider5.value = 0.5f;
        slider6.value = 0.5f;
        slider7.value = 0.5f;
        slider8.value = 0.5f;
        slider1.onValueChanged.AddListener(OnSlider1ValueChanged);
        slider2.onValueChanged.AddListener(OnSlider2ValueChanged);
        slider3.onValueChanged.AddListener(OnSlider3ValueChanged);
        slider4.onValueChanged.AddListener(OnSlider4ValueChanged);
        slider5.onValueChanged.AddListener(OnSlider5ValueChanged);
        slider6.onValueChanged.AddListener(OnSlider6ValueChanged);
        slider7.onValueChanged.AddListener(OnSlider7ValueChanged);
        slider8.onValueChanged.AddListener(OnSlider8ValueChanged);
        dropdown.options.Clear();
        dropdown.options.Add(new Dropdown.OptionData("1:1"));
        dropdown.options.Add(new Dropdown.OptionData("9:16"));
        dropdown.options.Add(new Dropdown.OptionData("4:5"));
        dropdown.options.Add(new Dropdown.OptionData("5:7"));
        dropdown.options.Add(new Dropdown.OptionData("3:4"));
        dropdown.options.Add(new Dropdown.OptionData("3:5"));
        dropdown.options.Add(new Dropdown.OptionData("2:3"));
        dropdown.options.Add(new Dropdown.OptionData("9:21"));

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        isSetBodyPos = false;
        if (AvatarParent != null)
        {
            originalAvatarParentPos = AvatarParent.localPosition;
        }

        var userInfo = AccountDataManager.Inst.UserInfo;





        PlayerAnimationCtrl avatarAnimCtrl = null;
        CharacterData avatarInfo = userInfo.avatarInfo;
        if (avatarInfo != null)
        {
            AvatarDataManager.Inst.SelfCharacterData = avatarInfo;
            // 同一帧调用两次会有问题 待解决
            var wrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo, AvatarParent);
            _characterWrap = wrap;
            avatarAnimCtrl = wrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarAnimCtrl.CheckAndOverrideSpecialAnim();
            idleBehaviour = wrap.Avatar.AddComponent<PlayerIdleBehaviour>();
            idleBehaviour.Init(avatarAnimCtrl);

            var ugcBehaivour = wrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = wrap.Avatar.GetComponent<AnimIKController>();
            ugcBehaivour.Init(ikController);
        }

        var petInfo = AccountDataManager.Inst.PetInfo;
        PetData petData = petInfo?.avatarInfo;
        if (petData != null)
        {
            _petWrap = PetAvatarController.Inst.CreateUIAvatarWithIKController(petData, AvatarParent);
            var animationCtrl = _petWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            petIdleBehaviour = _petWrap.Avatar.AddComponent<PetPlayerIdleBehaviour>();
            petIdleBehaviour.Init(animationCtrl);

            var petUgcIdleBehaivour = _petWrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = _petWrap.Avatar.GetComponent<AnimIKController>();
            petUgcIdleBehaivour.Init(ikController);
            petIdleBehaviour.avatarAnimCtr = avatarAnimCtrl;
            _petWrap.Avatar.SetActive(false);
        }


        var npcInfo = AccountDataManager.Inst.AIBuddyInfo;
        var npcAvatarJson = npcInfo?.npc?.npcAvatarJson;

        if (string.IsNullOrEmpty(npcAvatarJson))
        {
            // 特殊逻辑，确保创建出来了NPC，后续只是做换装处理；
            npcAvatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        }

        if (!string.IsNullOrEmpty(npcAvatarJson))
        {
            var npcData = CharacterData.DeserializeObject(npcAvatarJson);
            _npcWrap = AvatarController.Inst.CreateUIAvatarWithIKController(npcData, AvatarParent);
            var animationCtrl = _npcWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            npcIdleBehaviour = _npcWrap.Avatar.AddComponent<NpcPlayerIdleBehaviour>();
            npcIdleBehaviour.Init(animationCtrl);
            npcIdleBehaviour.avatarAnimCtr = avatarAnimCtrl;
            var npcUgcIdleBehaivour = _npcWrap.Avatar.AddComponent<UgcIdleBehaviour>();
            var ikController = _npcWrap.Avatar.GetComponent<AnimIKController>();
            npcUgcIdleBehaivour.Init(ikController);
            _npcWrap.Avatar.SetActive(false);
        }

        DragUtil.RotateTarget = AvatarParent.transform;
        ReddotManagerUtils.Inst.Init();
        OnPreHttpRequest();
        SetUserInfo();
    }

    private void OnBackBtnClick()
    {
        GlobalCameraManager.Inst.Remove(AvatarCamera);
        xasset.Scene.LoadAsync($"Assets/Arts/Scenes/GameHall.unity").completed += operation =>
        {
            UIManager.Inst.ClosePanel(PanelId.TestIncubationCabinPanel);
            UIManager.Inst.OpenPanel(PanelId.GameHallPanel);
        };
    }
    private void OnHideAllBtnClick()
    {
        hideAllRoot.SetActive(!hideAllRoot.activeSelf);
    }
    private void OnPrintBtnClick()
    {
        printText.text = $"{slider1.value},{slider2.value},{slider3.value},{slider4.value},{slider5.value},{slider6.value},{slider8.value},{slider7.value},{dropdown.value}";
    }
    private void OnSlider1ValueChanged(float value)
    {
        var newValue = (value - 0.5f) * 2;
        AvatarCamera.orthographicSize = 1.48f + newValue;
    }
    /// <summary>
    /// 缩放比例
    /// </summary>
    /// <param name="value"></param>
    private void OnSlider2ValueChanged(float value)
    {
        //0~5的倍数
        var newValue = value * 2;
        AvatarParent.localScale = new Vector3(newValue, newValue, newValue);
    }
    /// <summary>
    /// 左右
    /// </summary>
    /// <param name="value"></param>
    private void OnSlider3ValueChanged(float value)
    {
        var moveValue = (value - 0.5f) * 4;
        // 使用本地坐标
        leftGo.transform.position = originalLeftPos - leftGo.transform.up * moveValue;
        rightGo.transform.position = originalRightPos - rightGo.transform.up * moveValue;
    }
    /// <summary>
    /// 只调整上面的板子
    /// </summary>
    /// <param name="value"></param>
    private void OnSlider4ValueChanged(float value)
    {
        var moveValue = (value - 0.5f) * 4;
        upGo.transform.position = originalUpPos - upGo.transform.up * moveValue;
    }
    /// <summary>
    /// 调整相机y位置
    /// </summary>
    /// <param name="value"></param>
    private void OnSlider5ValueChanged(float value)
    {
        var newValue = (value - 0.5f) * 6;
        var trans = AvatarCamera.transform;
        trans.position = new Vector3(trans.position.x, 0.8f + newValue, trans.position.z);
    }
    /// <summary>
    /// 顶光角度x
    /// </summary>
    /// <param name="value"></param>
    private void OnSlider6ValueChanged(float value)
    {
        var newValue = (value - 0.5f) * 360;
        var eulerAnglesX = lightOriginalAngleX + newValue;
        light.transform.eulerAngles = new Vector3(eulerAnglesX, light.transform.eulerAngles.y, light.transform.eulerAngles.z);
    }
    private void OnSlider7ValueChanged(float value)
    {
        var newValue = (value - 0.5f) * 6;
        light.intensity = originalLightIntensity + newValue;
    }
    /// <summary>
    /// 顶光角度y
    /// </summary>
    /// <param name="value"></param>
    private void OnSlider8ValueChanged(float value)
    {
        var newValue = (value - 0.5f) * 360;
        var eulerAnglesY = lightOriginalAngleY + newValue;  
        light.transform.eulerAngles = new Vector3(light.transform.eulerAngles.x, eulerAnglesY, light.transform.eulerAngles.z);
    }
    private void OnDropdownValueChanged(int value)
    {
        if (AvatarCamera == null) return;

        // 解析宽高比
        float targetAspect = 1f;
        switch (value)
        {
            case 0: // 1:1
                targetAspect = 1f / 1f;
                break;
            case 1: // 9:16
                targetAspect = 9f / 16f;
                break;
            case 2: // 4:5
                targetAspect = 4f / 5f;
                break;
            case 3: // 5:7
                targetAspect = 5f / 7f;
                break;
            case 4: // 3:4
                targetAspect = 3f / 4f;
                break;
            case 5: // 3:5
                targetAspect = 3f / 5f;
                break;
            case 6: // 2:3
                targetAspect = 2f / 3f;
                break;
            case 7: // 9:21
                targetAspect = 9f / 21f;
                break;
            default:
                targetAspect = 9f / 16f;
                break;
        }
        targetAspect = 1 / targetAspect;

        // 获取当前屏幕宽高比
        float screenAspect = (float)Screen.width / Screen.height;

        // 计算相机视口矩形
        Rect rect = new Rect(0, 0, 1, 1);

        // if (screenAspect > targetAspect)
        // {
        //     // 屏幕更宽，需要上下加黑边（letterbox）
        //     float height = targetAspect / screenAspect;
        //     float y = (1f - height) / 2f;
        //     rect = new Rect(0, y, 1, height);
        // }
        // else if (screenAspect < targetAspect)
        // {
        //     // 屏幕更高，需要左右加黑边（pillarbox）
        //     float width = screenAspect / targetAspect;
        //     float x = (1f - width) / 2f;
        //     rect = new Rect(x, 0, width, 1);
        // }
        // Debug.LogError(AvatarCamera.rect);

        // AvatarCamera.rect = rect;



        float scaleHeight = screenAspect / targetAspect;
        if (scaleHeight < 1.0f)
        {
            // 屏幕太窄 → 上下黑边
            Rect rect2 = AvatarCamera.rect;
            rect2.width = 1.0f;
            rect2.height = scaleHeight;
            rect2.x = 0;
            rect2.y = (1.0f - scaleHeight) / 2.0f;
            AvatarCamera.rect = rect2;
            Debug.LogError("111=" + AvatarCamera.rect);
        }
        else
        {
            // 屏幕太宽 → 左右黑边
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect2 = AvatarCamera.rect;
            rect2.width = scaleWidth;
            rect2.height = 1.0f;
            rect2.x = (1.0f - scaleWidth) / 2.0f;
            rect2.y = 0;
            AvatarCamera.rect = rect2;
            Debug.LogError("222=" + AvatarCamera.rect);
        }

        // 应用视口矩形

        GetComponent<StretchToOffsets>().ConvertStretchToOffsets();

        Debug.LogError($"OnDropdownValueChanged: {value}, targetAspect: {targetAspect}, screenAspect: {screenAspect}, rect: {AvatarCamera.rect}");
    }















    private void ResetAvatarParentPos()
    {
        if (AvatarParent != null)
        {
            AvatarParent.localPosition = originalAvatarParentPos;
        }
    }




    private void SetUserInfo()
    {
        AccountUserInfo userInfo = AccountDataManager.Inst.UserInfo;
        userName.SetText(userInfo.nickname);
        var path = userInfo.portraitUrl;
    }


    private void OnPreHttpRequest()
    {
        var httpRequests = Es.DataTables.GetNetHttpBusinessList();
        for (var i = 0; i < httpRequests.Count; i++)
        {
            var netData = httpRequests[i];
            if (netData.IsPreRequest == (int)PreRequestType.Pre)
            {
                NetworkProxy.SendHttpRequest(netData.HttpUrl, HttpMethod.GET, netData.ParamStr,
                    arg => OnNextHttpRequest(netData, arg), null, new NetCacheEvent(null));
            }
        }
    }

    private void OnNextHttpRequest(NetHttpBusiness data, string content)
    {
        string nextCmd = data.NextCmd;
        if (!string.IsNullOrEmpty(nextCmd))
        {
            var nextData = Es.DataTables.GetNetHttpBusiness(nextCmd);
            if (nextData.IsPreRequest == (int)PreRequestType.Next)
            {
                JObject rObject = JsonConvert.DeserializeObject<JObject>(content);
                JArray rArray = rObject[data.Arg0] as JArray;
                JObject firstObject = rArray.First() as JObject;
                JObject jObject = JsonConvert.DeserializeObject<JObject>(nextData.ParamStr);
                Dictionary<string, string> maps =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(data.Arg1);
                foreach (var keyValue in maps)
                {
                    jObject[keyValue.Value] = firstObject[keyValue.Key];
                }

                NetworkProxy.SendHttpRequest(nextData.HttpUrl, HttpMethod.GET, JsonConvert.SerializeObject(jObject),
                    null, null, new NetCacheEvent(null));
            }
        }
    }



    public void OnIdleChange(AccountUserInfo userInfo, AccountPetInfo petInfo, AIBuddyInfo buddyInfo, VehicleInfo vehicleInfo = null)
    {
        //当前的载具显示优先级最高，后续有冲突和策划确认
        if (vehicleInfo != null && vehicleInfo.isHidden == 0)
        {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, vehicleInfo, AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            _npcWrap.Avatar.SetActive(false);
            _petWrap.Avatar.SetActive(false);
            //_characterWrap.CustomAvatar.transform.localPosition = Vector3.zero;
            isSetBodyPos = false;
            //StartSetPosByBodyType(userInfo.avatarInfo);
            return;
        }

#if UNITY_EDITOR
        if (DebugSetting.Inst != null && DebugSetting.Inst.isShowTempVehicle && TempVehicleDataSave.GetTempVehicleInfo() != null)
        {
            AvatarAndVehicleAnimView vehicleAnimView = new AvatarAndVehicleAnimView(_characterWrap, TempVehicleDataSave.GetTempVehicleInfo(), AvatarParent);
            vehicleAnimView.StartVehicleAnim(true);
            return;
        }
#endif

        AvatarAndPetAnimView animView = new AvatarAndPetAnimView(_characterWrap, _petWrap, _npcWrap, idleBehaviour, petIdleBehaviour, npcIdleBehaviour, AvatarCamera);
        var animType = animView.GetHallAnimatoionType(petInfo, buddyInfo);
        animView.OnAnimChange(true, animType, userInfo, petInfo, buddyInfo);
        if (!string.IsNullOrEmpty(buddyInfo?.npc?.npcAvatarJson))
        {
            _npcWrap.RefreshAvatar(CharacterData.DeserializeObject(buddyInfo.npc.npcAvatarJson));
        }
        isSetBodyPos = false;
        StartSetPosByBodyType(userInfo.avatarInfo);
        ResetAvatarParentPos();
    }


    public override void OnShow(params object[] args)
    {



        if (!PlayerPrefs.HasKey(key))
        {
            UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel);
        }
        ExternalLinkSkipManager.Inst.GetCurrentUniversalLinkInfo();

    }

    public override void OnHidden()
    {

    }



    protected override void OnDestroy()
    {
        if (AvatarParent != null)
        {
            Destroy(AvatarParent.gameObject);
        }
        QualityManager.Inst.SetTargetQualityShadow(false);

        ResetAvatarParentPos();
    }

    public override void OnWindowBeCovered(bool isCover)
    {
        if (isCover)
        {
            if (!this || !this.transform) return;
            DealBaseLayout2D(false);
            DealBaseLayout3D(false);
            isSetBodyPos = false;
        }
    }

    public override void OnWindowShow()
    {
        if (!this || !this.transform) return;
        DealBaseLayout2D(true);
        DealBaseLayout3D(true);
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (avatarInfo == null)
            avatarInfo = AvatarDataManager.Inst.SelfCharacterData;

        var avatarAnimCtrl = _characterWrap?.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();

        avatarAnimCtrl?.CheckAndOverrideSpecialAnim();
        ReddotManagerUtils.Inst.RefreshRedDot();
        LimitTimePropManager.Inst.RefashLimitTimeProps();
        AIBuddyDataManager.Inst.RequestAIBuddyList();
        if (AccountDataManager.Inst.VehicleInfo != null && AccountDataManager.Inst.VehicleInfo.isHidden == 0)
        {
            return;
        }
        StartSetPosByBodyType(avatarInfo);
        ResetAvatarParentPos();
    }

    public override void OnWindowPop()
    {
    }


    private void StartSetPosByBodyType(CharacterData userAvatarData)
    {
        if (isSetBodyPos) return;
        SetPosByBodyType(_characterWrap, (CustomBodyTypeController.BodyType)userAvatarData.bodyType);
        if (!string.IsNullOrEmpty(AccountDataManager.Inst.AIBuddyInfo?.npc?.npcAvatarJson))
        {
            var npcData = CharacterData.DeserializeObject(AccountDataManager.Inst.AIBuddyInfo.npc.npcAvatarJson);
            SetPosByBodyType(_npcWrap, (CustomBodyTypeController.BodyType)npcData.bodyType);
        }
        isSetBodyPos = true;
    }

    private void SetPosByBodyType(CharacterWrap wrap, CustomBodyTypeController.BodyType bodyType)
    {
        if (wrap == null) return;
        if (wrap.Avatar == null) return;


        if (AccountDataManager.Inst.VehicleInfo != null
        && AccountDataManager.Inst.VehicleInfo.isHidden == 0)
        {
            if (!int.TryParse(AccountDataManager.Inst.VehicleInfo.id, out int pgcId))
            {
                ResetAvatarParentPos();
                AvatarParent.localPosition += new Vector3(0, 12, 285);
            }
        }
        else
        {
            ResetAvatarParentPos();
        }

        switch (bodyType)
        {
            case CustomBodyTypeController.BodyType.Type1:
            case CustomBodyTypeController.BodyType.Type2:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.6f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type3:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.43f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type4:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.26f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type5:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.49f, 0);
                break;
            case CustomBodyTypeController.BodyType.Type6:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.57f, 0);
                break;
            default:
                wrap.Avatar.transform.localPosition = new Vector3(0, -0.5f, 0);
                break;
        }
    }
}
