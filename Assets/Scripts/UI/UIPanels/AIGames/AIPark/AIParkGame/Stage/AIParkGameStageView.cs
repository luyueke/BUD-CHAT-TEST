using Com.TheFallenGames.OSA.Util.IO;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameStageView : MonoBehaviour
    {
        [HideInInspector] public AIParkGameStagePanel Root;

        public CButton StageBtn;

        public CButton PeopleBtn;

        public List<CButton> NpcBtn;
        public void Init(AIParkGameStagePanel root)
        {
            Root = root;
        }

        void Start()
        {
            StageBtn.onClick.AddListener(OnStageBtn);

            PeopleBtn.onClick.AddListener(OnPeopleBtn);

            for (int i = 0; i < NpcBtn.Count; i++)
            {
                var idx = i;
                NpcBtn[i].onClick.AddListener(() => { OnNpc(idx); });
            }
        }

        private void OnEnable()
        {
            var panel = UIManager.Inst.FindPanel(WindowId.GuestWindow,PanelId.AIParkGuestPanel) as AIParkGuestPanel;
            panel.HidePanel();
            var npc = Root.behaviour.npc;
            for (int i = 0; i < NpcBtn.Count; i++)
            {
                var item = NpcBtn[i];
                if (i < npc.Count)
                {
                    var img = item.transform.GetChild(1).GetChild(0).GetComponent<Image>();
                    var remote = item.transform.GetChild(1).GetChild(1).GetComponent<RemoteImageBehaviour>();

                    remote.gameObject.SetActive(false);
                    img.gameObject.SetActive(false);

                    var cover = AIPark_NpcUtil.GetHead(npc[i], img);
                    if (!string.IsNullOrEmpty(cover))
                    {
                        remote.Load(cover);
                        remote.gameObject.SetActive(true);
                    }

                    item.gameObject.SetActive(true);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }

        }

        private void OnDisable()
        {
            var panel = UIManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.AIParkGuestPanel) as AIParkGuestPanel;
            panel.ShowPanel();
        }

        private void OnStageBtn() {
            Root.behaviour.ToStageView(false);
        }

        private void OnPeopleBtn()
        {
            Root.behaviour.ToPeopleView();
        }

        private void OnNpc(int idx)
        {
            var npc = Root.behaviour.npc[idx];
            foreach (var item in Root.behaviour.beUse) {
                if (item.Value == npc)
                {
                    Root.behaviour.ToNpcView(Root.behaviour.behaviours.IndexOf(item.Key));
                    return;
                }
            }
       
        }
    }
}