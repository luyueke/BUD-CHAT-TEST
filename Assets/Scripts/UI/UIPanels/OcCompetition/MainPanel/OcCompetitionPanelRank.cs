using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using GameData.Base;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UnityEngine;

namespace GameUI
{
    public class OcCompetitionPanelRank : MonoBehaviour
    {
        public OcCptWorkItem WorkItem;

        public OcCompetitionPanel Root;

        public OcCompetitionRankAdapter adapter;

        public PullToRefreshBehaviour PullToRefreshBehaviour;
        public void Init(OcCompetitionPanel root)
        {
            Root = root;
            PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
        }

        private void OnEnable()
        {
            OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.RankWork, () => {
                var ls = OcCompetitionSystem.Inst.data.RankListMsg.list;
                SetData(ls);
                if (ls != null  && ls.Count > 0)
                {
                    OnOcSelect(ls[0]);
                }
            });
        }

        public void SetData(List<OcCptListMsgItem> ocList)
        {
            PullToRefreshBehaviour.HideGizmo();
            adapter.OnItemSelected = OnOcSelect;
            adapter.OnItemsUpdated?.Invoke();
            adapter.Data.ResetItems(ocList);
            adapter.Refresh();
        }

        public void OnOcSelect(OcCptListMsgItem ocInfo)
        {
            WorkItem.SetData(ocInfo);
        }

        private void OnPullRefresh()
        {
            OcCompetitionSystem.Inst.OcWorks(OcCompetitionListType.RankWork, () =>
            {
                var ls = OcCompetitionSystem.Inst.data.RankListMsg.list;
                SetData(ls);
                //if (ls != null && ls.Count > 0)
                //{
                //    OnOcSelect(ls[0]);
                //}
            });
        }

        [Button("测试2条")]
        public void Test2()
        {
            OcCptListMsgItem item1 = new OcCptListMsgItem(){
                scoreInfo = new ScoreInfo(){
                    rank = 1,
                    score = 100
                },
                creator = new BaseCreator(){
                    nickname = "测试1"
                },
            };
            OcCptListMsgItem item2 = new OcCptListMsgItem(){
                scoreInfo = new ScoreInfo(){
                    rank = 2,
                    score = 200
                },
                creator = new BaseCreator(){
                    nickname = "测试2"
                },
            };
            SetData(new List<OcCptListMsgItem>(){item1,item2});
        }

        [Button("测试50条")]
        public void Test50()
        {
            List<OcCptListMsgItem> ocList = new List<OcCptListMsgItem>();
            for(int i = 0; i < 50; i++)
            {
                ocList.Add(new OcCptListMsgItem(){
                    scoreInfo = new ScoreInfo(){
                        rank = i + 1,
                        score = i * 100
                    },
                    creator = new BaseCreator(){
                        nickname = "测试" + (i + 1)
                    },
                });
            }
            SetData(ocList);
        }
    }
}