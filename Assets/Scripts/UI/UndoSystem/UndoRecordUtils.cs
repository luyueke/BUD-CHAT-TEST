using System;
using System.Collections;
using Basic.UndoRedo;
using Game.Config;
using Game.ECS;
using UI.Manager;
using UnityEngine;

namespace UndoSystem
{
    public static class UndoRecordUtils
    {
        const string TAG = "UndoRecordUtils";

        /// <summary>
        /// 创建记录
        /// </summary>
        public static void AddCreateRecord(GameObject gameObject)
        {
            if(gameObject == null)
            {
                LoggerUtils.LogError($"{TAG} AddCreateRecord gameObject is null!");
                return;
            }
            LoggerUtils.Log($"{TAG} AddCreateRecord {gameObject.name}");
            CreateDestroyUndoData beginData = new CreateDestroyUndoData();
            beginData.targetNode = null;
            beginData.createUndoMode = (int)UndoRedoConfig.CreateUndoMode.Create;

            CreateDestroyUndoData endData = new CreateDestroyUndoData();
            endData.targetNode = gameObject;
            beginData.createUndoMode = (int)UndoRedoConfig.CreateUndoMode.Create;

            UndoRecord record = new UndoRecord(UndoHelperName.CreateDestroyUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public static void AddDestroyRecord(GameObject gameObject)
        {
            if(gameObject == null)
            {
                LoggerUtils.LogError($"{TAG} AddDestroyRecord gameObject is null!");
                return;
            }
            LoggerUtils.Log($"{TAG} AddDestroyRecord {gameObject.name}");
            CreateDestroyUndoData beginData = new CreateDestroyUndoData();
            beginData.targetNode = gameObject;
            beginData.createUndoMode = (int)UndoRedoConfig.CreateUndoMode.Destroy;

            CreateDestroyUndoData endData = new CreateDestroyUndoData();
            endData.targetNode = null;
            beginData.createUndoMode = (int)UndoRedoConfig.CreateUndoMode.Destroy;

            UndoRecord record = new UndoRecord(UndoHelperName.CreateDestroyUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        /// <summary>
        /// 复制记录
        /// </summary>
        public static void AddDuplicateRecord(GameObject gameObject)
        {
            if(gameObject == null)
            {
                LoggerUtils.LogError($"{TAG} AddDuplicateRecord gameObject is null!");
                return;
            }
            LoggerUtils.Log($"{TAG} AddDuplicateRecord {gameObject.name}");
            CreateDestroyUndoData beginData = new CreateDestroyUndoData();
            beginData.targetNode = null;
            beginData.createUndoMode = (int)UndoRedoConfig.CreateUndoMode.Duplicate;

            CreateDestroyUndoData endData = new CreateDestroyUndoData();
            endData.targetNode = gameObject;
            beginData.createUndoMode = (int)UndoRedoConfig.CreateUndoMode.Duplicate;

            UndoRecord record = new UndoRecord(UndoHelperName.CreateDestroyUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        public static void AddUnCombineRecord(object beginData,object endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.CombineUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;

            UndoRecordPool.Inst.PushRecord(record);
        }

        public static void AddLockRecord(bool islock, SceneEntity target)
        {
            if (target == null)
            {
                return;
            }
            LockHideUndoData beginData = new LockHideUndoData();
            beginData.targetNode = target;
            beginData.LockHideType = (int)GameGlobalEnum.LockHideType.Lock;
            beginData.isLock = GizmoManager.Inst.CurGizmoCtrl.GetLockState();
            LockHideUndoData endData = new LockHideUndoData();
            endData.targetNode = target;
            endData.LockHideType = (int)GameGlobalEnum.LockHideType.Lock;
            endData.isLock = islock;
            UndoRecord record = new UndoRecord(UndoHelperName.LockHideUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        public static void AddHideRecord(SceneEntity target)
        {
            if (target == null)
            {
                return;
            }
            LockHideUndoData beginData = new LockHideUndoData();
            beginData.targetNode = target;
            beginData.LockHideType = (int)GameGlobalEnum.LockHideType.Hide;
            beginData.activeSelf = true;
            
            LockHideUndoData endData = new LockHideUndoData();
            endData.targetNode = target;
            endData.LockHideType = (int)GameGlobalEnum.LockHideType.Hide;
            endData.activeSelf = false;
            
            UndoRecord record = new UndoRecord(UndoHelperName.LockHideUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        public static void AddShowAllRecrod()
        {
            LockHideUndoData beginData = new LockHideUndoData();
            beginData.hideList = LockHideManager.Inst.GetHideList();
            beginData.LockHideType = (int)GameGlobalEnum.LockHideType.Show;
            beginData.activeSelf = false;
            
            LockHideUndoData endData = new LockHideUndoData();
            endData.LockHideType = (int)GameGlobalEnum.LockHideType.Show;
            endData.activeSelf = true;
            
            UndoRecord record = new UndoRecord(UndoHelperName.LockHideUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }


        public static EditTilingUndoData CreateTilingUndoData(SceneEntity target,Vector2 tileValue,Type adapterType = null)
        {
            EditTilingUndoData data = new EditTilingUndoData();
            data.tile = tileValue;
            data.targetEntity = target;
            data.adapterType = adapterType;
            return data;
        }

        public static void AddTilingRecord(EditTilingUndoData beginData, EditTilingUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.EditTilingUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }
    }
}