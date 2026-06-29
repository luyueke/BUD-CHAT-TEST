using System.Collections.Generic;
using Basic;
using Es;
using Game.Audio;
using UI.Preview3D.Bean;
using UI.Preview3D.Helper;
using UI.Preview3D.Mono;
using UnityEngine;
using xasset;

namespace UI.Preview3D.PreviewHandle
{
    public class ThemeSkinBasicHandler : BasicPreviewHandler
    {
        private GameObject _previewProp;
        private PreviewThemeConfig _themeConfig;
        private GameObject _themePrefab;
        // private List<ThemeSkinPreviewMono> _themeSkinPreviewcMonos = new List<ThemeSkinPreviewMono>();

        public override void HandlePreview(Preview3DData data, PreviewWrap wrap)
        {
            _themeConfig = PreviewThemeConfigHelper.GetPreviewThemeConfig(data.PgcIdStr);
            if (_themeConfig == null)
            {
                LoggerUtils.LogError($"ThemeSkinBasicHandler: Can not find theme preview config :{data.PgcIdStr}");
                return;
            }

            base.HandlePreview(data, wrap);
            wrap.CharacterWrap?.Avatar.gameObject.SetActive(false);
            PlayBGM();
            LoadPrefab(wrap);
        }

        public override void CancelPreview(Preview3DData data, PreviewWrap wrap)
        {
            base.CancelPreview(data, wrap);
            wrap.CharacterWrap?.Avatar.gameObject.SetActive(true);
            StopBGM();
            ReleasePrefab();
        }

        #region BGM

        protected virtual void PlayBGM()
        {
            AkSoundManager.Inst.PlayThemeSkinPreviewSound(_themeConfig.Bgm, GlobalCameraManager.Inst.UICamera.gameObject);
        }

        protected virtual void StopBGM()
        {
            AkSoundManager.Inst.StopThemeSkinPreviewSound(GlobalCameraManager.Inst.UICamera.gameObject);
        }

        #endregion


        #region Skin Prefab

        protected virtual void LoadPrefab(PreviewWrap wrap)
        {
            var aotReq = Asset.Load(_themeConfig.PropPrefabPath, typeof(GameObject));
            _themePrefab = GameObject.Instantiate(aotReq.asset as GameObject, wrap.PreviewModelRoot.previewModel);
            
            // _themeSkinPreviewcMonos.Clear();
            // var children = _themePrefab.transform.GetComponentsInChildren<ThemeSkinPreviewMono>();
            // foreach (var pMono in children)
            // {
            //     _themeSkinPreviewcMonos.Add(pMono);
            // }
        }

        protected virtual void ReleasePrefab()
        {
            GameObject.DestroyImmediate(_themePrefab);
        }

        #endregion
    }
}