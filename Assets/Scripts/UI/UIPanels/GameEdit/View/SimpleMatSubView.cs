using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.ECS;
using UI.UIWidgets;
using UndoSystem;
using UnityEngine;
using UnityEngine.U2D;

namespace UI.UIPanels.GameEdit
{
    public class SimpleMatSubView : PgcListEditSubView
    {
        [SerializeField] private EditTilingView TilingView;
        
        private Vector2 curTiling;
        
        protected override void OnInit()
        {
             var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.MatIconSprite);
             mSpriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
        }
        
        public void AddTileChangeListener(Action onTileAdd, Action onTileReduce)
        {
            TilingView.AddTileAddClickListenter(onTileAdd);
            TilingView.AddTileReduceClickListener(onTileReduce);
        }
        
        public void AddTileUndoListener(Action<Vector2> tileValue)
        {
            TilingView.AddUndoListener(tileValue);
        }
        
        public void SetTileSettingVisible(bool isEnable)
        {
            if (TilingView.gameObject != null)
            {
                TilingView.gameObject.SetActive(isEnable);
            }
        }
        
        public void SetTiling(Vector2 tileValue)
        {
            curTiling = tileValue;
            TilingView.SetTiling(tileValue);
        }
        
        public override void OnSelectEntity(SceneEntity entity)
        {
            base.OnSelectEntity(entity);
            TilingView.SetSelectEntity(entity);
        }
    }
}
