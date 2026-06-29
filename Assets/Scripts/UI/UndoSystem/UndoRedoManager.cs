
using System;
using System.Collections.Generic;
using Basic.UndoRedo;
using Game.Base;

namespace UndoSystem
{
    /// <summary>
/// Author:JayWill
/// Description:Undo/Redo管理器，通过从UndoRecordPool获取数据执行undo/redo操作
/// </summary>
    public class UndoRedoManager: GameInstance<UndoRedoManager>,IAutoInit
    {
        public Dictionary<string,BaseUndoHelper> UndoHelperDict = new Dictionary<string,BaseUndoHelper>();

        public UndoRedoManager()
        {
            UndoRecordPool.Inst.AddRecordsRemovedListener(OnRecordsRemoveFromPool);
        }

        public void Init()
        {
            LoggerUtils.Log("UndoRedoManager Init");
        }

        public void Undo()
        {
            if(UndoRecordPool.Inst.GetUndoCount() == 0){
                return;
            }
            UndoRecord record = UndoRecordPool.Inst.PopUndo();
             if(string.IsNullOrEmpty(record.Key)){
                LoggerUtils.LogError("UndoRedoManager UndoRecord.Key is null!");
                return;
            }
            var helper = GetUndoHelper(record.Key);
            helper.Undo(record);
            UndoRecordPool.Inst.PushRedo(record); 
        }

        public void Redo()
        {
            if(UndoRecordPool.Inst.GetRedoCount() == 0){
                return;
            }
            UndoRecord record = UndoRecordPool.Inst.PopRedo();
            if(string.IsNullOrEmpty(record.Key)){
                LoggerUtils.LogError("UndoRedoManager UndoRecord.Key is null!");
                return;
            }
            var helper = GetUndoHelper(record.Key);
            helper.Redo(record);
            UndoRecordPool.Inst.PushUndo(record);
        }

        public void OnRecordsRemoveFromPool(List<UndoRecord> records,int fromType)
        {
            //目前只有删除/创建、组合/取消组合 需要处理
            foreach(var record in records){
                if(record.Key == UndoHelperName.CreateDestroyUndoHelper || record.Key == UndoHelperName.CombineUndoHelper)
                {
                    var helper = GetUndoHelper(record.Key);
                    helper.OnRemoveFromPool(record,fromType);
                }
            }
        }

        public void AddUndoHelper(string helpKey,BaseUndoHelper undoHelper){
            if(!UndoHelperDict.ContainsKey(helpKey))
            {
                UndoHelperDict.Add(helpKey,undoHelper);
            }
        }

        public BaseUndoHelper GetUndoHelper(string helpKey)
        {
            if(UndoHelperDict.ContainsKey(helpKey)){
               return UndoHelperDict[helpKey];
            }else{
                Type type = Type.GetType(helpKey);
                
                if(type != null){
                    BaseUndoHelper helper =  Activator.CreateInstance(type) as BaseUndoHelper;
                    UndoHelperDict.Add(helpKey,helper);
                    return helper;
                }
            }
           return null;
        }

        public override void Release()
        {
            base.Release();
            UndoHelperDict.Clear();
            if (UndoRecordPool.HasInstance)
            {
                UndoRecordPool.Inst.RemoveRecordsRemovedListener(OnRecordsRemoveFromPool);
            }
        }
    }
}
