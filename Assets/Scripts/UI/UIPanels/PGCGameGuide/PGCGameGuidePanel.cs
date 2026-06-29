using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PGCGameGuidePanel : BasePanel<PGCGameGuidePanel>
{
    public GameObject Go_GuideView;
    public Image GuideDialogueBG;
    public Image GuideNameBG;
    public SuperTextMesh GuideDialogueText;
    public SuperTextMesh NameText;
    public Image PeoplePic;
    public Button SkipButton;

    private NpcVoicePlayer voicePlayer;

    private int index;
    private PGCGameGuideConfig mConfig;
    private Action mOver;
    private bool isPlaying;

    public override void OnCreate()
    {
        base.OnCreate();
        voicePlayer = NpcVoicePlayer.Create("PUGCGuideTalker");
        SkipButton.onClick.AddListener(DoTalk);
    }

    public void SetData(PGCGameType gameType, Action guideOverAct)
    {
        var key = "PUGC_GUIDE_" + gameType + AccountDataManager.Inst.Uid;
        if (PlayerPrefs.GetInt(key , 0) == 0)
        {
            Go_GuideView.SetActive(true);
            // PlayerPrefs.SetInt(key , 1);
            var curConfig = PGCGameGuideUtils.GetGuideConfig(PGCGameType.AIYandere);
            Init(curConfig, guideOverAct);
            return;
        }
        else
        {
            guideOverAct?.Invoke();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        voicePlayer.Destroy();
        voicePlayer = null;
        GuideDialogueText.DOKill();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        voicePlayer?.Stop();
    }

    public void Init(PGCGameGuideConfig config, Action over)
    {
        mConfig = config;
        mOver = over;
        NameText.text = config.talkerName;
        GuideDialogueBG.sprite = PGCGameGuideUtils.LoadGuideIcon(config.guideDialogueBgImage, this.gameObject);
        GuideNameBG.sprite = PGCGameGuideUtils.LoadGuideIcon(config.guideNameImage, this.gameObject);
        PeoplePic.sprite = PGCGameGuideUtils.LoadGuideIcon(config.guidePeopleImage, this.gameObject);
        index = -1;
        isPlaying = false;
        DoTalk();
    }

    public void DoTalk()
    {
        if (index >= mConfig.talks.Count)
        {
            voicePlayer.Stop();
            mOver?.Invoke();
            return;
        }

        if (isPlaying)
        {
            GuideDialogueText.DOKill();
            GuideDialogueText.SetText(mConfig.talks[index]);
            isPlaying = false;
        }
        else
        {
            index++;
            if (index >= mConfig.talks.Count)
            {
                mOver?.Invoke();
                return;
            }
            var content = mConfig.talks[index];
            void DoTextAni(float time = -1)
            {
                GuideDialogueText.DOKill(false);
                GuideDialogueText.text = "";
                
                if (time <= 0) time = content.Length / 20;
                GuideDialogueText.DOText(content, time).SetEase(Ease.Linear).OnComplete(() =>
                {
                    isPlaying = false;
                });
            }
            
            GuideDialogueText.DOKill();
            GuideDialogueText.text = "";
            isPlaying = true;
            if (!string.IsNullOrEmpty(mConfig.voiceId))
            {
                if (voicePlayer)
                {
                    voicePlayer.PlayText(content, 2, () => DoTextAni(voicePlayer.GetVoiceLength()), (e) => DoTextAni());
                }
                else
                {
                    DoTextAni();
                }
            }
            else
            {
                DoTextAni();
            }
        }
    }
}
