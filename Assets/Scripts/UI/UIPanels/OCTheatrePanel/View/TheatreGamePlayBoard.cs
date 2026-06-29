using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TheatreGamePlayBoard : MonoBehaviour{
    [SerializeField] private RectTransform DialogueRoot;
    [SerializeField] private Text AvatarName;
    [SerializeField] private RemoteImageBehaviour AvatarImage;
    [SerializeField] private RectTransform AvatarRoot;
    [SerializeField] private TheatreTypewriter Typewriter;
    [SerializeField] private GameObject OptionRoot;
    [SerializeField] private List<Button> Options;
    [SerializeField] private Animation nextIconAnimation;
    [SerializeField] private GameObject avatarImageGObj;
    [SerializeField] private GameObject avatarNameGObj;
    [SerializeField] private RectTransform dialogueTextArea;

    public UnityAction<int> onOptionClick;
    public Action onDialogueCompleted;

    private Vector2 DialogueRootOriginalScale;

    public void Init(UnityAction<int> onOptionClick){
        this.onOptionClick = onOptionClick;
        OptionRoot.SetActive(false);
        //添加点击事件，不能直接用i，
        for(int i = 0; i < Options.Count; i++){ 
            int index = i;
            Options[i].onClick.AddListener(() => onOptionClick?.Invoke(index));
        }
        Typewriter.TypingComplete += OnDialogueCompleted;
        nextIconAnimation.gameObject.SetActive(false);
        DialogueRootOriginalScale = DialogueRoot.localScale;
    }

    public void SetTypingEnabled(bool enabled){
        Typewriter.EnableTypewriter = enabled;
    }

    public bool CanJumpNext(){
        if(Typewriter.IsTyping){
            Typewriter.CompleteImmediately();
            return false;
        }
        return true;
    }

    public void PlayEnterAnimation(){
        OptionRoot.SetActive(false);
        //从下往上进场
        DialogueRoot.localScale = Vector3.zero;
        DialogueRoot.DOScale(DialogueRootOriginalScale, 0.5f).SetEase(Ease.OutBack);
    }

    public void SetAvatarVisible(bool visible)
    {
        if (avatarImageGObj != null) avatarImageGObj.SetActive(visible);
        if (avatarNameGObj != null) avatarNameGObj.SetActive(visible);
        if (dialogueTextArea != null)
        {
            var min = dialogueTextArea.offsetMin;
            min.x = visible ? 278.41f : 43f;
            dialogueTextArea.offsetMin = min;
        }
    }

    public void SetAvatar(string name, string avatarURL, int type){
        AvatarName.text = name;
        AvatarImage.Load(avatarURL);
    }

    public void SetDialogue(string dialogueText){
        //Debug.LogError($"SetDialogue: {dialogueText}");
        nextIconAnimation.gameObject.SetActive(false);
        Typewriter.Play(dialogueText);
    }

    public void SetOptions(List<string> options){
        OptionRoot.SetActive(true);
        for(int i = 0; i < Options.Count; i++){
            if(i >= options.Count){
                Options[i].gameObject.SetActive(false);
                continue;
            }
            Options[i].gameObject.SetActive(true);
            Options[i].GetComponentInChildren<Text>().text = options[i];
        }
    }

    private void OnDialogueCompleted(string text){
        nextIconAnimation.gameObject.SetActive(true);
        onDialogueCompleted?.Invoke();
    }
    

}