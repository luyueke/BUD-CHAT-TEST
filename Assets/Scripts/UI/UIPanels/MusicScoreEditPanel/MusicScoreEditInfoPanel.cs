using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using Game.CommunityGame;
using Game.COSXML;
using Game.PropStore;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class MusicScoreEditInfoPanel : BasePanel<MusicScoreEditInfoPanel>
{
    [SerializeField] private CButton backBtn;
    [SerializeField] private TextInputView nameInputView;
    [SerializeField] private CButton key15Btn;
    [SerializeField] private CButton key22Btn;
    [SerializeField] private GameObject key15Select;
    [SerializeField] private GameObject key22Select;
    [SerializeField] private CButton nextStepBtn;
    [SerializeField] private CButton editCoverBtn;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private Transform _trans_Bg;
    private MusicScoreInfo currentMusicScoreInfo;
    private ToneType _toneType = ToneType.Fifteen;
    private bool isInEditMode;
    private const int defBPM = 56;
    public override void OnCreate()
    {
        AddListeners();
        InitBG();
    }
    private void InitBG()
    {
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "music_icon_1", "music_icon_2", "music_icon_3"
        });
        item.gameObject.SetActive(true);
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args!=null&&args.Length>0)
        {
            isInEditMode =(bool)args[0];
            if (args.Length==2)
            {
                currentMusicScoreInfo= args[1] as MusicScoreInfo;
                LoadPhoto();
                nameInputView.SetInput(currentMusicScoreInfo.name);
            }
            else
            {
                nextStepBtn.interactable = false;
                currentMusicScoreInfo = new MusicScoreInfo()
                {
                    name = "",
                    toneType = (int)_toneType,
                    bpm = defBPM,
                };
            }
            if (currentMusicScoreInfo!=null)
            {
                OnKeySelect((ToneType)currentMusicScoreInfo.toneType);
            }

        }


    }

    private void AddListeners()
    {
        backBtn.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            UIManager.Inst.ClosePanel(this);
        });
        nameInputView.SetOnInput(OnInputComplete);
        key15Btn.onClick.AddListener(()=>OnKeySelect(ToneType.Fifteen));
        key22Btn.onClick.AddListener(()=>OnKeySelect(ToneType.TwentyTwo));
        nextStepBtn.onClick.AddListener(OnNextStepClick);
        editCoverBtn.onClick.AddListener(OnEditCoverClick);
    }
    private void OnInputComplete(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            nextStepBtn.interactable = false;
            return;
        }
        currentMusicScoreInfo.name = input;
        nextStepBtn.interactable = true;
    }

    private void OnKeySelect(ToneType type)
    {
        currentMusicScoreInfo.toneType = (int)type;
        key15Select.SetActive(type == ToneType.Fifteen);
        key22Select.SetActive(type == ToneType.TwentyTwo);
    }
    private void OnNextStepClick()
    {
        if (!isInEditMode)
        {
            GameController.StartGame(EnterGameModel.UgcMusicScoreEmpty, currentMusicScoreInfo, true, null);
        }
        else
        {
            CloseSelf();
        }
    }
    private void OnEditCoverClick()
    {
        OpenSystemAlbumParams albumParams = new OpenSystemAlbumParams()
        {
            albumType = 1, //0竖屏 1横屏
            isCrop = 1, //裁剪
            cropAspectRatio = 1, //宽高比
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnNativeUrl);
        MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
#if UNITY_EDITOR
        var filePath = Path.Combine(Application.streamingAssetsPath, "signIn_bg.png");
        AlbumResData resData = new AlbumResData();
        resData.localUrl = filePath;
        OnNativeUrl(JsonConvert.SerializeObject(resData));
#endif
    }
    private void OnNativeUrl(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
        AlbumResData authData = JsonConvert.DeserializeObject<AlbumResData>(msg);
        if (string.IsNullOrEmpty(authData.localUrl))
        {
            LoggerUtils.LogError("authData.localUrl is null ", authData.localUrl);
            return;
        }

        UploadImg(authData.localUrl);
    }
    private void UploadImg(string filePath)
    {
        var uri = $"AvatarPartTemplate/{AccountDataManager.Inst.Uid}/{Path.GetFileName(filePath)}";
        CosXmlUploadManager.UploadFile(uri, filePath, (url, err) => { UploadImgCallback(url, err, filePath); });
    }
    private void UploadImgCallback(string url, string err, string filePath)
    {
        if (this==null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(err))
        {
            LoggerUtils.LogError($"Upload Image Fail!!! Err : {err}");
            TipPanel.ShowToast("导入图片失败， 请再试一遍!");
        }
        else
        {

            LoggerUtils.Log("Upload Image Success url: " + url);
            var req = new Dictionary<string, string>()
            {
                {"url", url}
            };
            NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
                HttpMethod.POST, req, rsp =>
                {
                    if (rsp != null && rsp.auditResult == (int) AuditResult.Passed)
                    {
                        currentMusicScoreInfo.cover = url;
                        LoadPhoto();
                    }
                    else
                    {
                        TipPanel.ShowToast("图片审核未通过，请重新上传!");
                    }
                }, null);

        }

    }
    public void LoadPhoto()
    {
        if (string.IsNullOrEmpty(currentMusicScoreInfo.cover))
        {
            return;
        }
        var wrapper = Loader.LoadRemoteImageAsync(currentMusicScoreInfo.cover);
        wrapper.completed += success =>
        {
            if (success)
            {
                var tex = wrapper.RetainAsset(this.gameObject);
                SetCover(tex);
            }
            else
            {
                SetCover(null);
            }
        };
    }

    public void SetCover(Texture tex)
    {
        if (tex)
        {
            rawImage.gameObject.SetActive(true);
            rawImage.texture = tex;
        }
        else
        {
            rawImage.gameObject.SetActive(false);
        }

    }
}
