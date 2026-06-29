using System.Collections.Generic;
using View.UI.PopupPanelSystem.Base.Core;

namespace View.UI.PopupPanelSystem.Base.SubSystem
{
    public class PopupImagePreLoaderSystem : BasePopupSystem
    {
        public List<RemoteImageWrapper> proloadImage = new List<RemoteImageWrapper>(10);

        public void AddPreloadImage(string imgUrl)
        {
            var wrapper = Loader.LoadRemoteImageAsync(imgUrl);
            if (wrapper != null)
            {
                proloadImage?.Add(wrapper);
            }
        }

        public PopupImagePreLoaderSystem(PopupPanelManager context) : base(context)
        {
        }

        public override void Release()
        {
            proloadImage?.Clear();
        }
    }
}