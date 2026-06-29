using AIGame.Base;
using Com.TheFallenGames.OSA.Util.IO;
using Network.Http;
using Network;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using Newtonsoft.Json.Linq;
using GameData.UGCData;
using Game;
using System.Collections;
using GameData.Base;
using GameData.BaseInfo;
using System.Collections.Generic;
using UnityEngine.UI;
using GameData;


#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class AIHospitalSharePanel : BasePanel<AIHospitalSharePanel>
{
    [SerializeField]
    private RemoteImageBehaviour _mapCover;

    [SerializeField] private RemoteImageBehaviour _fullMapCover;

    [SerializeField]
    private GameObject _waterMarks;

    [SerializeField]
    private GameObject _topBar;

    [SerializeField] private CButton _backBtn;

    [SerializeField] private CButton _savePicBtn;

    [SerializeField] private UserInfoView _userInfo;

    [SerializeField] private Image _logo;

    private bool _bShareMode = false;

    private MapInfo _curMapInfo;

    private float pauseStartTime;    // Pause 开始时间
    private float pauseEndTime;      // Pause 结束时间
    private float focusStartTime;    // Focus 开始时间
    private float focusEndTime;      // Focus 结束时间
    private const float SHARE_TIMEOUT = 2f;  // 分享超时时间

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        AddListener();
        string mapID = string.Empty;
        if (args != null && args.Length > 0 && args[0] != null)
        {
            mapID = args[0].ToString();
            OnGetMapInfo(mapID);
        }
        else
        {
            LoggerUtils.LogError("AIHospitalSharePanel: mapID is null or empty");
        }
    }

    private void OnGetMapInfo(string mapId)
    {
        JObject req = new JObject()
        {
            ["id"] = mapId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
    OnMapInfoSuccess, OnMapInfoFail);
    }


    private void InitCover(string url)
    {
        _mapCover.Load(url);
        _mapCover.gameObject.SetActive(true);
        _fullMapCover.Load(url);
    }

    private void InitUserInfo(AccountUserInfo accountUserInfo)
    {
        _userInfo.SetData(accountUserInfo);
    }

    private void OnMapInfoSuccess(string content)
    {
        UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
        _curMapInfo = rspData.mapInfo;
        var _curCreator = rspData.creator;
        //_curInteractInfo = rspData.interactInfo;
        //_curRelationShipInfo = rspData.relationShipInfo;
        InitCover(_curMapInfo.cover);
        InitUserInfo(_curCreator);
    }

    private IEnumerator takeScreenshotAndSave()
    {
        yield return new WaitForEndOfFrame();
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height);

        //Get Image from screen
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();

        //Convert to png
        byte[] imageBytes = screenImage.EncodeToPNG();
        string img_name = "碧优蒂的世界.png";
        string destination_path = Application.persistentDataPath + "/" + img_name; ;
        System.IO.File.WriteAllBytes(destination_path, imageBytes);
        SavePhoto(img_name,imageBytes);
        LoggerUtils.Log($"保存路径: {destination_path}");

        yield return new WaitForEndOfFrame();
        SetTopBarActive(true);
        _waterMarks.SetActive(false);
        _mapCover.gameObject.SetActive(true);
        SunShineNativeShare.instance.ShareSingleFile(destination_path, SunShineNativeShare.TYPE_IMAGE, _curMapInfo.name, "碧优蒂的世界");
        ShareSuccess();
    }

    private void SavePhoto(string name, byte[] bytes)
    {
        string filePath = LocalDataUtils.Inst.SaveTempImgRes(name, bytes);
        SaveMediaParams data = new SaveMediaParams()
        {
            mediaType = 1,
            mediaUrl = filePath
        };

        MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
    }

    private void OnMapInfoFail(string error)
    {
        LoggerUtils.LogError(error);
    }

    private void AddListener()
    {
        _backBtn.onClick.AddListener(CloseSelf);
        _savePicBtn.onClick.AddListener(TakePhoto);

        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.saveMediaToLocal, OnSaveSuccess);
        MobileInterface.Instance.AddClientFail(MobileInterfaceDefine.saveMediaToLocal, OnSaveFail);
    }

    private void RemoveListener()
    {
        _backBtn.onClick.RemoveAllListeners();
        _savePicBtn.onClick.RemoveAllListeners();

        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.saveMediaToLocal);
        MobileInterface.Instance.DelClientFail(MobileInterfaceDefine.saveMediaToLocal);
    }

    private void OnSaveSuccess(string info)
    {
        TipPanel.ShowToast("照片已保存");
    }

    private void OnSaveFail(string info)
    {
        TipPanel.ShowToast("照片保存失败，请重试");
    }

    private void TakePhoto()
    {
        if (!HasRequiredPermissions())
        {
            RequestPermissions();
            return;
        }
        if (DeviceInfoManager.Inst.DeviceBaseData.CompareVersion(AIGameHospitalConfig.shareNeedVersion) > 0)
        {
            _bShareMode = true;
            //todo 拍照的时候先把_topbar隐藏，显示 _waterMarks
            SetTopBarActive(false);
            _mapCover.gameObject.SetActive(false);
            _waterMarks.SetActive(true);
            StopCoroutine(takeScreenshotAndSave());
            StartCoroutine(takeScreenshotAndSave());
        }
        else
        {
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, ForceUpdate.NeedUpdateFeature);
        }
    }

    private void SetTopBarActive(bool value)
    {
        _topBar.SetActive(value);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        RemoveListener();
    }

    public void ShareSuccess()
    {
        //todo
        _bShareMode = false;
        LoggerUtils.LogError("分享成功:可以向服务器发起请求");
        ShareRequest();
    }

    private void ShareRequest()
    {
        List<int> shareType = new List<int>();
        var aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
        shareType.Add((int)EShareType.Play);
        
        if (aiGame && aiGame.GetGamePassState())
        {
            shareType.Add((int)EShareType.Pass);
        }
        
        JObject req = new()
        {
            ["id"] = _curMapInfo.id,
            ["from"] = JArray.FromObject(shareType)
        };
        
        var paramStr = JsonConvert.SerializeObject(req);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UgcGameShare, HttpMethod.POST, paramStr, (content) =>
        {
            var s9UgcShareRtn = JsonConvert.DeserializeObject<S9UgcShareRtn>(content);
            if (s9UgcShareRtn.result != 0 && s9UgcShareRtn.data != null)
            {
                LoggerUtils.LogError(s9UgcShareRtn.rmsg);
                return;
            }
            // 转换格式 把s9UgcShareRtn.data.rewards转换为TaskRewardData    
            if (s9UgcShareRtn.data != null && s9UgcShareRtn.data.rewards != null)
            {
                //var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                //List<TaskRewardData> rewardList = s9UgcShareRtn.data.rewards.Select(x => new TaskRewardData()
                //{
                //    rewardType = x.rewardType,
                //    num = x.amount
                //}).ToList();
                //rewardPanel.ShowRewards(rewardList);
            }
            LoggerUtils.Log("游戏分享成功");
        }, (err) => { LoggerUtils.Log("s9游戏分享失败", err); }, null, 0, 3);
    }



    public void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            pauseStartTime = Time.realtimeSinceStartup;
        }
        else
        {
            pauseEndTime = Time.realtimeSinceStartup;
        }
        CheckShareTimer(!pause, "OnApplicationPause");
    }

    public void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            focusStartTime = Time.realtimeSinceStartup;
        }
        else
        {
            focusEndTime = Time.realtimeSinceStartup;
        }
        CheckShareTimer(focus, "OnApplicationFocus");
    }

    private void CheckShareTimer(bool value, string source)
    {
        if (!_bShareMode)
        {
            return;
        }

        if (!value)
        {
            // 失去焦点或暂停时，记录开始时间
            pauseStartTime = Time.realtimeSinceStartup;
            focusStartTime = Time.realtimeSinceStartup;
        }
        else
        {
            // 恢复焦点或暂停时，计算最长的暂停时长
            pauseEndTime = Time.realtimeSinceStartup;
            focusEndTime = Time.realtimeSinceStartup;
            
            float pauseDuration = pauseEndTime - pauseStartTime;
            float focusDuration = focusEndTime - focusStartTime;
            float maxDuration = Mathf.Max(pauseDuration, focusDuration);
            
            LoggerUtils.Log($"[SharePanel] {source} - Pause时长: {pauseDuration:F2}s, Focus时长: {focusDuration:F2}s, 取最大值: {maxDuration:F2}s");
            
            
            // 如果最长暂停时间超过阈值，认为分享成功
            if (maxDuration >= SHARE_TIMEOUT)
            {
                _bShareMode = false;
                ShareSuccess();
            }
        }
    }

    private bool HasRequiredPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) &&
                Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                return true;
            }
            return false;
#else
        return true;
#endif
    }

    private void RequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 创建权限回调
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (string permissionName) =>
            {
                LoggerUtils.Log($"权限已授予: {permissionName}");
                if (HasRequiredPermissions())
                {
                    // 如果所有需要的权限都获取到了，继续拍照操作
                    TakePhoto();
                }
            };
            callbacks.PermissionDenied += (string permissionName) =>
            {
                LoggerUtils.LogError($"权限被拒绝: {permissionName}");
                TipPanel.ShowToast("需要存储权限才能保存图片");
            };
            callbacks.PermissionDeniedAndDontAskAgain += (string permissionName) =>
            {
                LoggerUtils.LogError($"权限被永久拒绝: {permissionName}");
                TipPanel.ShowToast("请在系统设置中开启存储权限");
            };

            // 请求权限
            string[] permissions = new string[] {
            Permission.ExternalStorageWrite,
            Permission.ExternalStorageRead
        };
            Permission.RequestUserPermissions(permissions, callbacks);
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"请求权限出错：{e.Message}");
            TipPanel.ShowToast("权限请求失败，请重试");
        }
#endif
    }
}
