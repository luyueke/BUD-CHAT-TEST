using Es;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;

public class RedDotManager : GlobalInstance<RedDotManager>
{
    private readonly string RedDotPath = "Assets/Arts/Prefabs/RedDot/";

    private List<IRedDot> redDotList;
    private List<RedDotUIConfig> redDotUIConfigList;
    private List<LocalRedDotData> redDotDataList;
    private List<RedDotBindConfig> redDotBindConfigList;

    private Dictionary<string, Transform> openPanelDic;

    #region private

    public void Init()
    {
        LoadConfig();

        UIManager.Inst.AddOpenPanelAction(OnOpenPanel);
        UIManager.Inst.AddClosePanelAction(OnClosePanel);

        MessageHelper.AddListener(MessageName.LoginSuccess, GetRedDotData);
        MessageHelper.AddListener<string, int, int>(MessageName.AddRedDot, AddNewRedDot);
        MessageHelper.AddListener<int, string>(MessageName.RemoveRedDot, TriggerRedDot);
    }

    public override void Release()
    {
        base.Release();

        MessageHelper.RemoveListener(MessageName.LoginSuccess, GetRedDotData);
        MessageHelper.RemoveListener<string, int, int>(MessageName.AddRedDot, AddNewRedDot);
        MessageHelper.RemoveListener<int, string>(MessageName.RemoveRedDot, TriggerRedDot);
    }

    /// <summary>
    /// 获取红点配置及当前账号红点数据
    /// </summary>
    private void LoadConfig()
    {
        openPanelDic = new Dictionary<string, Transform>();
        redDotList = new List<IRedDot>();
        redDotDataList = new List<LocalRedDotData>();

        redDotUIConfigList = DataTables.GetRedDotUIConfigList();
        redDotBindConfigList = DataTables.GetRedDotBindConfigList();
    }

    private void OnOpenPanel(BasePanel panel)
    {
        if (openPanelDic.ContainsKey(panel.name))
        {
            LoggerUtils.Log("重复调用OpenPanel : " , panel.name);
            openPanelDic[panel.name] = panel.transform;
        }
        else
        {
            openPanelDic.Add(panel.name, panel.transform);
        }

        CheckPanelRedDot(panel.transform);
    }

    private void OnClosePanel(BasePanel panel)
    {
        if (openPanelDic.ContainsKey(panel.name))
            openPanelDic.Remove(panel.name);
        else
            LoggerUtils.Log("重复调用ClosePanel : " , panel.name);
    }

    /// <summary>
    /// 从服务端获取红点数据
    /// </summary>
    private void GetRedDotData()
    {
        return;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getRedDot, HttpMethod.GET, null, response =>
        {
            var res = JsonConvert.DeserializeObject<RedDotData>(response);

            if (res.pgcReddot != null && res.pgcReddot.idList != null)
            {
                foreach (var pgcId in res.pgcReddot.idList)
                {
                    //redDotDataList.Add(GetPGCItemData(pgcId));
                    AddNewRedDot(pgcId);
                }
            }

            if (res.ugcReddot != null && res.ugcReddot.list != null)
            {
                foreach (var ugcRedDot in res.ugcReddot.list)
                {
                    AddNewRedDot(ugcRedDot.id, ugcRedDot.type, ugcRedDot.redDotNum);
                }
                //redDotDataList.AddRange(res.ugcReddot.list);
            }

        }, fail =>
        {
            LoggerUtils.LogError($"获取红点数据失败 : {fail}");
        });
    }

    /// <summary>
    /// 创建红点
    /// </summary>
    /// <param name="redDotParent"></param>
    /// <param name="redDotUI"></param>
    /// <param name="data"></param>
    private void CreateRedDot(Transform redDotParent, RedDotUIConfig redDotUI, LocalRedDotData data)
    {
        IRedDot redDot = redDotUI.isStatic ? redDotParent.GetComponentInChildren<IRedDot>(true) : null;

        if (redDot == null)
        {
            var redDotRes = Loader.Load<GameObject>(RedDotPath + redDotUI.prefabName + ".prefab", redDotParent.gameObject);
            var redDotGo = GameObject.Instantiate(redDotRes, redDotParent);
            redDotGo.transform.localPosition = redDotUI.localPosition;

            redDot = redDotGo.GetComponent<IRedDot>();
            redDot.SetData(data, OnReleaseRedDot);
            if (redDotUI.isTrigger)
            {
                redDot.AddClickAction();
            }

            redDotList.Add(redDot);
        }
        else
        {
            redDot.AddData(data);
        }
    }

    /// <summary>
    /// 释放红点
    /// </summary>
    /// <param name="redDot"></param>
    private void OnReleaseRedDot(IRedDot redDot)
    {
        if (redDotList.Contains(redDot))
        {
            redDotList.Remove(redDot);
        }
    }

    /// <summary>
    /// 检查当前界面是否存在红点
    /// </summary>
    /// <param name="panel"></param>
    private void CheckPanelRedDot(Transform panel)
    {
        if (redDotDataList == null || redDotDataList.Count == 0) return;

        string panelName = panel.name.Replace("(Clone)", "");

        var curPanelRedDotList = redDotUIConfigList.FindAll(uiConfig => panelName.Equals(uiConfig.panelName));
        if (curPanelRedDotList == null || curPanelRedDotList.Count == 0) return;

        foreach (var redDotData in redDotDataList)
        {
            var bindConfig = GetBindConfig(redDotData);

            var showRedDot = curPanelRedDotList.Find(uiConfig => bindConfig.bindRedDotUI.Contains(uiConfig.redDotUIID));
            if (showRedDot != null)
            {
                if (!showRedDot.isStatic) continue;

                var redDotParent = GameObjectEx.FindChildByName(panel.transform, showRedDot.parentPath);
                if (redDotParent != null)
                {
                    CreateRedDot(redDotParent, showRedDot, redDotData);
                }
            }
        }
    }

    /// <summary>
    /// 根据红点数据显示关联的红点UI
    /// </summary>
    /// <param name="newRedDotData"></param>
    private void ShowBindRedDot(LocalRedDotData newRedDotData)
    {
        var bindConfig = GetBindConfig(newRedDotData);
        if (bindConfig != null && bindConfig.bindRedDotUI != null)
        {
            for (int i = 0; i < bindConfig.bindRedDotUI.Count; i++)
            {
                var redDotUI = redDotUIConfigList.Find(uiConfig => bindConfig.bindRedDotUI[i] == uiConfig.redDotUIID);

                if (redDotUI != null)
                {
                    string panelName = redDotUI.panelName + "(Clone)";
                    if (openPanelDic.ContainsKey(panelName) && redDotUI.isStatic)
                    {
                        var redDotParent = GameObjectEx.FindChildByName(openPanelDic[panelName], redDotUI.parentPath);
                        if (redDotParent != null)
                        {
                            CreateRedDot(redDotParent, redDotUI, newRedDotData);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 删除红点数据
    /// 如果当前节点上没有红点时彻底销毁红点
    /// </summary>
    /// <param name="data"></param>
    private void RemoveRedDot(LocalRedDotData data)
    {
        for (int i = redDotList.Count - 1; i >= 0; i--)
        {
            if (redDotList[i].RemoveRedDotData(data))
            {
                redDotList.RemoveAt(i);
            }
        }

        redDotDataList.Remove(data);
    }

    /// <summary>
    /// 获取当前节点Panel下的相对路径
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private string GetTransformPath(Transform transform, string path = "")
    {
        if (transform.parent == null || transform.parent.GetComponent<BasePanel>() != null)
        {
            return path.Remove(0, 1);
        }

        path = "/" + transform.parent.name + path;

        return GetTransformPath(transform.parent, path);
    }

    /// <summary>
    /// 获取关联的UI
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private RedDotBindConfig GetBindConfig(LocalRedDotData data)
    {
        var bindConfig = redDotBindConfigList.Find(bindConfig => bindConfig.type == data.type);

        return bindConfig;
    }

    /// <summary>
    /// 根据PGC ID 获取type
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private LocalRedDotData GetPGCItemData(string id)
    {
        var redDotData = new LocalRedDotData();
        redDotData.id = id;
        redDotData.type = DataTables.GetGameResData(id).SubType;
        redDotData.redDotNum = 0;

        return redDotData;
    }
    #endregion

    // 动态需要缓存，等新增data时需要把动态的确认一次新增type属不属于当前动态节点上

    /// <summary>
    /// 检查是否添加红点（用于动态添加type）
    /// </summary>
    /// <param name="redDotParent"></param>
    /// <param name="types"></param>
    public void CheckRedDot(Transform redDotParent, params int[] types)
    {
        if (types == null || types.Length == 0) return;

        IRedDot redDot = redDotParent.GetComponentInChildren<IRedDot>(true);
        if (redDot != null)
        {
            redDot.ClearRedDot();
            redDotList.Remove(redDot);
        }

        if (this.redDotDataList == null || this.redDotDataList.Count == 0) return;

        List<LocalRedDotData> allTypeDataList = new List<LocalRedDotData>();
        for (int i = 0; i < types.Length; i++)
        {
            var typeDataList = this.redDotDataList.FindAll(redDotData => redDotData.type == types[i]);
            if (typeDataList != null && typeDataList.Count > 0)
            {
                allTypeDataList.AddRange(typeDataList);
            }
        }

        string transformPath = GetTransformPath(redDotParent);
        var redDotUIData = redDotUIConfigList.Find(uiConfig => transformPath.Contains(uiConfig.parentPath));

        if (transformPath != null && redDotUIData != null)
        {
            foreach (var redDotData in allTypeDataList)
            {
                CreateRedDot(redDotParent, redDotUIData, redDotData);
            }
        }
    }

    /// <summary>
    /// 检查是否添加红点（用于动态添加item）
    /// </summary>
    /// <param name="type"></param>
    /// <param name="itemId"></param>
    /// <param name="redDotParent"></param>
    public void CheckRedDot(int type, string itemId, Transform redDotParent)
    {
        IRedDot redDot = redDotParent.GetComponentInChildren<IRedDot>(true);
        if (redDot != null)
        {
            redDot.ClearRedDot();
            redDotList.Remove(redDot);
        }

        if (redDotDataList == null || redDotDataList.Count == 0) return;

        var redDotData = redDotDataList.Find(redDotData => redDotData.type == type && redDotData.id == itemId);
        if (redDotData == null) return;

        string transformPath = GetTransformPath(redDotParent);
        var redDotUIData = redDotUIConfigList.Find(uiConfig => transformPath.Contains(uiConfig.parentPath));

        if (transformPath != null && redDotUIData != null)
        {
            CreateRedDot(redDotParent, redDotUIData, redDotData);
        }
    }

    /// <summary>
    /// 触发红点
    /// </summary>
    /// <param name="type"></param>
    /// <param name="itemId"></param>
    public void TriggerRedDot(int type, string itemId)
    {
        var triggerData = redDotDataList.Find(data => data.type == type && data.id == itemId);

        if (triggerData != null)
        {
            RemoveRedDot(triggerData);
        }
    }

    /// <summary>
    /// 新增红点数据
    /// </summary>
    /// <param name="type"></param>
    /// <param name="itemId"></param>
    public void AddNewRedDot(string id, int type = -1, int redDotNum = 1)
    {
        LocalRedDotData newRedDotData;

        if (type < 0)
        {
            newRedDotData = GetPGCItemData(id);
            newRedDotData.isPGC = true;
        }
        else
        {
            newRedDotData = new LocalRedDotData();
            newRedDotData.id = id;
            newRedDotData.type = type;
            newRedDotData.redDotNum = redDotNum;
            newRedDotData.isPGC = false;
        }

        redDotDataList.Add(newRedDotData);

        ShowBindRedDot(newRedDotData);
    }
}