using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using System.Collections;
using System.Collections.Generic;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using GameData;
using Network.Http;
using Newtonsoft.Json.Linq;
using Network;
using System;

/// <summary>
/// 相册删除上传
/// </summary>
public class CameraAllPhotoPanel : BasePanel<CameraAllPhotoPanel>
{
    public CButton CloseBtn;

    public CButton DeleteAllBtn;

    public CButton UploadAllBtn;

    public Text Des;
    public Text Empty;

    public CButton DeleteBtn;

    public CButton CancelDelBtn;

    public CButton UploadBtn;

    public CButton CancelUpBtn;

    public LoadingButton SureBtn; //参与打卡大赛按钮

    public CameraAllPhotoAdpter Adpter;

    public PullToRefreshBehaviour PullToRefreshBehaviour;

    private readonly List<CameraImagePack> datas = new List<CameraImagePack>();

    private List<CameraImagePack> curSelectList = new List<CameraImagePack>();

    private int _curType = -1;
    private Coroutine _applyPhotoListCo;
    private bool _isUploaded = false;//是否正在上传

    private bool isFromAlbum = true;

    private  CameraImagePack _curInfo = null;

    public override void OnCreate()
    {
        base.OnCreate();
        DeleteAllBtn.onClick.AddListener(() => {
            HideCom();
            DeleteBtn.gameObject.SetActive(true);
            CancelDelBtn.gameObject.SetActive(true);
        });
        UploadAllBtn.onClick.AddListener(() =>
        {
            HideCom();
            UploadBtn.gameObject.SetActive(true);
            CancelUpBtn.gameObject.SetActive(true);
        });
        DeleteBtn.onClick.AddListener(OnDelete);
        CancelDelBtn.onClick.AddListener(Show);
        UploadBtn.onClick.AddListener(OnUpload);
        CancelUpBtn.onClick.AddListener(Show);
        SureBtn.onClick.AddListener(Join);
        CloseBtn.onClick.AddListener(CloseSelf);
        PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
        Adpter.OnItemSelected = OnItemSelected;
        Adpter.IsPackSelected = pack => pack != null && curSelectList.Contains(pack);
        Adpter.Data = new LazyDataHelper<CameraImagePack>(Adpter, GetInfo);
        MessageHelper.AddListener<int>(MessageName.OnAlbumPhotoDataChanged, OnAlbumPhotoDataChanged);
    }

    private ContestInfo _contestData;
    public void SetContestFlag(ContestInfo contestData)
    {
        _contestData = contestData;

    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _contestData = null;
        isFromAlbum = (bool)args[0];

        Show();

        if (isFromAlbum == false)
        {
            _curType = (int)AlbumType.Photo;
        }else
        {
            _curType = (int)args[1];
        }

        CameraImgDataUtils.Inst.GetAllMerged(AccountDataManager.Inst.UserInfo.uid, _curType, OnGetAllMergedComplete);
    }

    protected override void OnDestroy()
    {
        if (_applyPhotoListCo != null)
        {
            StopCoroutine(_applyPhotoListCo);
            _applyPhotoListCo = null;
        }

        base.OnDestroy();
        curSelectList.Clear();
        MessageHelper.RemoveListener<int>(MessageName.OnAlbumPhotoDataChanged, OnAlbumPhotoDataChanged);
    }

    private void OnAlbumPhotoDataChanged(int opt)
    {
        if (!this || !gameObject.activeInHierarchy) return;
        curSelectList.Clear();
        Show();
        CameraImgDataUtils.Inst.GetAllMerged(AccountDataManager.Inst.UserInfo.uid, _curType, OnGetAllMergedComplete);
    }

    private void OnGetAllMergedComplete(List<CameraImagePack> list)
    {
        datas.Clear();
        if (list != null && list.Count > 0)
        {
            // 该面板只支持“照片”的上传/删除，过滤掉视频（mediaType==1）
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item == null) continue;
                if (item.mediaType == 1) continue;
                datas.Add(item);
            }
        }else
        {
            Empty.gameObject.SetActive(true);
            SureBtn.gameObject.SetActive(false);
        }

        if (_applyPhotoListCo != null)
        {
            StopCoroutine(_applyPhotoListCo);
        }

        _applyPhotoListCo = StartCoroutine(CoApplyPhotoListWhenOSAReady());
    }


    private IEnumerator CoApplyPhotoListWhenOSAReady()
    {
        const int maxWaitFrames = 120;
        int frames = 0;
        while (Adpter != null && !Adpter.IsInitialized && frames < maxWaitFrames)
        {
            frames++;
            yield return null;
        }

        _applyPhotoListCo = null;

        if (!this || !gameObject.activeInHierarchy || Adpter == null)
        {
            yield break;
        }

        if (!Adpter.IsInitialized)
        {
            LoggerUtils.LogError("CameraAllPhotoPanel: OSA 未在预期帧数内完成 Init，跳过列表刷新");
            yield break;
        }

        Adpter.Data.ResetItems(datas.Count);
        Adpter.Refresh();
    }

    private CameraImagePack GetInfo(int index)
    {
        return datas[index];
    }

    private void Join()
    {
        bool joinContestDirect = _contestData != null && _contestData.CurrentContestType == BUDContestType.Camera;
        var albumId = _curInfo?.albumId;
       // Debug.LogError($"OnItemSelected info={JsonConvert.SerializeObject(_curInfo)},joinContestDirect={joinContestDirect}");
        if (joinContestDirect)
        {
            if(_curInfo == null)
            {
                TipPanel.ShowToast("请选择一张照片参赛");
                return;
            }
            if (string.IsNullOrEmpty(albumId)) //需要先上传
            {
                SureBtn.ShowLoading();
                CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(_curInfo, data =>
                {
                    PublicPhotoInfo(new List<CameraAlbumInfo>() { data }, 0, (list) =>
                    {
                        if (list?.Count == 1)
                        {
                            albumId = list[0].id;
                            _curInfo.albumId = list[0].id;
                            ContestDataManager.Inst.JoinContest(albumId, new List<string>() { _contestData.contestId }, b =>
                            {
                                if (b)
                                {
                                    SureBtn.HideLoading();
                                    //var panel = UIManager.Inst.OpenPanel<JoinInContestPanel>(PanelId.JoinInContestPanel, new List<ContestInfo>() { _contestData }, albumId);
                                    //panel.ShowJoinSuccess(_contestData);
                                    if (UIManager.Inst.TryFindPanel(PanelId.CameraAllPhotoPanel, out CameraAllPhotoPanel panel4))
                                    {
                                        panel4.CloseSelf();
                                    }

                                    if (UIManager.Inst.TryFindPanel(PanelId.ClothContestPanel, out ContestBaseDetailPanel panel))
                                    {
                                        panel.ShowMyEntry();
                                    }
                                }
                            });
                        }else
                        {
                            SureBtn.HideLoading();
                        }
                    });
                }, err =>
                {
                    SureBtn.HideLoading();
                    LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
                });               
            }else
            {
                ContestDataManager.Inst.JoinContest(albumId, new List<string>() { _contestData.contestId }, b =>
                {
                    if (b)
                    {
                        //var panel = UIManager.Inst.OpenPanel<JoinInContestPanel>(PanelId.JoinInContestPanel, new List<ContestInfo>() { _contestData }, albumId);
                        //panel.ShowJoinSuccess(_contestData);
                        if (UIManager.Inst.TryFindPanel(PanelId.CameraAllPhotoPanel, out CameraAllPhotoPanel panel4))
                        {
                            panel4.CloseSelf();
                        }

                        if (UIManager.Inst.TryFindPanel(PanelId.ClothContestPanel, out ContestBaseDetailPanel panel))
                        {
                            panel.ShowMyEntry();
                        }
                    }
                });
            }
        }
    }

    public void PublicPhotoInfo(List<CameraAlbumInfo> publicPhotoInfo, int opt, Action<List<CameraAlbumInfo>> onComplete = null)
    {
        JObject jObject = new JObject()
        {
            ["list"] = publicPhotoInfo != null ? JToken.FromObject(publicPhotoInfo) : new JArray(),
            ["opt"] = opt,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetAlbum,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                var res = JsonConvert.DeserializeObject<SetAlbumRes>(content);
                onComplete?.Invoke(res.list);

            },
            (error) =>
            {
                onComplete?.Invoke(null);
            });
    }

    private void OnItemSelected(CameraImagePack info)
    {
        _curInfo = info;
      //  Debug.LogError($"OnItemSelected info={JsonConvert.SerializeObject(_curInfo)}");
        if (isFromAlbum == false)
        {
            curSelectList.Clear();
            curSelectList.Add(info);
            Adpter.Refresh();
            return;
        }

        if (curSelectList.Contains(info))
        {
            curSelectList.Remove(info);
        }
        else
        {
            curSelectList.Add(info);
        }
    }

    private void OnPullRefresh()
    {

    }

    void Show() {
        HideCom();
        DeleteAllBtn.gameObject.SetActive(isFromAlbum);
        UploadAllBtn.gameObject.SetActive(isFromAlbum);
        SureBtn.gameObject.SetActive(!isFromAlbum);
        Des.gameObject.SetActive(isFromAlbum);
        Des.text = string.Format("云端剩余空间:{0}张/{1}张", AlbumRequestCtrl.Inst.albumTotal, AlbumRequestCtrl.Inst.albumTotalSlot);
    }

    void HideCom() {
        DeleteAllBtn.gameObject.SetActive(false);
        UploadAllBtn.gameObject.SetActive(false);
        Des.gameObject.SetActive(false);
        DeleteBtn.gameObject.SetActive(false);
        CancelDelBtn.gameObject.SetActive(false);
        UploadBtn.gameObject.SetActive(false);
        CancelUpBtn.gameObject.SetActive(false);
        Empty.gameObject.SetActive(false);
        SureBtn.gameObject.SetActive(false);
    }

    void OnDelete() 
    {
        CommonConfirmPanel commonConfirmPanel =
        UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("提示", "图片删除后无法复原和展示\n在相册，是否继续删除", "确定", "取消");
        commonConfirmPanel.SetOnClickAction(() => { 
            CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(curSelectList, dataList =>
            {
                AlbumRequestCtrl.Inst.curSelectPackList.Clear();
                AlbumRequestCtrl.Inst.curSelectPackList.AddRange(curSelectList);
                AlbumRequestCtrl.Inst.PublicPhotoInfo(dataList, 1, (list, opt) =>
                {
                    CameraImgDataUtils.Inst.DelectPhotoImg(list,opt);
                });
                for(int i = curSelectList.Count - 1; i >= 0; i--)
                {
                    var localDeleted = CameraImgDataUtils.Inst.DeleteLocal(curSelectList[i]);
                    if (!localDeleted)
                    {
                        TipPanel.ShowToast("本地删除失败，请重试");
                    }
                }
            }, err =>
            {
                TipPanel.ShowToast("上传失败，请重试");
                LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
            });
        }, null);
    }

    void OnUpload() 
    {
        if(curSelectList.Count <= 0)
        {
            TipPanel.ShowToast("请选择要上传的图片");
            return;
        }
        for(int i = curSelectList.Count - 1; i >= 0; i--)
        {
            if(curSelectList[i].isCloud)
            {
                curSelectList.RemoveAt(i);
            }
        }
        if(curSelectList.Count <= 0)
        {
            TipPanel.ShowToast("选择图片已上传云端，无需再次上传");
            return;
        }
        if(_isUploaded)
        {
            return;
        }
        _isUploaded = true;
        CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(curSelectList, dataList =>
        {
            AlbumRequestCtrl.Inst.curSelectPackList.Clear();
            AlbumRequestCtrl.Inst.curSelectPackList.AddRange(curSelectList);
            AlbumRequestCtrl.Inst.PublicPhotoInfo(dataList, 0, (list, opt) =>
            {
                CameraImgDataUtils.Inst.PersistUploadOrReuploadToLocal(list, opt);
                _isUploaded = false;
            }, (str) =>
            {
                _isUploaded = false;
                Debug.LogError("上传照片出错:" + str);
            });
        }, err =>
        {
            TipPanel.ShowToast("上传失败，请重试");
            LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
        });
    }

}