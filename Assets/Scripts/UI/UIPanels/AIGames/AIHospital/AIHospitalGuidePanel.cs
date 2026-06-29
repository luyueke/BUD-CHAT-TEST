using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIHospitalGuidePanel : BasePanel<AIHospitalGuidePanel>
    {
        public CButton Btn_Start;
        public Button Btn_Bg;
        public Text Txt_Title1;
        public Text Txt_Title2;

        private string ugcContent = "劳累了一天的你在床上倒下就睡着了，再次睁开眼时，却发现自己身处一座废弃医院当中。\n你不知道发生了什么，甚至分不清这是现实还是梦境，你现在只有一个想法：";
        private string ugcContent1 = "逃离这座医院!";

        private Tweener titleTween1;
        private Tweener titleTween2;

        public override void OnHidden()
        {
            base.OnHidden();
            titleTween1?.Kill();
            titleTween2?.Kill();
        }

        public void SetEnterAct(Action action)
        {
            Btn_Bg.onClick.RemoveAllListeners();
            Btn_Start.onClick.RemoveAllListeners();
            Btn_Start.onClick.AddListener(() =>
            {
                action?.Invoke();
                AIGameSoundUtils.Inst.StopAllBgm();
                AIGameSoundUtils.Inst.StopSound(AIHospitalConfig.GUIDE_BGM);
                CloseSelf();
            });
            Btn_Bg.onClick.AddListener(ForceCompleteTween);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            AIGameSoundUtils.Inst.PlaySound(AIHospitalConfig.GUIDE_BGM);
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Typing_Loop);
            Txt_Title1.text = "";
            Txt_Title2.text = "";

            titleTween1 = Txt_Title1.DOText(ugcContent, ugcContent.Length * 0.1f)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    titleTween2 = Txt_Title2.DOText(ugcContent1, ugcContent1.Length * 0.1f)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
                            Btn_Start.gameObject.SetActive(true);
                        });
                });
        }

        private void ForceCompleteTween()
        {
            // 立即完成第一个文本动画
            if (titleTween1 != null && titleTween1.IsPlaying())
            {
                titleTween1.Complete();
            }

            // 立即完成第二个文本动画
            if (titleTween2 != null && titleTween2.IsPlaying())
            {
                titleTween2.Complete();
            }

            // 确保文本显示完整内容
            Txt_Title1.text = ugcContent;
            Txt_Title2.text = ugcContent1;

            // 停止打字音效
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
            AIGameSoundUtils.Inst.StopSound(AIHospitalConfig.GUIDE_BGM);
            
            // 显示开始按钮
            Btn_Start.gameObject.SetActive(true);
        }
    }
}