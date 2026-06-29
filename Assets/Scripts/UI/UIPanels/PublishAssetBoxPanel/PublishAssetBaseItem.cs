using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using UI.BaseWidgets;
using UnityEngine;


namespace Game.AssetToolBox
{
    public class PublishAssetBaseItem : MonoBehaviour
    {
        public RemoteImageBehaviour Remote_Cover;
        public CButton Btn_Prase;

        protected PropResInfo _curData;
        protected Action<PropResInfo> _onClickAct;

        private void Awake()
        {
            Btn_Prase.onClick.AddListener(OnBtnClick);
        }

        public virtual void RefreshData(PropResInfo data, Action<PropResInfo> onClickAct)
        {
            this._curData = data;
            this._onClickAct = onClickAct;
            //1.UI刷新
            Remote_Cover.Load(data?.propInfo?.cover);
        }

        private void OnBtnClick()
        {
            _onClickAct?.Invoke(this._curData);
        }
    }
}
