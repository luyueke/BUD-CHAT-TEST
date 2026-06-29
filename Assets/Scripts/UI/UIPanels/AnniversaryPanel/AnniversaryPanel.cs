using Game.Audio;
using System.Collections;
using System.Collections.Generic;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using System;
using GameUI;

public class AnniversaryPanel : BasePanel<AnniversaryPanel>
{
    public AnniversaryTabItem tabItem;
    List<AnniversaryTabItem> tabItems = new List<AnniversaryTabItem>();
    public GameObject line;
    public Transform tabContont;
    public Transform activytyContont;
    public CButton backBtn;
    List<Es.ActivityConfig> configs;

    public GameObject bg_normal;
    public GameObject bg_summer;

    List<ActivityId> LiveActyvity;

    Dictionary<ActivityId, GameObject> prefabs = new Dictionary<ActivityId, GameObject>();
    //Dictionary<ActivityId, Func<bool>> id2RedDotFuncDict = new()
    //{
    //    {ActivityId.AnniversaryCelebrationGift, AnniversaryCelePackMgr.Inst.IsEntryRedDot},
    //    {ActivityId.AnniversaryCelebrationMonth, AnniversaryMonthCardMgr.Inst.IsEntryRedDot},
    //    {ActivityId.S11GroupConsume, GroupConsumeSystem.Inst.RedDot},
    //};

    void CheckLiveActivity(){
        //上线的周年庆活动
        LiveActyvity ??= new();
        LiveActyvity.Clear();
        //LiveActyvity.Add(ActivityId.AnniversaryCelebrationCalendar);
        //LiveActyvity.Add(ActivityId.AnniversaryCelebrationSummer);
        //LiveActyvity.Add(ActivityId.S11CelebrationStore);
        //if(AnniversaryCelePackMgr.Inst.IsDuringActivity())
        //{
        //    LiveActyvity.Add(ActivityId.AnniversaryCelebrationGift);
        //}
        //if(AnniversaryMonthCardMgr.Inst.IsDuringActivity()){
        //    LiveActyvity.Add(ActivityId.AnniversaryCelebrationMonth);
        //}
        //LiveActyvity.Add(ActivityId.S11GroupConsume);
        //LiveActyvity.Add(ActivityId.AnniversaryEvent);
        //if(AnniversaryLuckyKoiMgr.Inst.IsDuringActivity()){
        //    LiveActyvity.Add(ActivityId.AnniversaryLuckyKoi);
        //}

        LiveActyvity.AddRange(ActivityManager.Inst.GetActivityTypes(ActivityPanelType.AnniversaryPanel));
    }
    public override void OnCreate()
    {
        base.OnCreate();
        backBtn.onClick.AddListener(CloseSelf);
        configs = Es.DataTables.GetActivityConfigList();
        bool isInit = false;
        CheckLiveActivity();
        foreach (var activyty in LiveActyvity)
        {
            if (!isInit)
            {
                isInit = true;
            }
            else
            {
                Instantiate(line, tabContont);
            }
            var config = configs.Find(x => x.id == (int)activyty);
            var item = Instantiate(tabItem, tabContont);
            tabItems.Add(item);
            var red = item.redDot.GetComponent<RedDotNew>();
            var act = ActivityManager.Inst.GetReddotTypes((ActivityId)activyty);
            if (act != null)
            {
                red.customType.AddRange(act);
                red.gameObject.SetActiveValid(true);
            }
            else
            {
                red.gameObject.SetActiveValid(false);
            }
            //if (id2RedDotFuncDict.ContainsKey(activyty))
            //{
            //    item.redDot.SetActive(id2RedDotFuncDict[activyty]());
            //}
            item.Init(config, OnSelectTab);
        }
        OnSelectTab((int)ActivityId.AnniversaryCelebrationCalendar);

        PreGetData();

        MessageHelper.AddListener<ActivityId>(MessageName.AnniversaryPanel_PackRedDot, redDotUpdate);
        MessageHelper.AddListener<int>(MessageName.AnniversaryPanel_TabChange, skip2Tab);
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener<ActivityId>(MessageName.AnniversaryPanel_PackRedDot, redDotUpdate);
        MessageHelper.RemoveListener<int>(MessageName.AnniversaryPanel_TabChange, skip2Tab);
        base.OnDestroy();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        // 播放活动BGM
        AkSoundManager.Inst.PostEvent("Play_Bgm_Hall_S11_Cele", gameObject);
    }

    public override void CloseSelf()
    {
        base.CloseSelf();
        // 停止活动BGM
        AkSoundManager.Inst.PostEvent("Stop_Bgm_Hall_S11_Cele", gameObject);
    }

    void PreGetData()
    {
        AnniversaryMgr.Inst.PreGetAnniversaryInfo();

    }

    void redDotUpdate(ActivityId id)
    {
        //if (id2RedDotFuncDict.ContainsKey(id))
        //{
        //    var item = tabItems.Find(x => x._info.id == (int)id);
        //    if (item != null)
        //    {
        //        item.redDot.SetActive(id2RedDotFuncDict[id]());
        //    }
        //}
        MessageHelper.Broadcast(MessageName.ReddotNotice);
    }

    public void skip2Tab(int id)
    {
        try
        {
            if (!LiveActyvity.Contains((ActivityId)id))
            {
                //未上线 不能跳转
                return;
            }
            OnSelectTab(id);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"skip2Tab id={id} error: " + e.Message);
        }
    }
    void OnSelectTab(int id)
    {
        var config = configs.Find(x => x.id == id);
        foreach (var item in tabItems)
        {
            if (item._info.id == id)
            {
                item.SetSelect(true);
            }
            else
            {
                item.SetSelect(false);
            }
        }
        if (id == (int)ActivityId.AnniversaryCelebrationSummer)
        {
            bg_normal.SetActive(false);
            bg_summer.SetActive(true);
        }
        else
        {
            bg_normal.SetActive(true);
            bg_summer.SetActive(false);
        }

        if (prefabs.ContainsKey((ActivityId)id))
        { //如果已创建
            foreach (var prefab in prefabs)
            {
                if (prefab.Key == (ActivityId)id)
                {
                    prefab.Value.SetActive(true);
                }
                else
                {
                    prefab.Value.SetActive(false);
                }
            }
        }
        else //如果未创建
        {
            if (config.PrefabPath == "")
            {
                Debug.LogError("预制体路径为空， id:" + config.id);
                return;
            }
            GameObject prefab = XAssetLoaderMgr.Inst.LoadResource<GameObject>(config.PrefabPath, gameObject);
            GameObject instance = Instantiate(prefab, activytyContont);
            prefabs[(ActivityId)id] = instance;

            foreach (var prefa in prefabs)
            {
                if (prefa.Key != (ActivityId)id)
                {
                    prefa.Value.SetActive(false);
                }
            }
        }


    }


}