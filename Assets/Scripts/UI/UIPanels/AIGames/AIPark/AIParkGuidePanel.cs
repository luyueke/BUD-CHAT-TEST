using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.MapSetting;
using GameData.Manager;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGuidePanel : BasePanel<AIParkGuidePanel>
    {
        public CButton Btn_Start;
        public Button Btn_Bg;
        public Text Txt_Title1;
        public Text Txt_Title2;
        public Text Txt_CustomContent;


        public const string ugcContent = "这是一座神秘而又梦幻的游乐园。游乐园中只有一群能够自由活动的玩偶，他们在这里享受生活，少有外人来扰。";
        public const string ugcContent1 = "然而，最近几天，一系列恼人又诡异的恶作剧打破了这份平静.....";

        string content1 = "";
        string content2 = "";

        bool isCustomContent = false;

        private Tweener titleTween1;
        private Tweener titleTween2;
        public Transform loadTrans;
        bool _writeEnd = false;

        public bool isWaitNextSceneData = false;

        public override void OnHidden()
        {
            base.OnHidden();
            titleTween1?.Kill();
            titleTween2?.Kill();
            titleTween1 = null;
            titleTween2 = null;
        }

        public void SetEnterAct(Action action)
        {
            Btn_Bg.onClick.RemoveAllListeners();
            Btn_Start.onClick.RemoveAllListeners();
            Btn_Start.onClick.AddListener(() =>
            {
                titleTween1?.Kill();
                titleTween2?.Kill();
                action?.Invoke();
                // AIGameSoundUtils.Inst.StopSound(AIParkConfig.GUIDE_BGM);
                // AIGameSoundUtils.Inst.StopAllBgm();
                // var ugc = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.bgMusicUrl;
                // if (!string.IsNullOrEmpty(ugc))
                // {
                //     BgMusicManager.Inst.SetUGCMusic(ugc);
                //     BgMusicManager.Inst.PlayUGCMusic();
                // }
                // else
                // {
                //     AIGameSoundUtils.Inst.PlayBgm(AIParkConfig.Bgm_S11Para_MainScene);
                // }
                // CloseSelf();
            });
            Btn_Bg.onClick.AddListener(ForceCompleteTween);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            if (args.Length > 0)
            {
                content1 = args[0] as string;
                if (string.IsNullOrEmpty(content1))
                {
                    content1 = "";
                }
                isCustomContent = true;
            }
            else
            {
                if (AIParkUtils.Inst.ParkGameData.isPgcEnter)
                {
                    content1 = ugcContent;
                }
                else
                {
                    content1 = AIParkUtils.Inst.ParkGameData.aICommonGameConfig.plot;
                    if (string.IsNullOrEmpty(content1))
                    {
                        content1 = "";
                    }
                }
                isCustomContent = false;
            }
            Btn_Start.gameObject.SetActive(false);
            loadTrans.gameObject.SetActive(false);
            BeginWriteTxt();
            // AIGameSoundUtils.Inst.PlaySound(AIParkConfig.GUIDE_BGM);
        }

        public void SetWaitNextSceneDataState(bool isWait)
        {
            Debug.Log("乐园SetWaitNextSceneDataState isWaitNextSceneData = " + isWait);

            isWaitNextSceneData = isWait;
        }

        public void ReceiveNextSceneData()
        {
            SetWaitNextSceneDataState(false);
            if (_writeEnd)
            {
                SetBtnState();
            }
        }



        public void AddCustomContent(string content)
        {
            content1 = content;
            if (string.IsNullOrEmpty(content1))
            {
                content1 = "";
            }
            isCustomContent = true;
            BeginWriteTxt();
            Btn_Start.gameObject.SetActive(false);
            Btn_Bg.gameObject.SetActive(true);
        }



        private void BeginWriteTxt()
        {
            titleTween2?.Kill();
            titleTween2 = null;
            titleTween1?.Kill();
            titleTween1 = null;
            AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Typing_Loop);

            // 安全地设置文本内容
            if (Txt_Title1 != null) Txt_Title1.text = "";
            if (Txt_Title2 != null) Txt_Title2.text = "";
            if (Txt_CustomContent != null) Txt_CustomContent.text = "";

            if (isCustomContent)
            {
                if (Txt_CustomContent != null)
                {
                    titleTween1 = Txt_CustomContent.DOText(content1, content1.Length * 0.1f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        // 检查Text组件是否仍然存在
                        if (Txt_CustomContent != null)
                        {
                            _writeEnd = true;
                            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
                            SetBtnState();
                        }
                        titleTween1 = null;
                    });
                }
            }
            else
            {
                if (AIParkUtils.Inst.ParkGameData.isPgcEnter)
                {
                    if (Txt_Title1 != null)
                    {
                        titleTween1 = Txt_Title1.DOText(ugcContent, ugcContent.Length * 0.1f)
                           .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                titleTween1 = null;
                                if (Txt_Title2 != null)
                                {
                                    titleTween2 = Txt_Title2.DOText(ugcContent1, ugcContent1.Length * 0.1f)
                                        .SetEase(Ease.Linear)
                                        .OnComplete(() =>
                                        {
                                            // 检查Text组件是否仍然存在
                                            if (Txt_Title2 != null)
                                            {
                                                _writeEnd = true;
                                                AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
                                                SetBtnState();
                                            }
                                            titleTween2 = null;
                                        });
                                }
                            });
                    }
                }
                else
                {
                    if (Txt_Title1 != null)
                    {
                        titleTween1 = Txt_Title1.DOText(content1, content1.Length * 0.1f)
                                              .SetEase(Ease.Linear)
                       .OnComplete(() =>
                       {
                           // 检查Text组件是否仍然存在
                           if (Txt_Title1 != null)
                           {
                               _writeEnd = true;
                               AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
                               SetBtnState();
                           }
                       });
                    }
                }

            }
        }

        private void ForceCompleteTween()
        {
            Debug.Log("乐园ForceCompleteTween");
            // 立即完成第二个文本动画
            if (titleTween2 != null && titleTween2.IsPlaying())
            {
                Debug.Log("乐园ForceCompleteTween2");
                titleTween2?.Kill();
                titleTween2 = null;
            }
            // 立即完成第一个文本动画
            if (titleTween1 != null && titleTween1.IsPlaying())
            {
                Debug.Log("乐园ForceCompleteTween1");
                titleTween1?.Kill();
                titleTween1 = null;
            }



            // 安全地设置文本内容，检查组件是否存在
            if (isCustomContent)
            {
                if (Txt_CustomContent != null)
                {
                    Txt_CustomContent.text = content1;
                }
            }
            else
            {
                if (AIParkUtils.Inst.ParkGameData.isPgcEnter)
                {
                    if (Txt_Title1 != null)
                    {
                        Txt_Title1.text = ugcContent;
                    }
                    if (Txt_Title2 != null)
                    {
                        Txt_Title2.text = ugcContent1;
                    }
                }
                else
                {
                    if (Txt_Title1 != null)
                    {
                        Txt_Title1.text = content1;
                    }
                    if (Txt_Title2 != null)
                    {
                        Txt_Title2.text = "";
                    }
                }

            }
            _writeEnd = true;

            // 停止打字音效
            AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
            // AIGameSoundUtils.Inst.StopSound(AIParkConfig.GUIDE_BGM);

            SetBtnState();
        }

        void SetBtnState()
        {
            Debug.Log("乐园SetBtnState isWaitNextSceneData = " + isWaitNextSceneData);
            // 显示开始按钮
            if (isWaitNextSceneData)
            {
                if (Btn_Start != null) Btn_Start.gameObject.SetActive(false);
                if (loadTrans != null) loadTrans.gameObject.SetActive(true);
            }
            else
            {
                if (Btn_Start != null) Btn_Start.gameObject.SetActive(true);
                if (loadTrans != null) loadTrans.gameObject.SetActive(false);
            }
        }
    }
}