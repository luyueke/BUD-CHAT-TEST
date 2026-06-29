using System;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class SyllableUploadItem : MonoBehaviour
{
    public Image Img_Icon;
    public GameObject Go_Uploaded;
    public CButton Btn_Upload;

    private int _audioLimit = 5;
    private string _upLoadUrl = "";
    private string _atlasPath = "Assets/Loadable/UI/UIPanel/MusicalInstrument/MusicalInstrument.spriteatlas";
    public SyllableType _syllableType;
    private Action<SyllableType, string> _onUploadSyllableAct;

    private void Awake()
    {
        Btn_Upload.onClick.AddListener(OnBtnUploadClick);
    }

    public void SetType(SyllableType type)
    {
        this._syllableType = type;

        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "Syllable_Icon_" + (int)type, gameObject);
        Img_Icon.sprite = sp;
    }

    public void SetUploadSuccessAction(Action<SyllableType, string> act)
    {
        this._onUploadSyllableAct = act;
    }

    public void ClearData()
    {
        this._onUploadSyllableAct = null;
        this._upLoadUrl = "";
        this.Go_Uploaded.SetActive(false);
    }

    private void OnBtnUploadClick()
    {
        bool isCancel = false;
        Action onCancel = ()=>{
            isCancel = true;
        };
        UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
        AlbumUtils.Inst.UploadMusic(_audioLimit, (remoteUrl) => {
            if (isCancel) {
                return;
            }
            if (!string.IsNullOrEmpty(remoteUrl)) {
                OnUploadSyllableSuccess(remoteUrl);
            }
            UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
        }, err => {
            UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
        });
    }

    public void OnUploadSyllableSuccess(string url)
    {
        this._upLoadUrl = url;
        Go_Uploaded.SetActive(true);
        _onUploadSyllableAct?.Invoke(this._syllableType, _upLoadUrl);
    }

    public void RestoreFromCache(string url)
    {
        this._upLoadUrl = url;
        Go_Uploaded.SetActive(true);
    }

    public string GetCurUrl()
    {
        return _upLoadUrl;
    }
}
