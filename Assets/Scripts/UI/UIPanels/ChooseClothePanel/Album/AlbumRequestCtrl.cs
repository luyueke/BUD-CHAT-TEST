using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine;

public class AlbumRequestCtrl : GlobalInstance<AlbumRequestCtrl>
{

    public int albumTotal = 0;//云端照片数
    public int albumTotalSlot = 0;//照片容量
    public List<CameraImagePack> albumPhotoInfoList = new List<CameraImagePack>();//本地持有数据
    public List<CameraImagePack> curSelectPackList = new List<CameraImagePack>();//当前持有数据（用于上传成功后更新数据）
    public void RequestRemoteAlbumPage(string uid, int type, string cookie, int isPublic, Action<AlbumListRes> onComplete, Action<string> onError)
    {
        var jb = new JObject
        {
            ["uid"] = uid,
            ["cookie"] = cookie ?? "",
            ["isPublic"] = 0,//0:所有相册,1:公开相册
            ["type"] = type,//-1:全部,0:照片,1:视频
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AlbumList, HttpMethod.GET, reqParam, (content) =>
        {
            try
            {
                var res = JsonConvert.DeserializeObject<AlbumListRes>(content);
                if (res != null && res.totalSlot > 0)
                {
                    albumTotal = res.total;
                    albumTotalSlot = res.totalSlot;
                }
                onComplete?.Invoke(res);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }, (error) =>
        {
            onError?.Invoke(error);
        });
    }

    /// <summary>
    /// 使用 cookie 分页拉取相册数据，直到 isEnd==1 或 cookie 不再变化。
    /// </summary>
    public void RequestRemoteAlbumAllPages(string uid, int type, int isPublic, Action<AlbumListRes> onComplete, Action<string> onError, int maxPageCount = 10)
    {
        var merged = new List<AlbumPhotoInfo>();
        string cookie = "";
        int page = 0;
        int total = 0;
        int totalSlot = 0;

        Action nextPage = null;
        nextPage = () =>
        {
            if (page >= maxPageCount)
            {
                onComplete?.Invoke(new AlbumListRes { cookie = cookie, isEnd = 1, list = merged, total = total, totalSlot = totalSlot });
                return;
            }

            RequestRemoteAlbumPage(uid, type, cookie, isPublic, pageRes =>
            {
                if (pageRes != null)
                {
                    total = pageRes.total;
                    totalSlot = pageRes.totalSlot;
                    if (pageRes.list != null && pageRes.list.Count > 0)
                    {
                        merged.AddRange(pageRes.list);
                    }
                }

                if (pageRes == null || pageRes.isEnd == 1)
                {
                    onComplete?.Invoke(new AlbumListRes { cookie = cookie, isEnd = 1, list = merged, total = total, totalSlot = totalSlot });
                    return;
                }

                if (string.IsNullOrEmpty(pageRes.cookie) || pageRes.cookie == cookie)
                {
                    onComplete?.Invoke(new AlbumListRes { cookie = cookie, isEnd = 1, list = merged, total = total, totalSlot = totalSlot });
                    return;
                }

                cookie = pageRes.cookie;
                page++;
                nextPage?.Invoke();
            }, error =>
            {
                onError?.Invoke(error);
                // 失败降级：返回已合并到的结果
                onComplete?.Invoke(new AlbumListRes { cookie = cookie, isEnd = 1, list = merged, total = total, totalSlot = totalSlot });
            });
        };

        nextPage?.Invoke();
    }


    public void PublicPhotoInfo(List<CameraAlbumInfo> publicPhotoInfo, int opt, Action<List<CameraAlbumInfo>,int> onComplete = null, Action<string> onError = null)
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
                if (res == null)
                {
                    onError?.Invoke("response parse failed");
                    return;
                }
                onComplete?.Invoke(res.list, opt);
            },
            (error) =>
            {
                curSelectPackList.Clear();
                onError?.Invoke(error);
            });
    }

    public void RequestTaskData(Action<bool, TaskListRsp> resultAction = null)
    {
        string taskId = "AlbumExpansionTask";
        GetTaskListReq getTaskListReq = new GetTaskListReq()
        {
            idList = new List<string>{taskId}
        };
    
        var reqParam = JsonConvert.SerializeObject(getTaskListReq);
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
        {
            var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
            resultAction?.Invoke(true, taskListRsp);
        } , failMessage =>
        {
            resultAction?.Invoke(false, null);
        });
    }

    public void RequestBuyAlbumVolume()
    {
        JObject req = new JObject()
        {
            ["productType"] = 18,
            ["slotType"] = 10,
            ["productId"] = "AlbumSlot_1_10",
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
        {
            BuyProductResult rsp = JsonConvert.DeserializeObject<BuyProductResult>(response);
            TipPanel.ShowToast("购买容量成功，请前往相册查看");
            MessageHelper.Broadcast(MessageName.OnAlbumPhotoDataChanged, -1);
        }, (_) =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();

        });
    }

}
