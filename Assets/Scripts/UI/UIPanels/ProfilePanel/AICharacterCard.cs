using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class AICharacterCard : BaseCard
    {
        [SerializeField] private int shrinkLimit;
        [SerializeField] private ProfileAICharacterEntry gameEntry;
        [SerializeField] private ProfileAICharacterDataLoader gameLoader;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
        }

        void Start()
        {
            gameEntry.SetLoader(gameLoader);
            gameEntry.SetActions(OnItemClick);
        }

        public void RefreshWithData(string uid, Action<bool> onComplete = null)
        {
            Show(false);
            gameLoader.ToUid = uid;
            gameLoader.FetchPublished(uid, (success, list) =>
            {
                bool hasData = success && list != null && list.Count > 0;
                if (!hasData) { onComplete?.Invoke(false); return; }
                Show(true);
                if (list.Count <= shrinkLimit) Shrink(); else Expand();

                var infos = new List<CabinCharacterUgcInfo>();
                foreach (var d in list)
                    if (d?.characterInfo != null) infos.Add(d.characterInfo);
                gameEntry.InitData(infos);
                onComplete?.Invoke(true);
            });
        }

        private void OnItemClick(CabinCharacterUgcInfo info)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CabinCharacter, info.id);
        }
    }
}
