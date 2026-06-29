using Game.Base;
using Game.Props.PropsManagers;
using Game.Scene.ModeController;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameStageMusic : MonoBehaviour
    {
        [HideInInspector] public AIParkGameStagePanel Root;

        public CButton ConfirmBtn;

        public CButton ChooseBtn;
        public Dropdown Drop;

        public List<Toggle> MusicalTog;

        AICommonGameConfig_Stage Config => AIParkGameUgcsetTool.gameConfig.stage;
        public void Init(AIParkGameStagePanel root)
        {
            Root = root;
        }
        private void Start()
        {
            ConfirmBtn.onClick.AddListener(OnConfirmBtn);

            ChooseBtn.onClick.AddListener(OnChooseBtn);
            Drop.onValueChanged.AddListener(OnConditionDrop);

            var ls = Config.musicalInstruments;
            for (int i = 0; i < MusicalTog.Count; i++)
            {
                if (i < ls.Count && ls[i] != null)
                {
                    MusicalTog[i].gameObject.SetActive(true);
                    MusicalTog[i].isOn = true;
                    MusicalTog[i].transform.GetChild(1).GetComponent<Text>().text = ls[i].name;
                    MusicalTog[i].onValueChanged.AddListener((succ) =>
                    {
                        OnToggle(succ, i);
                    });
                }
                else
                {
                    MusicalTog[i].gameObject.SetActive(false);
                }
            }

            var ls2 = new List<Dropdown.OptionData>();
            Drop.ClearOptions();
            foreach (var item in Config.musicUrls)
            {
                ls2.Add(new Dropdown.OptionData(item.musicName));
            }

            Drop.AddOptions(ls2);
            Drop.transform.localScale = Vector3.zero;
            ChooseBtn.gameObject.SetActive(true);
        }

        private void OnToggle(bool bo, int idx)
        {


        }

        private void OnConfirmBtn() {
            Root.behaviour.bgm = Config.musicUrls[Drop.value];
            Root.behaviour.musical.Clear();
            for (int i = 0; i < MusicalTog.Count; i++)
            {
                var item = MusicalTog[i];
                if (item.gameObject.activeSelf && item.isOn && Config.musicalInstruments[i] != null)
                {
                    Root.behaviour.musical.Add(Config.musicalInstruments[i]);
                }
            }
            if (Root.behaviour.musical.Count == 0) 
            {
                TipPanel.ShowToast("至少勾选一个乐器");
            }
            else if (Root.behaviour.musical.Count == 1)
            {
                //一个乐器直接演出
                Root.behaviour.npc.Clear();
                Root.behaviour.npc.Add(AccountDataManager.Inst.UserInfo.uid);
                gameObject.SetActive(false);
                Root.View.gameObject.SetActive(true);
                Root.behaviour.ToStageView();
            }
            else
            {
                var panel = UIManager.Inst.OpenPanel<AIParkConfirmPanel>(PanelId.AIParkConfirmPanel);
                panel.SetData("邀请同伴", "是否需要邀请指定同伴一起演出", "取消", "确定",
                    () =>
                    {
                        Root.behaviour.FillNpc();
                        gameObject.SetActive(false);
                        Root.View.gameObject.SetActive(true);

                        Root.behaviour.ToStageView();
                    },
                    () =>
                    {
                        gameObject.SetActive(false);
                        Root.Npc.gameObject.SetActive(true);

                        var behaviour = Root.behaviour;
                        Root.Npc.SetData(behaviour.musical.Count - 1, (ls) =>
                        {
                            behaviour.npc.Clear();
                            behaviour.npc.Add(AccountDataManager.Inst.UserInfo.uid);
                            behaviour.npc.AddRange(ls);

                            Root.Npc.gameObject.SetActive(false);
                            Root.View.gameObject.SetActive(true);
                            Root.behaviour.ToStageView();
                        });
                    });
            }
        }


        private void OnConditionDrop(int idx)
        {

        }

        private void OnChooseBtn()
        {
            ChooseBtn.gameObject.SetActive(false);
            Drop.transform.localScale = Vector3.one;
            Drop.Show();
        }

    }
}