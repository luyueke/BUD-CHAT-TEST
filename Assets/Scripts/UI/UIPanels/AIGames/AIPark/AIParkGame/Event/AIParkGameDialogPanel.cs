using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Game.Props;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData.BaseInfo;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameDialogPanel : BasePanel<AIParkGameEventPanel>
    {
        public Text TitleTxt;
        public GameObject Head;
        public Image HeadIcon;
        public RemoteImageBehaviour RM_Cover;

        public Text DescTxt;

        public CButton CloseBtn;

        public List<Image> UgcImage = new();

        public List<Image> UgcImage2 = new();

        public List<Text> Text = new();

        public List<Text> Text2 = new();

        private List<(AICommonGameConfig_NPC, string, string, string)> Items;

        private Action<string, string, string> ShowDialogAc;
        private Action CloseAc;
        private Tweener descTween;

        string _curSoundId;
        GameObject _curPlaySoundGo;
        public string _curEmoteId;
        AIPark_CharacterBehaviour _curNpc;

        public override void OnCreate()
        {
            base.OnCreate();

            foreach (var item in UgcImage)
            {
                item.ParkPopImageColor1();
            }

            foreach (var item in UgcImage2)
            {
                item.ParkPopImageColor2();
            }

            Color color = Color.white;
            if (AIParkGameUgcsetTool.GetParkPopImageColor2(ref color))
            {
                foreach (var item in Text)
                {
                    item.color = color;
                }
            }

            foreach (var item in Text2)
            {
                item.ParkPopTextColor();
            }

            CloseBtn.onClick.AddListener(OnClose);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (args.Length > 0)
            {
                Items = args[0] as List<(AICommonGameConfig_NPC, string, string, string)>;
            }

            RefreshView();
        }

        public void SetEmoteId(string emoteId)
        {
            _curEmoteId = emoteId;
        }

        public void SetShowDialogAc(Action<string, string, string> showDialogAc)
        {
            ShowDialogAc = showDialogAc;
        }
        public void SetCloseAc(Action closeAc)
        {
            CloseAc = closeAc;
        }

        void RefreshView()
        {
            if (Items.Count > 0)
            {
                var item = Items[0];
                //var title = AIGameController.Inst.GetCurAIGame<AIParkGame>().GetNpcName(item.Item2);
                var title = AIPark_NpcUtil.GetName(item.Item2);
                if (string.IsNullOrEmpty(title))
                {
                    TitleTxt.text = "";
                    Head.gameObject.SetActive(false);
                }
                else
                {
                    TitleTxt.text = title;
                    Head.gameObject.SetActive(true);
                    RM_Cover.gameObject.SetActive(false);
                    HeadIcon.gameObject.SetActive(false);
                    string cover = AIPark_NpcUtil.GetHead(item.Item2, HeadIcon);
                    if (!string.IsNullOrEmpty(cover))
                    {
                        RM_Cover.Load(cover);
                        RM_Cover.gameObject.SetActive(true);
                    }
                }
                StopSound();
                if (_curNpc != null)
                {
                    // RevertEmote(_curNpc);
                }

                var npc = AIPark_CharacterManager.Inst.GetNpc(item.Item2);
                if (npc != null)
                {
                    _curPlaySoundGo = npc.gameObject;
                }
                else
                {
                    npc = AIPark_CharacterManager.Inst.GetNpc("0");
                    if (npc != null)
                    {
                        _curPlaySoundGo = npc.gameObject;
                    }
                }
                _curNpc = npc;
                bool doExitEmote = false;
                descTween?.Kill();
                DescTxt.text = "";
                descTween = DescTxt.DOText(item.Item3, item.Item3.Length * 0.1f)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (doExitEmote) return;
                    doExitEmote = true;
                    StopSound();
                    RevertEmote(npc);
                }).OnKill(() =>
                {
                    if (doExitEmote) return;
                    doExitEmote = true;
                    StopSound();
                    RevertEmote(npc);
                    descTween = null;
                });

                ShowDialogAc?.Invoke(item.Item2, item.Item3, item.Item4);
                Items.RemoveAt(0);
            }
        }

        void RevertEmote(AIPark_CharacterBehaviour npc)
        {
            // if(_curNpc == npc) return;
            // npc?._npcAnimController?.SetPlayerAniState(PlayerAniState.Idle,true);
            // npc?.playerStateControllerState.ExitState(PlayerState.SingleEmote);
            if(npc.GetNpcID() == "0")
            {
                return;
            }
            var originEmoteId = AIPark_CharacterManager.Inst.GetOriginStateWhenDiscuss(npc.GetNpcID());
            if (!string.IsNullOrEmpty(originEmoteId) && !AIPark_CharacterManager.Inst.CanEnterServerStateWhenDiscuss(npc.GetNpcID()))
            {
                npc.PlayAnim(originEmoteId);
            }
        }

        private void OnClose()
        {
            //     var npcBehaviour = AIPark_CharacterManager.Inst.GetNpc("1");
            //             npcBehaviour.PlayAnim("40200505");
            //             return;
            // try
            // {
            // AIPark_CharacterManager.Inst.NewResetNpc2DiscussPoint();
            // // AIPark_CharacterManager.Inst.ExitAllFromDiscussPoint();
            // }
            // catch (System.Exception ex)
            // {
            //     LoggerUtils.LogError($"OnClose: {ex.Message}");
            // }
            // Debug.LogError("OnClos11e");
            // // return;
            // // AIPark_CharacterManager.Inst.NewResetNpc2DiscussPoint(LocationType.Stage);

            //          Debug.LogError("OnClos11111e");
            // AIPark_CharacterManager.Inst.ExitAllFromDiscussPoint();
            // return;

            if (Items == null || Items.Count <= 0)
            {
                if (!AIPark_CharacterUtils.Inst.CanContinueOsDialog)
                {
                    return;
                }
                StopSound();
                descTween?.Kill();
                CloseSelf();
                CloseAc?.Invoke();
                CloseAc = null;
                ShowDialogAc = null;
            }
            else
            {
                RefreshView();
            }
        }

        public void PlaySound()
        {
            if (_curPlaySoundGo == null)
            {
                return;
            }
            _curSoundId = "NPC_Voice_Loop"; //测试用 随机的没生效
            AIGameSoundUtils.Inst.PlaySound(_curSoundId, _curPlaySoundGo);
        }

        public void StopSound()
        {
            if (string.IsNullOrEmpty(_curSoundId))
            {
                return;
            }
            AIGameSoundUtils.Inst.StopSound(_curSoundId, _curPlaySoundGo);
            _curSoundId = "";
        }
    }
}