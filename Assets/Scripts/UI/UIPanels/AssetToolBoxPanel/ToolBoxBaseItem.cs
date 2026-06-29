using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;


namespace Game.AssetToolBox
{
    public class ToolBoxBaseItem : MonoBehaviour
    {
        public RemoteImageBehaviour Remote_Cover;
        public CButton Btn_Prase;

        protected ToolBoxItemData _curData;
        protected Action<ToolBoxItemData> _onClickAct;

        private void Awake()
        {
            Btn_Prase.onClick.AddListener(OnBtnClick);
        }

        public virtual void RefreshData(ToolBoxItemData data, Action<ToolBoxItemData> onClickAct)
        {
            this._curData = data;
            this._onClickAct = onClickAct;
            //1.UI刷新
            Remote_Cover.Load(data?.ugcInfo?.cover);
        }

        private void OnBtnClick()
        {
            _onClickAct?.Invoke(this._curData);
        }
    }
}
