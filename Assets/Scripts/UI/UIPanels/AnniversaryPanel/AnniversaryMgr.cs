using Basic.Utils;
using EventTracking;
using Game.Event;
using GameData.Manager;
using GameUI;
using System.Collections.Generic;


public class AnniversaryMgr : GlobalInstance<AnniversaryMgr>, IActivity
{

    public override void Initialize()
    {
        base.Initialize();

        ActivityManager.Inst.AddActivity(ActivityId.AnniversaryCelebrationCalendar, this);
    }

    public bool IsOpen()
    {
        return true;
    }

    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType>();
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }
    public void ShowPanel()
    {

    }

    public void PreGetAnniversaryInfo()
    {
        IAPDataManager.Inst.GetProductInfo();
        AnniversarySummerMgr.Inst.GetTaskData(); //盛夏之约信息
        AnniversarySummerMgr.Inst.GetActivityDataByHttp(); //盛夏之约 周年赠礼信息

        AnniversaryStoreMgr.Inst.InitData(); //庆典商店内容

        AnniversaryCelePackMgr.Inst.PreGetProductInfo(); //礼包信息
        AnniversaryCelePackMgr.Inst.PreGetTaskData(); //任务信息
        AnniversaryMonthCardMgr.Inst.GetMonthCardInfo(); //月卡信息
        AnniversaryCumulativeMgr.Inst.InitData();//限时累充

        _ = AnniversaryLuckyKoiMgr.Inst; // 幸运锦鲤 初始化
    }


}
