using System;
using System.Collections;
using System.Collections.Generic;
using Basic.UndoRedo;
using Game.ECS;
using UI.BaseWidgets;
using UndoSystem;
using UnityEngine;

namespace UI.UIWidgets
{
    public class EditTilingView : MonoBehaviour
    {
        private Action _addTileClickCallback;
        private Action _reduceTileClickCallback;
        private Action<Vector2> _undoCallBack;
        
        public CButton AddBtn;
        public CButton ReduceBtn;
        
        //undo/redo相关
        public bool IsUseUndo = true;
        protected SceneEntity selectEntity;
        private Vector2 curTiling = Vector2.one;

        private void Awake()
        {
            AddBtn.onClick.AddListener(OnAddTileClick);
            ReduceBtn.onClick.AddListener(OnReduceTileClick);
        }
        
        public void SetSelectEntity(SceneEntity entity)
        {
            selectEntity = entity;
        }
        
        //将来可以用作显示
        public void SetTiling(Vector2 value)
        {
            curTiling = value;
        }
        

        public void AddTileAddClickListenter(Action callback)
        {
            _addTileClickCallback += callback;
        }

        public void RemoveTileAddClickListener(Action callback)
        {
            _addTileClickCallback -= callback;
        }

        public void AddTileReduceClickListener(Action callback)
        {
            _reduceTileClickCallback += callback;
        }
        
        public void RemoveTileReduceClickListener(Action callback)
        {
            _reduceTileClickCallback -= callback;
        }

        public void OnUndoValue(Vector2 value)
        {
            curTiling = value;
            _undoCallBack?.Invoke(value);
        }

        public void AddUndoListener(Action<Vector2> callback)
        {
            _undoCallBack += callback;
        }

        public void RemoveUndoListener(Action<Vector2> callback)
        {
            _undoCallBack -= callback;
        }

        public void ClearTileChangeListener()
        {
            _addTileClickCallback = null;
            _reduceTileClickCallback = null;
            _undoCallBack = null;
        }
        
        private void HandleTileAction(Action tileAction)
        {
            EditTilingUndoData beginData = null;
            EditTilingUndoData endData = null;
            if (CheckUseUndo())
            {
                beginData = UndoRecordUtils.CreateTilingUndoData(selectEntity, curTiling);
            }

            tileAction?.Invoke();

            if (CheckUseUndo())
            {
                endData = UndoRecordUtils.CreateTilingUndoData(selectEntity, curTiling);
                if (beginData != null && endData != null)
                {
                    UndoRecordUtils.AddTilingRecord(beginData, endData);
                }
            }
        }
        
        
        private void OnAddTileClick()
        {
            HandleTileAction(_addTileClickCallback);
        }

        private void OnReduceTileClick()
        {
            HandleTileAction(_reduceTileClickCallback);
        }

        private bool CheckUseUndo()
        {
            return selectEntity != null && IsUseUndo;
        }
    }
}
