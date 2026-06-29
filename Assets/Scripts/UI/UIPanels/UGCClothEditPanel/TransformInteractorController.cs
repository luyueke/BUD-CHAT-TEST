using System;
using UnityEngine;

namespace Game.UGCEditor
{
    /// <summary>
    /// Class that controls the Transform Interactors and provide settings to customize the interactors
    /// </summary>
    public class TransformInteractorController : GlobalInstance<TransformInteractorController>
    {
        public const float RestrictX = 600;
        public const float RestrictY = 500;

        public UGCImportInteractor InterActor { get; private set; }
        private Action setUndoRedoAct;
        
        public void InitializeInteractor(Transform par,Action undoRedoAct)
        {
            setUndoRedoAct = undoRedoAct;
            var UIRoot = GameObject.Find("UIRoot");
            var boundingRectanglePrefab =
                XAssetLoaderMgr.Inst.LoadResource<GameObject>(
                    "Assets/Loadable/UI/UIPanel/UGCResourceEditPanel/UGCImportInteractor.prefab", UIRoot);
            InterActor = GameObject.Instantiate(boundingRectanglePrefab).GetComponent<UGCImportInteractor>();
            InterActor.transform.parent = par;
            InterActor.transform.SetAsLastSibling();
            InterActor.transform.localScale = Vector3.one;
            InterActor.transform.localPosition = Vector3.one;
            InterActor.transform.eulerAngles = Vector3.zero;
            InterActor.SetUndoRedoAct = setUndoRedoAct;
            InterActor.Init();
            InterActor.gameObject.SetActive(false);
        }
        

        public void Dispose()
        {
            if (InterActor != null)
            {
                GameObject.Destroy(InterActor);
            }
        }
    }
}
