using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData.UGCData;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public enum InteratorMode
    {
        Normal,
        Transform,
    }

    public class ElementBaseBehaviour : MonoBehaviour
    {
        public bool IsCanSelect { get; set; } = true;
        public RectTransform rectTrans;
        public Button clickArea;
        public Image clickImage;
        public float scalingRatio = 1.5f;
        public float minSize = 370f;
        public int hierarchy; //层级记录

        public Vector3 copyOffset = new Vector3(18, -18, 0);

        //非业务开发逻辑，禁止调用
        private UGCImportInteractor interactor;

        public virtual void OnCreate(Transform mirrorParent)
        {
        }

        public virtual void SetSelfData()
        {
        }

        public virtual void SetData(UGCImportData data)
        {
            
        }

        public virtual void Select()
        {
        }

        public virtual void Replace()
        {
        }

        public virtual void OnTransformChange()
        {
        }

        public virtual GameObject GetDynamicMir()
        {
            return null;
        }

        public virtual void OnCopy()
        {
        }

        public virtual void OnDes()
        {
        }

        public virtual void OnClick()
        {
            if (!IsCanSelect)
            {
                return;
            }
            interactor = TransformInteractorController.Inst.InterActor;
            interactor.ResetInfo();
            interactor.Show(rectTrans, OnTransformChange, Init);
        }

        public virtual GameObject ExcuteMirObj()
        {
            return null;
        }

        public virtual void RedoInfo(int partIndex)
        {
        }

        public virtual void UndoInfo(int partIndex)
        {
        }

        public virtual void SetActive(bool isActive)
        {
            
        }

        public virtual void Init()
        {
            interactor = TransformInteractorController.Inst.InterActor;
            interactor.OnCopyAct = OnCopy;
            interactor.OnDistroyAct = OnDes;
            interactor.OnSelectAct = Select;
            interactor.OnReplace = Replace;
            interactor.OnEndDragAct = EndDrag;
            interactor.OnTransUndoAct = TransUndo;
            interactor.CreatUndoDataAct = CreatUndoData;
            rectTrans.SetSiblingIndex(rectTrans.transform.parent.childCount - 1);
            var mir = GetDynamicMir();
            if (mir)
            {
                mir.transform.SetSiblingIndex(mir.transform.parent.childCount - 1);
            }
        }

        public virtual void EndDrag()
        {
            SetSelfData();
        }

        public virtual void TransUndo()
        {
            OnTransformChange();
            interactor.Show(rectTrans, OnTransformChange, Init);
            UGCImportPhotoManager.Inst.CurrentSelectBehaviour = this;
        }

        public void AddClickEvent()
        {
            clickArea = gameObject.GetComponentInChildren<Button>();
            clickImage = clickArea.GetComponent<Image>();
            if (clickArea != null)
            {
                clickArea.onClick.AddListener(() => { OnClick(); });
            }
        }
        

        public virtual void SetMirSiblingIndex()
        {
        }

        public virtual void CreatUndoData(ElementUndoData data)
        {
        }
    }

}