using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityEngine;

public class InitializeCallbackImpl : IInitializeCallback
{
    public void onFailed(string errorMsg)
    {
        Debug.Log("gravity initialize failed  with message " + errorMsg);
    }

    public void onSuccess(Dictionary<string, object> responseJson)
    {
        Debug.Log("gravity initialize success");
        if (responseJson != null)
        {
            Debug.Log("gravity initalize response :" + responseJson.ToString());
        }
        // 建议在此执行一次Flush
        GravityEngineAPI.Flush();
    }
}
public class GetOpenIdCallbackImpl : IGetOpenIdCallback
{
    public void onFailed(string errorMsg)
    {
        Debug.Log("gravity getOpenId failed  with message " + errorMsg);
    }

    public void onSuccess(Dictionary<string, object> responseJson)
    {
        Debug.Log("gravity getOpenId success");
        if (responseJson != null)
        {
            Dictionary<string, object> dataDict = (Dictionary<string, object>)responseJson;
            foreach (var kvp in dataDict)
            {
                Debug.Log("gravity key " + kvp.Key + " : " + kvp.Value?.ToString());
            }
        }
    }
}

public class ResetClientIdCallbackImpl : IResetCallback
{
    public void onFailed(string errorMsg)
    {
        Debug.Log("gravity reset failed  with message " + errorMsg);
    }

    public void onSuccess()
    {
        Debug.Log("gravity reset success");
        // 建议在此执行一次Flush
        GravityEngineAPI.Flush();
    }
}

public class QueryDryRunCallbackImpl : IQueryDryRunCallback
{
    public void onFailed(string errorMsg)
    {
        Debug.Log("gravity query failed  with message " + errorMsg);
    }

    public void onEmpty()
    {
        // 当前未查询到需要给媒体上报的付费事件
    }

    public void onTrackPay(int backValue, string company, Dictionary<string, object> otherParams)
    {
        // 可以从otherParams中解析自己传入的参数使用
        if (company.Equals("bytedance"))
        {
            // 2. 调用巨量的上报付费的方法，传入金额backValue
            // 客户实现
        }
        else if (company.Equals("tencent"))
        {
            // 2. 调用腾讯的上报付费的方法，传入金额backValue
            // 客户实现
        }
    }

    public void onTrackKeyActive(string company, Dictionary<string, object> otherParams)
    {
        // 可以从otherParams中解析自己传入的参数使用
        if (company.Equals("bytedance"))
        {
            // 2. 调用巨量的上报关键行为的方法
            // 客户实现
        }
        else if (company.Equals("tencent"))
        {
            // 2. 调用腾讯的上报关键行为的方法
            // 客户实现
        }
    }
}

public class LogoutCallbackImpl : ILogoutCallback
{
    public void onCompleted()
    {
        Debug.Log("gravity logout callback");
    }
}
