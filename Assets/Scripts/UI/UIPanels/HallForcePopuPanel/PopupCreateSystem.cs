using System;
using System.Collections.Generic;
using View.UI.PopupPanelSystem.Base.Core;
using View.UI.PopupPanelSystem.Data;
using View.UI.PopupPanelSystem.ExtendsCreators;

namespace View.UI.PopupPanelSystem.Base.SubSystem
{
    public class PopupCreateSystem : BasePopupSystem
    {
        private readonly Dictionary<Type, BasePopupCreator> _creators = new Dictionary<Type, BasePopupCreator>(10);

        public PopupCreateSystem(PopupPanelManager context) : base(context)
        {
            Register();
        }

        public override void Release()
        {
            _creators?.Clear();
        }

        public void ParsePopupDataAndCreate(PopupRspData data, Action callback = null)
        {
            if (data == null)
            {
                LoggerUtils.LogError("Popup data error, skip");
                // Log.E("Popup data error, skip");
                return;
            }

            foreach (var c in _creators.Values)
            {
                c?.ParseDataAndCreatePopup(data);
            }
            
            callback?.Invoke();
        }
        
        private void Register()
        {
            //注意注册的先后顺序，决定了弹出顺序
            _creators.Add(typeof(WebToolPopupCreator), new WebToolPopupCreator(Context));
        }
    }
}