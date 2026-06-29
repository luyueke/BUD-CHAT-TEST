using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GravityEngine;
using GravityEngine.Utils;
using GravitySDK.PC.Storage;
using GravitySDK.PC.TaskManager;
using GravitySDK.PC.Utils;

namespace GravitySDK.PC.GravityTurbo
{
    public static class SyncHelper
    {
#if GRAVITY_XIAOMI_GAME_MODE
        // 将小米快游戏的参数获取改为同步
        public static bool GetLaunchInfoSync(out bool success, out Dictionary<string, string> launchQuery, out string launchScene)
        {
            // 初始化 out 参数
            success = false;
            launchQuery = new Dictionary<string, string>();
            launchScene = "";

            // 使用局部变量存储 lambda 内部的结果
            bool localSuccess = false;
            string localLaunchScene = null;
            Dictionary<string, string> localLaunchQuery = null;
            AutoResetEvent waitHandle = new AutoResetEvent(false);

            // 在 lambda 中修改局部变量
            MiBridge.Instance.GetLaunchInfo((success, info) =>
            {
                localSuccess = success;
                if (success)
                {
                    localLaunchScene = info.scene.ToString();
                    localLaunchQuery = new Dictionary<string, string>();
                    if (info.query.type != null)
                    {
                        localLaunchQuery.Add("type", info.query.type);
                    }
                }
                waitHandle.Set(); // 通知主线程继续
            });

            // 阻塞等待回调完成，设置超时（例如 5 秒）
            bool completed = waitHandle.WaitOne(3000);

            // 将局部变量赋值给 out 参数
            if (completed && localSuccess)
            {
                success = localSuccess;
                launchScene = localLaunchScene;
                launchQuery = localLaunchQuery;
                return true;
            }

            return false;
        }
        
        // 将小米快游戏的系统参数获取改为同步
        public static bool GetSystemInfoSync(out bool success, out mi.SystemInfo outData)
        {
            // 初始化 out 参数
            success = false;
            outData = new mi.SystemInfo();

            // 使用局部变量存储 lambda 内部的结果
            bool localSuccess = false;
            mi.SystemInfo localData = null;
            AutoResetEvent waitHandle = new AutoResetEvent(false);

            // 在 lambda 中修改局部变量
            MiBridge.Instance.GetSystemInfo((success, systemData) =>
            {
                localSuccess = success;
                if (success)
                {
                    localData = systemData;
                }
                waitHandle.Set(); // 通知主线程继续
            });

            // 阻塞等待回调完成，设置超时（例如 5 秒）
            bool completed = waitHandle.WaitOne(3000);

            // 将局部变量赋值给 out 参数
            if (completed && localSuccess)
            {
                success = localSuccess;
                outData = localData;
                return true;
            }

            return false;
        }
#endif
    }
}