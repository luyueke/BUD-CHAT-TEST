using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Event;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;

/// <summary>
/// 相册扩容
/// </summary>
public class CameraExpandPanel : BasePanel<CameraExpandPanel>
{
    public CButton CloseBtn;

    public CameraExpandAdpter Adpter;

    public PullToRefreshBehaviour PullToRefreshBehaviour;

    private List<TaskItemData> taskList = new List<TaskItemData>();
    //List<int> datas = new List<int>();

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
        Adpter.OnItemSelected = OnItemSelected;
        Adpter.Data = new LazyDataHelper<TaskItemData>(Adpter, GetInfo);

    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        RequestTaskData();
    }


    private void RequestTaskData()
    {
        AlbumRequestCtrl.Inst.RequestTaskData((success, taskListRsp) =>
        {
            if (!success)
            {
                return;
            }
            if (taskListRsp != null && taskListRsp.list != null && taskListRsp.list.Count > 0)
            {
                TaskInfoData taskInfoData = taskListRsp.list[0];
                taskList = taskInfoData.eventList;
                Adpter.Data.ResetItems(taskList.Count);
                Adpter.Refresh();
            }
            else
            {
                LoggerUtils.LogError("RequestTaskData: taskListRsp is null");
            }
        });
    }

    private TaskItemData GetInfo(int index)
    {
        return taskList[index];
    }

    private void OnItemSelected(TaskItemData info)
    {

    }

    private void OnPullRefresh()
    {

    }
}