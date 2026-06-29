using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Game.Audio;
using Game.Props.PropsComponents;
using Pb.Base;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereTVBehaviour: AIPropBaseBehaviour
    {
        private GameObject tvView;
        private Coroutine videoCoroutine;
        private Coroutine videoEndCoroutine;
        private bool isPlayTv = false;
        private Func<bool, bool> _checkStartGame;
        private Animator tvAnimatar;
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIYandereNodeComponent>();
            var temp = GameObject.Find(dataComp.nodeName);
            tvView = temp.transform.GetChild(0).gameObject;
            tvAnimatar = tvView.GetComponent<Animator>();
        }

        public void SetFunctions(Func<bool, bool> func)
        {
            this._checkStartGame = func;
        }
        
        public override void OnTouchClick()
        {
            if (IsCanClick && _checkStartGame(false))
            {
                if (!isPlayTv)
                {
                    tvView.gameObject.SetActive(true);
                    tvAnimatar.Play("tv_open");
                    AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_TV_On);
                }
                else
                {
                    if (videoCoroutine != null)
                    {
                        StopCoroutine(videoCoroutine);
                    }
                    tvAnimatar.Play("tv_close");
                    AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_TV_Off);
                }
                isPlayTv = !isPlayTv;
            }
        }
    }
}