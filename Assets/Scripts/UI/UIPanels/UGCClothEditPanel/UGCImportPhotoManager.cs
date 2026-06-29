using System;
using System.Collections.Generic;
using System.Linq;
using Basic.UndoRedo;
using Game.Utils;
using GameData.UGCData;
using UndoSystem;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Game.UGCEditor
{
    public class UGCImportPhotoManager:UGCImportManager<UGCImportPhotoManager>,IPool
    {
        protected override float textScalingRatio => 1.5f;
        protected override float minSize => 370f;
        protected override int maxNum => 10;

        protected override string overTip => "糟糕! 只能添加10张图片。";

        public UGCPhotoBehaviour CreatEmptyElement(int partIndex, Transform protoParent, Transform mirrorParent, GameObject photoPrefab)
        {
            var behaviour = base.CreatEmptyElement<UGCPhotoBehaviour>(partIndex, protoParent, mirrorParent, photoPrefab);
            if (behaviour != null)
            {
                behaviour.SetPool(this);
            }
            return behaviour;
        }
        

        public List<PhotoData> GetPartPhotoDatas(int partIndex)
        {
            List<PhotoData> pDatas = new List<PhotoData>();

            if (dataDic.ContainsKey(partIndex))
            {
                dataDic[partIndex].ForEach(behaviour =>
                {
                    behaviour.SetSelfData();
                    var photoBehaviour = behaviour as UGCPhotoBehaviour;
                    pDatas.Add(photoBehaviour.data);
                });
            }

            return pDatas;
        }
        

        public void AddCreateRecord(int type, GameObject go)
        {
            if (go == null)
            {
                return;
            }
            UGCClothesCreateDestroyUndoData beginData = new UGCClothesCreateDestroyUndoData();
            beginData.targetNode = null;
            beginData.createUndoMode = (int) UndoRedoConfig.CreateUndoMode.Create;
            beginData.type = type;

            UGCClothesCreateDestroyUndoData endData = new UGCClothesCreateDestroyUndoData();
            endData.targetNode = go;
            endData.createUndoMode = (int) UndoRedoConfig.CreateUndoMode.Create;
            endData.type = type;

            UndoRecord record = new UndoRecord(UndoHelperName.UGCClothesCreateDestroyUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
            UndoRedoAct?.Invoke();
        }

        
        public string[] GetUrlArr()
        {
            List<string> urlList = new List<string>();
            foreach (var keyValue in dataDic)
            {
                keyValue.Value.ForEach(behaviour =>
                {
                    var photoBehaviour = behaviour as UGCPhotoBehaviour;
                    urlList.Add(photoBehaviour.data.photoUrl);
                });
            }
            return urlList.ToArray();
        }

        public void SetTextureUndo(RectTransform rect, Texture tex, string url)
        {
            var behav = rect.GetComponent<UGCPhotoBehaviour>();
            if (tex != null && behav != null)
            {
                behav.self.texture = tex;
                behav.data.photoUrl = url;
                if (behav.photoDynamicMir)
                {
                    behav.photoDynamicMir.self.texture = tex;
                }

                bool isShow = behav.photoDynamicMir.self.texture != behav.photoDynamicMir.defaultImage ? true : false;
                behav.photoDynamicMir.gameObject.SetActive(isShow);
            }
        }

        private Dictionary<object, object> usePool = new Dictionary<object, object>();
        
        public int Capacity { get; } 

        public object Get(object key)
        {

            if (usePool.ContainsKey(key))
            {
                return usePool[key];
            }
            return null;
        }

        public void Relase(object key)
        {
        }

        public void Put(object key, object value)
        {
            if (usePool.ContainsKey(key))
            {
                usePool[key] = value;
            }
            else
            {
                usePool.Add(key,value);
            }
        }

        public void Clear()
        {
            foreach (var keyValue in usePool)
            {
                if (keyValue.Value != null)
                {
                    UnityEngine.Object.Destroy(keyValue.Value as UnityEngine.Object);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Clear();
        }
    }
}