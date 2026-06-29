using GameData.BaseInfo;
using System;
using UnityEngine;

namespace AIGame.Base
{
    public class AIParkUgcEditPaster : MonoBehaviour
    {
        public AIParkPicUpLoad Paster;

        [HideInInspector] public AICommonGamePaster Config;

        public void InitData(Action<AICommonGamePaster, int> _action, int _idx, Action<AICommonGamePaster, int> _delAction, Action<AICommonGamePaster, int> _clickAction)
        {
            Paster.InitData((p, i) => { OnSelectPic(p, i); _action?.Invoke(Config, i); },
                _idx,
                (p, i) => { _delAction?.Invoke(Config, i); Config = null; },
                (p, i) => { _clickAction?.Invoke(Config, i); });
        }

        public void SetDefault(AICommonGamePaster config)
        {
            if (config != null)
            {
                SetData(config);
                Paster.selectAction(config.url, 0);
                Paster.clickAction(config.url, 0);
            }
        }
        public void SetData(AICommonGamePaster config)
        {
            Config = config;
            if (Config != null)
            {
                Paster.SetData(Config.url);
            }
            else
            {
                Paster.SetData("");
            }
        }

        public void OnCancelBtnClick()
        {
            Paster.OnCancelBtnClick();
            Config = null;
        }

        private void OnSelectPic(string _pic, int _idx)
        {
            if (Config == null)
            {
                Config = new AICommonGamePaster();
            }
            Config.url = _pic;
        }
    }
}