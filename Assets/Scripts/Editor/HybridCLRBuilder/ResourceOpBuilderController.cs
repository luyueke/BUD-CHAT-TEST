using System;
using System.Collections.Generic;
using System.IO;
using BestHTTP;
using EasySpreadsheet;
using Game.Editor;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resource op 平台下载资源
/// </summary>
public class ResourceOpBuilderController
{
    #region 分平台

    private string GetEnvironmentUrl()
    {
#if PACKAGE_TYPE_US
        switch (_curEnvir)
        {
            case UploadEnrEnum.Master:
            default:
                return "https://global.joinbudapp.com/resourceOp/pgcResource/commonList";
            case UploadEnrEnum.Alpha:
                return "https://global.joinbudapp.com/resourceOp/pgcResource/commonList";
            case UploadEnrEnum.Prod:
                return "https://global.joinbudapp.com/resourceOp/pgcResource/commonList";
        }
#else
        switch (_curEnvir)
        {
            case UploadEnrEnum.Master:
            default:
                return "https://api-test.budapp.cn/resourceOp/pgcResource/commonList";
            case UploadEnrEnum.Alpha:
                return "https://api-test.budapp.cn/resourceOp/pgcResource/commonList";
            case UploadEnrEnum.Prod:
                return "https://api.budapp.cn/resourceOp/pgcResource/commonList";
        }
#endif
        
    }

    #endregion

    #region Editor 工具
    
    private static string GetResourceOPStoreUrl(UploadEnrEnum envir)
    {
#if PACKAGE_TYPE_US
        switch (envir)
        {
            case UploadEnrEnum.Master:
                return "https://global.joinbudapp.com/configuration/storeResource";
            case UploadEnrEnum.Prod:
                return "https://global.joinbudapp.com/configuration/storeResource";
            default:return "https://global.joinbudapp.com/configuration/storeResource";
        }
#else
         switch (envir)
        {
            case UploadEnrEnum.Master:
                return "https://api-test.budapp.cn/configuration/storeResource";
            case UploadEnrEnum.Prod:
                return "https://api.budapp.cn/configuration/storeResource";
            default:return "https://api-test.budapp.cn/configuration/storeResource";
        }
#endif
    }

    private static string GetProductJsonPath()
    {
        string productJsonUrl = "Assets/Arts/Config/StoreConfig/product.json";
#if PACKAGE_TYPE_US
        productJsonUrl = "Assets/Arts/Config/StoreConfig/product_us.json";
#endif
        return productJsonUrl;
    }

    private static string GetProductJsonTestPath()
    {
        string productJsonUrl = "Assets/Arts/Config/StoreConfig/product_test.json";
#if PACKAGE_TYPE_US
        productJsonUrl = "Assets/Arts/Config/StoreConfig/product_test_us.json";
#endif
        return productJsonUrl;
    }

    [MenuItem("EasySpreadsheet/ResourceOP/拉取最新表格并生成数据/Master", false, 1)]
    private static void EditorRequestExcelsMaster()
    {
        DoEditorRequestExcel(UploadEnrEnum.Master);
    }

    [MenuItem("EasySpreadsheet/ResourceOP/拉取商城数据/Master", false, 1)]
    private static void CheckStoreDataMaster()
    {
        string requestUrl = GetResourceOPStoreUrl(UploadEnrEnum.Master);
        CheckStoreData(requestUrl, UploadEnrEnum.Master);
    }

    [MenuItem("EasySpreadsheet/ResourceOP/拉取最新表格并生成数据/Prod", false, 1)]
    private static void EditorRequestExcelsProd()
    {
        DoEditorRequestExcel(UploadEnrEnum.Prod);
    }

    [MenuItem("EasySpreadsheet/ResourceOP/拉取商城数据/Prod", false, 1)]
    private static void CheckStoreDataProd()
    {
        string requestUrl = GetResourceOPStoreUrl(UploadEnrEnum.Prod);
        CheckStoreData(requestUrl, UploadEnrEnum.Prod);
    }

    public static void CheckStoreData(string url)
    {
        // 兼容旧调用：默认按 Master 处理
        CheckStoreData(url, UploadEnrEnum.Master);
    }

    public static void CheckStoreData(string url, UploadEnrEnum envir)
    {
        JObject req = new JObject() { ["resourceVersion"] = 0 };
        void ReqCallback(HTTPRequest request, HTTPResponse resp)
        {
            if (resp == null)
            {
                Debug.LogError("商品表拉取失败");
                return;
            }

            Debug.LogError(JsonConvert.SerializeObject(resp.Message));

            if (resp.IsSuccess)
            {
                Debug.Log($"GetCommonList Success: {resp.DataAsText}");
                HttpResponseRawData responseData = JsonConvert.DeserializeObject<HttpResponseRawData>(resp.DataAsText);

                if (responseData.result == 0)
                {
                    var rsp = JsonConvert.DeserializeObject<StoreResourceRsp>(responseData.data.ToString());
                    if (string.IsNullOrEmpty(rsp.resourceData))
                    {
                        Debug.LogError("商品表拉取失败" + rsp);
                        return;
                    }
                    try
                    {
                        string productJsonPath = GetProductJsonPath();
                        string productTestPaht = GetProductJsonTestPath();
                        xasset.Utility.CreateDirectoryIfNecessary(productJsonPath);
                        System.IO.File.WriteAllText(productJsonPath, rsp.resourceData);
                        byte[] bytes = Convert.FromBase64String(rsp.resourceData);
                        var storeData = Product.StoreData.Parser.ParseFrom(bytes);
                        System.IO.File.WriteAllText(productTestPaht, JsonConvert.SerializeObject(storeData));
                        AssetDatabase.Refresh();
                    }
                    catch
                    {
                        Debug.LogError("本地商城远端数据解析失败:" + rsp.resourceVersion + " 字节大小:" + rsp.resourceData.Length);
                    }
                }
                else
                {
                    Debug.LogError("商品表拉取失败");
                }
            }
            else
            {
                Debug.LogError("商品表拉取失败");
            }
        }
        var request = new HTTPRequest(new Uri(url), HTTPMethods.Get, ReqCallback);
        request.SetHeader(HeaderDefine.feature, HeaderDefine.CUR_FEATURE);
        request.SetHeader("environment", envir == UploadEnrEnum.Prod ? "prod" : "master");
        Debug.Log($"GetCommonList: {url}");
        HTTPManager.SendRequest(request);
        
    }

    private static void DoEditorRequestExcel(UploadEnrEnum enrEnum)
    {
        var ctrl = new ResourceOpBuilderController();
        ctrl.StartResourceOpProcedure(enrEnum, (result) =>
        {
            if (!result) return;
            Debug.Log("拉取ResourceOp表格完成");
            ctrl = null;

            SetOpBud();

            //自动导表
            // EsMenu.GenScripts();
            EsMenu.GenData();
        });
    }

    private static void SetOpBud()
    {
        var excel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/_OP_BUD.xlsx");
        var sheet = excel.sheets[0];
        sheet.LoadAllCells();
        Dictionary<string, int> hash = new();

        for (int i = 0, c = sheet.RowCount; i < c; i++)
        {
            hash[sheet.GetCellValue(i, 1)] = i;
        }

        var pgcNameExcel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/_OP_PgcName.xlsx");
        var pgcNameSheet = pgcNameExcel.sheets[0];
        pgcNameSheet.LoadAllCells();
        Dictionary<string, string> pgcNamehash = new();
        for (int i = 0, c = pgcNameSheet.RowCount; i < c; i++)
        {
            pgcNamehash[pgcNameSheet.GetCellValue(i, 1)] = pgcNameSheet.GetCellValue(i, 2);
        }

        var avatarExcel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/_OP_AvatarCommonData.xlsx");
        var avatarSheet = avatarExcel.sheets[0];
        avatarSheet.LoadAllCells();
        var avatarRow = sheet.RowCount;
        for (int i = 0, c = avatarSheet.RowCount; i < c; i++)
        {
            var pgcId = avatarSheet.GetCellValue(i, 1);
            var subType = avatarSheet.GetCellValue(i, 2);
            if (!hash.ContainsKey(pgcId))
            {
                sheet.SetCellValue(avatarRow, 1, pgcId).value = pgcId;
                if(pgcNamehash.ContainsKey(pgcId) && !string.IsNullOrEmpty(pgcNamehash[pgcId]))
                {
                    sheet.SetCellValue(avatarRow, 2, pgcId[0].ToString()).value = pgcNamehash[pgcId];
                }
                else
                {
                    sheet.SetCellValue(avatarRow, 2, pgcId[0].ToString()).value = pgcId[0].ToString();
                }
   
                sheet.SetCellValue(avatarRow, 3, subType).value = subType;
                avatarRow++;
            }
        }


        var petAvatarExcel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/_OP_PetAvatarCommonData.xlsx");
        var petAvatarSheet = petAvatarExcel.sheets[0];
        petAvatarSheet.LoadAllCells();
        var r = sheet.RowCount;
        for (int i = 0, c = petAvatarSheet.RowCount; i < c; i++)
        {
            var pgcId = petAvatarSheet.GetCellValue(i, 1);
            var subType = petAvatarSheet.GetCellValue(i, 2);
            if (!hash.ContainsKey(pgcId))
            {
                sheet.SetCellValue(r, 1, pgcId).value = pgcId;
      
                if (pgcNamehash.ContainsKey(pgcId) && !string.IsNullOrEmpty(pgcNamehash[pgcId]))
                {
                    sheet.SetCellValue(r, 2, pgcId[0].ToString()).value = pgcNamehash[pgcId];
                }
                else
                {
                    sheet.SetCellValue(r, 2, pgcId[0].ToString()).value = pgcId[0].ToString();
                }
                sheet.SetCellValue(r, 3, subType).value = subType;
                r++;
            }
        }

        var emoUIExcel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/EmoUIConfig.xlsx");
        var emoUISheet = emoUIExcel.sheets[0];
        emoUISheet.LoadAllCells();
        var emoUIR = sheet.RowCount;
        for (int i = 2, c = emoUISheet.RowCount; i < c; i++)
        {
            var pgcId = emoUISheet.GetCellValue(i, 1);
            var subType = emoUISheet.GetCellValue(i, 3);
            if (!hash.ContainsKey(pgcId))
            {
                sheet.SetCellValue(emoUIR, 1, pgcId).value = pgcId;
                if (pgcNamehash.ContainsKey(pgcId) && !string.IsNullOrEmpty(pgcNamehash[pgcId]))
                {
                    sheet.SetCellValue(emoUIR, 2, pgcId[0].ToString()).value = pgcNamehash[pgcId];
                }
                else
                {
                    sheet.SetCellValue(emoUIR, 2, pgcId[0].ToString()).value = pgcId[0].ToString();
                }
    
                sheet.SetCellValue(emoUIR, 3, subType).value = subType;
                emoUIR++;
            }
        }

        excel.SaveToFile(Application.dataPath + "/../GameConfig/_OP_BUD.xlsx");
        
        // 添加同步名字的调用
        SyncPgcNameToEmoUI();
    }

    private static void SyncPgcNameToEmoUI()
    {
        Debug.Log("开始同步 PgcName 到 EmoUIConfig");
        
        // 1. 加载 PgcName 表格并创建名字映射
        var pgcNameExcel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/_OP_PgcName.xlsx");
        var pgcNameSheet = pgcNameExcel.sheets[0];
        pgcNameSheet.LoadAllCells();
        
        // 创建 ID 到名字的映射字典
        Dictionary<string, string> nameMap = new();
        for (int i = 2, c = pgcNameSheet.RowCount; i < c; i++)
        {
            var pgcId = pgcNameSheet.GetCellValue(i, 1);  // 假设ID在第一列
            var pgcName = pgcNameSheet.GetCellValue(i, 4); // 假设名字在第二列
            if (!string.IsNullOrEmpty(pgcId))
            {
                nameMap[pgcId] = pgcName;
                //Debug.Log($"读取名字映射: {pgcId} -> {pgcName}");
            }
        }
        
        // 2. 更新 EmoUIConfig 表格
        var emoUIExcel = EsWorkbook.Load(Application.dataPath + "/../GameConfig/EmoUIConfig.xlsx");
        var emoUISheet = emoUIExcel.sheets[0];
        emoUISheet.LoadAllCells();
        
        bool hasChanges = false;
        // 从第2行开始处理（跳过表头）
        for (int i = 2, c = emoUISheet.RowCount; i < c; i++)
        {
            var pgcId = emoUISheet.GetCellValue(i, 1);  // 假设ID在第2列
            if (string.IsNullOrEmpty(pgcId))
            {
                continue;
            }
            
            // 如果找到对应的名字，更新到 EmoUIConfig 表格
            if (nameMap.TryGetValue(pgcId, out string pgcName))
            {
                var currentName = emoUISheet.GetCellValue(i, 2);  // 假设名字在第3列
                if (currentName != pgcName && !string.IsNullOrEmpty(pgcName))
                {
                    emoUISheet.SetCellValue(i, 2, pgcName).value = pgcName;
                    hasChanges = true;
                    Debug.Log($"更新名字: 行 {i}, ID {pgcId}, {currentName} -> {pgcName}");
                }
            }
            else
            {
                Debug.LogWarning($"未找到ID {pgcId} 对应的名字");
            }
        }
        
        // 3. 如果有更改，保存 EmoUIConfig 表格
        if (hasChanges)
        {
            try
            {
                emoUIExcel.SaveToFile(Application.dataPath + "/../GameConfig/EmoUIConfig.xlsx");
                Debug.Log("EmoUIConfig 表格更新完成并保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存 EmoUIConfig 失败: {e.Message}");
            }
        }
        else
        {
            Debug.Log("没有需要更新的名字");
        }
    }

    #endregion


    public class ResourceOpData
    {
        public string pgc_file_url;
        public string avatar_file_url;
        public string pet_avatar_file_url;
        public string pgc_name_file_url;

        public void DoRequestExcels(ResourceOpBuilderController ctr)
        {
            ctr._allFileCount = 2;
            //ctr.DoRequestExcelFile(this.pgc_file_url, Application.dataPath + "/../GameConfig/_OP_BUD.xlsx");
            ctr.DoRequestExcelFile(this.avatar_file_url, Application.dataPath + "/../GameConfig/_OP_AvatarCommonData.xlsx");
            ctr.DoRequestExcelFile(this.pet_avatar_file_url, Application.dataPath + "/../GameConfig/_OP_PetAvatarCommonData.xlsx");
            ctr.DoRequestExcelFile(this.pgc_name_file_url, Application.dataPath + "/../GameConfig/_OP_PgcName.xlsx");
        }
    }

    private Action<bool> _callback;
    private UploadEnrEnum _curEnvir = UploadEnrEnum.Master;
    private int _allFileCount = 0;
    private int _finishedCount = 0;

    public void StartResourceOpProcedure(UploadEnrEnum envir, Action<bool> callback)
    {
        Debug.Log($"开始下载ResourceOP表格: envir:{envir}");
        this._curEnvir = envir;
        this._callback = callback;

        _finishedCount = 0;
        GetAllExcelFileList();
    }

    public void CallNextStep()
    {
        if (_allFileCount <= 0)
        {
            _callback?.Invoke(false);
            _finishedCount = 0;
            _allFileCount = 0;
            return;
        }

        if (_finishedCount >= _allFileCount)
        {
            _callback?.Invoke(true);
            _finishedCount = 0;
            _allFileCount = 0;
        }
    }

    /// <summary>
    /// 获取要从ResourceOP下载的表链接
    /// </summary>
    public void GetAllExcelFileList()
    {
        void ReqCallback(HTTPRequest request, HTTPResponse resp)
        {
            if (resp == null)
            {
                CallNextStep();
                Debug.LogError($"GetCommonList Failed!");
                return;
            }

            if (resp.IsSuccess)
            {
                Debug.Log($"GetCommonList Success: {resp.DataAsText}");
                HttpResponseRawData responseData = JsonConvert.DeserializeObject<HttpResponseRawData>(resp.DataAsText);

                if (responseData.result == 0)
                {
                    ResourceOpData opData = JsonConvert.DeserializeObject<ResourceOpData>(responseData.data.ToString());
                    Debug.Log($"Get resource op data , file_url: {responseData.data}");
                    opData.DoRequestExcels(this);
                }
                else
                {
                    CallNextStep();
                    Debug.LogError($"Get resource op data failed. {responseData.result} , {responseData.rmsg}");
                }
            }
            else
            {
                CallNextStep();
                Debug.LogError($"GetCommonList Failed! StatusCode: {resp.StatusCode},  Message:{resp.Message},  data: {resp.DataAsText}");
            }
        }

        Debug.Log($"GetAllExcelFileList Request url:{GetEnvironmentUrl()}");
        HTTPManager.SendRequest(GetEnvironmentUrl(), HTTPMethods.Get, ReqCallback);
    }

    private void DoRequestExcelFile(string url, string savePath)
    {
        if (string.IsNullOrEmpty(url))
        {
            CallNextStep();
            return;
        }

        void ReqCallback(HTTPRequest request, HTTPResponse resp)
        {
            if (resp.IsSuccess)
            {
                if (resp.Data.Length > 0)
                {
                    File.WriteAllBytes(savePath, resp.Data);
                    Debug.Log($"DoRequestCommonExcelFile save excel to:{savePath}");

                    _finishedCount++;
                    CallNextStep();
                }
            }
            else
            {
                CallNextStep();
                Debug.LogError($"DoRequestCommonExcelFile Failed! StatusCode: {resp.StatusCode},  Message:{resp.Message},  data: {resp.DataAsText}");
            }
        }

        Debug.Log($"DoRequestExcelFile Request url:{url}");
        HTTPManager.SendRequest(url, HTTPMethods.Get, ReqCallback);
    }
}
