using System.Collections.Generic;
using Basic.UndoRedo;
using GameData.UGCData;
using UndoSystem;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public class UGCImportTextManager:UGCImportManager<UGCImportTextManager>
    {
        protected override float textScalingRatio => 3;
        protected override  float minSize => 414f;
        protected override int maxNum => 99;
        
        protected override string overTip => "糟糕! 只能添加99条文本。";
        public List<TextData> GetPartTextDatas(int partIndex)
        {
            List<TextData> pDatas = new List<TextData>();

            if (dataDic.ContainsKey(partIndex))
            {
                dataDic[partIndex].ForEach(behaviour =>
                {
                    behaviour.SetSelfData();
                    var textbehaviour = behaviour as UGCTextBehaviour;
                    pDatas.Add(textbehaviour.data);
                });
            }

            return pDatas;
        }
        
        
        public UGCTextBehaviour CreatEmptyElement(int partIndex, Transform protoParent, Transform mirrorParent, GameObject photoPrefab)
        {
            var behaviour = base.CreatEmptyElement<UGCTextBehaviour>(partIndex, protoParent, mirrorParent, photoPrefab);
            return behaviour;
        }
        
        
        public void SetColor(Color col)
        {
            var selectBehaviour = CurrentSelectBehaviour;
            var textBev = (selectBehaviour as UGCTextBehaviour);
            if (textBev != null)
            {
                var colorBeginData = CreateUndoData(UGCElementType.Color, textBev.rectTrans, FormatUtils.ColorToString(textBev.self.color));
                textBev.SetColor(col);
                var colorEndData = CreateUndoData(UGCElementType.Color, textBev.rectTrans, FormatUtils.ColorToString(textBev.self.color));
                AddRecord(colorBeginData, colorEndData);
            }
        }
        

        public void AddCreateRecord(int type, GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            UGCClothesCreateDestroyUndoData beginData = new UGCClothesCreateDestroyUndoData();
            beginData.targetNode = null;
            beginData.createUndoMode = (int) UndoRedoConfig.CreateUndoMode.Create;
            beginData.type = type;

            UGCClothesCreateDestroyUndoData endData = new UGCClothesCreateDestroyUndoData();
            endData.targetNode = gameObject;
            endData.createUndoMode = (int) UndoRedoConfig.CreateUndoMode.Create;
            endData.type = type;

            UndoRecord record = new UndoRecord(UndoHelperName.UGCClothesCreateDestroyUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
            UndoRedoAct?.Invoke();
        }
        
        private ElementUndoData CreateUndoData(UGCElementType type, RectTransform rectTrans, string color = "")
        {
            ElementUndoData data = new ElementUndoData();
            data.color = color;
            data.targetNode = rectTrans;
            data.transformType = (int)type;
            return data;
        }
        
        public void SetColorUndo(RectTransform targetTrans, Color col)
        {
            if (targetTrans == null) { return; }
            var behav = targetTrans.GetComponent<UGCTextBehaviour>();
            if (behav)
            {
                behav.self.color = col;
                behav.OnTransformChange();
                // TransformInteractorController.Inst.InterActor.Settup(targetTrans, behav.OnTransformChange,behav.Init);
            }
        }

        public void AddRecord(ElementUndoData beginData, ElementUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.UGCClothElementUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
            UndoRedoAct?.Invoke();
        }
    }
}