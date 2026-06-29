using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugSetting : MonoBehaviour
{
#if UNITY_EDITOR
    public enum DebugEnr
    {
        master,
        prod
    }

    public enum DebugMobile
    {
        android,
        ios
    }

    public enum AccountType
    {
        Test,    // 测试账号
        Zyw,     // 开发账号1
        Sxw,     //美术账号
        Boli, //开发账号2
        Input,   //自行输入
    }

    [Serializable]
    public class DebugUserAccount
    {
        public string uid;
        public string token;
        public string openId;
    }

    public DebugEnr environment = DebugEnr.master;
    public DebugMobile mobile = DebugMobile.ios;
    
    public string version = "1.0.0";
    public string HotUpdateVersion = "1.0.0.20";

    // 账号选择
    public AccountType selectedAccount = AccountType.Test;

    // 账号配置
    [SerializeField]
    public Dictionary<AccountType, DebugUserAccount> accountConfigs = new Dictionary<AccountType, DebugUserAccount>()
    {
        {AccountType.Test, new DebugUserAccount()
        {
            uid = "1815301222400552960",
            token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjIwNjA3NjMzNTYsImlhdCI6MTc0NTQwMzM1NiwiaXNzIjoiUlZGZWZYSGFOSnpHN21sdFhVc1NOZDY4dEJ6dzcxeEYiLCJuYmYiOjE3NDU0MDMzNTYsInBsIjoiZXlKMWNuWWlPaUlpTENKeWJDSTZNQ3dpWTJraU9pSWlMQ0oyYVhBaU9qQXNJbmQwSWpvd2ZRPT0iLCJ1aWQiOiIxODE1MzAxMjIyNDAwNTUyOTYwIn0.DQcsOFai-aMsQK8Zj2QISgkT2B1s44XO0aQeVnYzBcc",
            openId = "000626.114287c5903e4b11b03a4035e9dafa4f.1020",
        } },
        {AccountType.Zyw, new DebugUserAccount()
        {
            uid = "1897205010061262848",
            token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQ4OTgyMTQ3OTksImlhdCI6MTc0NDYxNDc5OSwibmJmIjoxNzQ0NjE0Nzk5LCJ1aWQiOiIxODk3MjA1MDEwMDYxMjYyODQ4In0.omfnfblFCwO2FTakc-ixPPIi3JDi5AxaMGiyryihQIs",
            openId = "2025030503654148",
        } },
        {AccountType.Sxw, new DebugUserAccount()
        {
            uid = "1815301273902411776",
            token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQ5MDA2MzAwMjUsImlhdCI6MTc0NzAzMDAyNSwibmJmIjoxNzQ3MDMwMDI1LCJ1aWQiOiIxODE1MzAxMjczOTAyNDExNzc2In0.AX8EB6W6KL0wtqDBSho4Qr6olNePK8kY3VrkuBhvZcE",
            openId = "001011.3bf41351229a43b8af0aaf58cf985bdb.1230",
        } },
         {AccountType.Boli, new DebugUserAccount()
        {
            uid = "1938167972430651392",
            token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQ5MDA2MzAwMjUsImlhdCI6MTc0NzAzMDAyNSwibmJmIjoxNzQ3MDMwMDI1LCJ1aWQiOiIxODE1MzAxMjczOTAyNDExNzc2In0.AX8EB6W6KL0wtqDBSho4Qr6olNePK8kY3VrkuBhvZcE",
            openId = "wx_obKbE0rvkpqwaaz61zmBcIh3yc3I",
        } },
        {AccountType.Input, new DebugUserAccount()
        {
            
        } },
    };

    // 当前使用的账号
    [SerializeField]
    public DebugUserAccount userAccount;

    [SerializeField]
    public bool isShowTempVehicle = false;

    public static DebugSetting Inst;

    public void Awake()
    {
        Inst = this;
        GameObject.DontDestroyOnLoad(gameObject);
    }

    private void OnValidate()
    {
        // 切换选择时更新当前账号
        if(accountConfigs.ContainsKey(selectedAccount) && selectedAccount!= AccountType.Input)
        {
            userAccount = accountConfigs[selectedAccount];
        }
    }
#endif
}
