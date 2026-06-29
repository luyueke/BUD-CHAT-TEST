using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using Game.Audio;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.Config;
using GameData.Manager;
using GameData.PgcData;
using Newtonsoft.Json;
using UGCAsset;
using UnityEngine;
using UnityEngine.Events;

public enum OCTheatreGameState{
    None = 0,
    Loading = 1,
    Playing = 2,
    Optioning = 3,
    Waiting = 4, //一般是等待动画播放完毕
}

public struct OCTheatreGamePack<T>{
    public string mainAvatar; //当前执行者
    public string[] otherAvatars;//其他参与者
    public T data; //当前数据
}

public class OCTheatreGameController{
    private const string protectTimerId = "ocTheatreProtectTimer"; //保护定时器id
    private const float protectTimerDuration = 60f; //保护定时器时长, 确保动画没有卡住

    public static OCTheatreGameController Current { get; private set; }

    struct OCTheatreInGameCharacter{
        public string characterID;
        public Dictionary<int, CharacterWrap> characterWrapDict;
    }

    private string theatreID;
    private Transform characterRoot;
    private GameObject musicRoot;
    private GameObject theatreAudioNode;   //背景音乐/音效播放节点
    private GameObject theatreBgMusicNode; //整场BGM播放节点
    private Dictionary<string, OCTheatreInGameCharacter> CharacterDict = new Dictionary<string, OCTheatreInGameCharacter>();
    private List<int> paths = new List<int>();

    //监听触发时机的动画和音频们
    private List<OCTheatreEmote> watchingEmotes = new List<OCTheatreEmote>();
    private List<OCTheatreAudio> watchingAudios = new List<OCTheatreAudio>();

    private OCTDetailInfoRuntime theatreInfo;
    private List<OCTheatreAvatarOc> _originalActors;
    private OCTheatreGameState curState = OCTheatreGameState.None;
    private int curSectionIndex = 0; //当前是第几个对话
    private int waitToSectionIndex = 0; //等待到第几个对话
    private int curDialogueIndex = 0; //当前对话中的第几句
    private bool isPlayingEmote = false;
    private BudTimer protectTimer;

    private UnityAction<OCTheatreGamePack<OCTheatreSection>> onSectionChange;
    private UnityAction<OCTheatreGamePack<OCTheatreDialogue>> onDialogueTrigger;
    private UnityAction<OCTheatreGamePack<OCTheatreEmote>> onEmoteTrigger;
    private UnityAction<OCTheatreGamePack<OCTheatreAudio>> onSoundTrigger;
    private UnityAction onEmoteEnd;
    private UnityAction onEnd;

    #region 缓存字段

    private OCTheatreSection curSection;
    private OCTheatreAudio curAudio;
    private bool _bgMusicPaused; // true when section audio interrupted the whole-theatre BGM

    #endregion

    public bool DisableSave { get; set; }

    public OCTheatreGameController(string theatreID, Transform characterRoot, GameObject musicRoot){
        Current = this;
        this.theatreID = theatreID;
        this.characterRoot = characterRoot;
        curState = OCTheatreGameState.Loading;
        this.musicRoot = musicRoot;

        var audioNode = GameObjectEx.FindChildByName(musicRoot.transform, "TheatreAudioNode");
        var bgMusicNode = GameObjectEx.FindChildByName(musicRoot.transform, "TheatreBgMusicNode");
         if(audioNode != null){
            theatreAudioNode = audioNode.gameObject;
        }else{
            theatreAudioNode = new GameObject("TheatreAudioNode");
            var audioSource = new GameObject("_UGCAudio");
            audioSource.transform.SetParent(theatreAudioNode.transform);
            audioSource.transform.localPosition = Vector3.zero;
            audioSource.AddComponent<AudioSource>();
            theatreAudioNode.transform.SetParent(musicRoot.transform, false);
        }
        if(bgMusicNode != null){
            theatreBgMusicNode = bgMusicNode.gameObject;
        }else{
            theatreBgMusicNode = new GameObject("TheatreBgMusicNode");
            var audioSource = new GameObject("_UGCAudio");
            audioSource.transform.SetParent(theatreBgMusicNode.transform);
            audioSource.transform.localPosition = Vector3.zero;
            audioSource.AddComponent<AudioSource>();
            theatreBgMusicNode.transform.SetParent(musicRoot.transform, false);
        }
    }

    public void RegisterEvents(
        UnityAction<OCTheatreGamePack<OCTheatreSection>> onSectionChange, 
        UnityAction<OCTheatreGamePack<OCTheatreDialogue>> onDialogueTrigger, 
        UnityAction<OCTheatreGamePack<OCTheatreEmote>> onEmoteTrigger, 
        UnityAction<OCTheatreGamePack<OCTheatreAudio>> onSoundTrigger,
        UnityAction onEmoteEnd,
        UnityAction onEnd
        ){
        this.onSectionChange = onSectionChange;
        this.onDialogueTrigger = onDialogueTrigger;
        this.onEmoteTrigger = onEmoteTrigger;
        this.onSoundTrigger = onSoundTrigger;
        this.onEmoteEnd = onEmoteEnd;
        this.onEnd = onEnd;
    }

    /// <summary>
    /// 加载剧本的存档数据, 播放到哪个段落, 以及游玩路径
    /// </summary>
    public void LoadTheatreSave(int progress, List<int> paths){
        curSectionIndex = progress;
        this.paths = paths;
    }

    public void LoadTheatreInfo(OCTDetailInfoRuntime theatreInfo){
        this.theatreInfo = theatreInfo;
    }

    public void StoreOriginalActors(List<OCTheatreAvatarOc> actors)
    {
        if (actors == null) { _originalActors = null; return; }
        _originalActors = new List<OCTheatreAvatarOc>(actors.Count);
        foreach (var a in actors)
            _originalActors.Add(new OCTheatreAvatarOc
            {
                playerId = a.playerId,
                avatarName = a.avatarName,
                avatarURL = a.avatarURL,
                clothesIndex = a.clothesIndex
            });
    }

    public List<OCTheatreAvatarOc> GetDefaultActorList() => _originalActors ?? theatreInfo?.allAvatars;
    public string TheatreID => theatreInfo?.theatreID;

    public bool IsCurrentTheatre(string theatreID){
        return this.theatreID == theatreID;
    }

    public OCTheatreGameState GetCurrentState(){
        return curState;
    }

    public void SetCurrentState(OCTheatreGameState state){
        curState = state;
    }

    public void StartGame(){
        if (theatreInfo == null) return;
        curState = OCTheatreGameState.Playing;
        if (!string.IsNullOrEmpty(theatreInfo.bgMusic))
        {
            AkSoundManager.Inst.PlayUGCAudioByUrl(theatreInfo.bgMusic, true, theatreBgMusicNode);
        }
        if(curSectionIndex == 0){
            curSectionIndex = 1; //默认1起始
        }
        SetSection(curSectionIndex);
    }

    public void StopBgMusic(){
        _bgMusicPaused = false;
        AkSoundManager.Inst.StopUGCAudio(theatreBgMusicNode);
    }

    private void PauseBgMusic(){
        if (_bgMusicPaused) return;
        _bgMusicPaused = true;
        var t = theatreBgMusicNode?.transform.Find("_UGCAudio");
        if (t != null)
            t.GetComponent<AudioSource>().mute = true;
    }

    private void ResumeBgMusic(){
        if (!_bgMusicPaused) return;
        _bgMusicPaused = false;
        var t = theatreBgMusicNode?.transform.Find("_UGCAudio");
        if (t != null)
        {
            var audioSource = t.GetComponent<AudioSource>();
            audioSource.mute = false;
            if (audioSource.clip != null)
                return;
        }
        if (!string.IsNullOrEmpty(theatreInfo?.bgMusic))
            AkSoundManager.Inst.PlayUGCAudioByUrl(theatreInfo.bgMusic, true, theatreBgMusicNode);
    }

    public void Release(){
        TimerManager.Inst.Stop(protectTimer);
        protectTimer = null;
        onSectionChange = null;
        onDialogueTrigger = null;
        onEmoteTrigger = null;
        onSoundTrigger = null;
        onEmoteEnd = null;
        onEnd = null;
    }

    public bool CheckEndGame(){

        if(theatreInfo.sections.TryGetValue(curSectionIndex, out curSection)){
            if(curSection.nextIndex == 0){
                onEnd?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void GoToNextSection(int index){
        
        OnSectionEnd();

        //如果是等待状态，无法进入下一个段落
        if(curState == OCTheatreGameState.Waiting){
            return;
        }

        if(theatreInfo.sections.ContainsKey(index)){
            if(isPlayingEmote){
                //停止当前动画
                StopCurrentPlayingEmote();
            }

            curSectionIndex = index;
            SetSection(curSectionIndex);
            // //如果都要去下一个段落，但是当前有动画正在播放，则等待动画播放完毕
            // if(isPlayingEmote){
            //     curState = OCTheatreGameState.Waiting;
            //     waitToSectionIndex = index;
            //     return;
            // }else{

            // }
        }
    }

    public void NextDialogue(out bool isEnd){
        curDialogueIndex++;
        if(curDialogueIndex >= curSection.dialogues.Count){
            isEnd = true;
            return;
        }
        onDialogueTrigger?.Invoke(new OCTheatreGamePack<OCTheatreDialogue>(){
            mainAvatar = curSection.avatarID,
            otherAvatars = new string[0],
            data = curSection.dialogues[curDialogueIndex]
        });
        OnDialogueChange();
        if(curDialogueIndex >= curSection.dialogues.Count-1){
            isEnd = true;
        }else{
            isEnd = false;
        }

        if(curSection.dialogues[curDialogueIndex].type == (int)OCTheatreDialogueType.Option){
            curState = OCTheatreGameState.Optioning;
        }else{
            curState = OCTheatreGameState.Playing;
        }
    }

    /// <summary>
    /// 重新播放当前表情演绎
    /// </summary>
    public void PlayCurrentEmoteAgain(){
        if(curSection.emote != null){
            ArrangeEmote(curSection.emote);
        }
    }

    /// <summary>
    /// 设置当前对话段落
    /// </summary>
    /// <param name="index"></param>
    private void SetSection(int index){
        watchingEmotes.Clear();
        watchingAudios.Clear();
        EndAudio();
        curDialogueIndex = 0;
        isPlayingEmote = false;

        if(theatreInfo.sections.TryGetValue(index, out curSection)){

            if(curSection.emote != null){
                watchingEmotes.Add(curSection.emote);
            }

            if(curSection.audio != null){
                watchingAudios.Add(curSection.audio);
            }
            
            onSectionChange?.Invoke(new OCTheatreGamePack<OCTheatreSection>(){
                mainAvatar = curSection.avatarID,
                otherAvatars = new string[0],
                data = curSection
            });
            OnSectionStart();
            OnDialogueChange(); // dialogues[0] is displayed immediately; fire triggerTime==1 events
            RecordPath(index);
        }

    }

    private void RecordPath(int index){
        paths.Add(index);
        if (DisableSave) return;
        OCTheatreDataManager.Inst.SavePaths(theatreID, paths);
        OCTheatreDataManager.Inst.SaveProgress(theatreID, index);
    }

    #region 事件回调

    private void OnSectionStart(){
        foreach(var emote in watchingEmotes){
            if(emote.triggerTime == 0){
                ArrangeEmote(emote);
            }
        }
        foreach(var audio in watchingAudios){
            if(audio.triggerTime == 0){
                ArrangeAudio(audio, theatreAudioNode);
            }
        }
    }

    private void OnDialogueChange(){
        foreach(var emote in watchingEmotes){
            if(emote.triggerTime == curDialogueIndex + 1 && emote.triggerType == 0){
                ArrangeEmote(emote);
            }
        }
        foreach(var audio in watchingAudios){
            if(audio.triggerTime == curDialogueIndex + 1 && audio.triggerType == 0){
                ArrangeAudio(audio, theatreAudioNode);
            }
            // triggerTimeEnd: stop when the Nth dialogue ends (N = triggerTimeEnd, 1-based).
            // After NextDialogue() increments curDialogueIndex, curDialogueIndex == triggerTimeEnd
            // means we've just advanced past dialogue triggerTimeEnd.
            if(audio.triggerTimeEnd > 0 && curDialogueIndex == audio.triggerTimeEnd){
                StopAudioOnNode(theatreAudioNode);
                curAudio = null;
                ResumeBgMusic();
            }
        }
    }

    /// <summary>
    /// 由UI层在对话文本播放完毕（打字机动画结束）时调用，触发 triggerType==1 的音频和表情
    /// </summary>
    public void NotifyDialogueTextComplete(){
        foreach(var emote in watchingEmotes){
            if(emote.triggerTime == curDialogueIndex + 1 && emote.triggerType == 1){
                ArrangeEmote(emote);
            }
        }
        foreach(var audio in watchingAudios){
            if(audio.triggerTime == curDialogueIndex + 1 && audio.triggerType == 1){
                ArrangeAudio(audio, theatreAudioNode);
            }
        }
    }

    private void OnSectionEnd(){
        foreach(var emote in watchingEmotes){
            if(emote.triggerTime == -1){
                ArrangeEmote(emote);
            }
        }
        foreach(var audio in watchingAudios){
            if(audio.triggerTime == -1){
                ArrangeAudio(audio, theatreAudioNode);
            }
        }
    }

    private void OnEmoteFinish(){
        TimerManager.Inst.Stop(protectTimer);
        protectTimer = null;
        isPlayingEmote = false;
        onEmoteEnd?.Invoke();
    }

    private void OnProtectTimerFinish(){
        if(isPlayingEmote){
            OnEmoteFinish();
        }
    }

    #endregion

    #region 功能行为
    private void HideAllAvatars()
    {
        foreach (var character in CharacterDict.Values)
        {
            if (character.characterWrapDict == null) continue;
            foreach (var wrap in character.characterWrapDict.Values)
                wrap?.Avatar?.SetActive(false);
        }
    }

    private void StopCurrentPlayingEmote()
    {
        foreach (var character in CharacterDict.Values)
        {
            if (character.characterWrapDict == null) continue;

            foreach (var characterWrap in character.characterWrapDict.Values)
            {
                if (characterWrap?.Avatar == null) continue;
                var animationCtrl = characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
                animationCtrl?.ResetEmoteAnimation();

                var animIkController = characterWrap.Avatar.GetComponent<AnimIKController>();
                if (animIkController != null)
                {
                    animIkController.StopAnim();
                    animIkController.ChangeUgcToPgcAnim();
                }
                characterWrap.Avatar.SetActive(false);
            }
        }

        OnEmoteFinish();
    }

    public void LoadCharacter(string characterID, int clothesIndex, string json, Action<CharacterWrap> onLoaded=null){

        if(CharacterDict.ContainsKey(characterID)
        && CharacterDict[characterID].characterWrapDict.ContainsKey(clothesIndex)){
            onLoaded?.Invoke(CharacterDict[characterID].characterWrapDict[clothesIndex]);
            return;
        }
        
        var characterData = CharacterData.DeserializeObject(json);
        var character = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, characterRoot);
        if(CharacterDict.ContainsKey(characterID)){
            CharacterDict[characterID].characterWrapDict[clothesIndex] = character;
        }else{
            CharacterDict.Add(characterID, new OCTheatreInGameCharacter(){
                characterID = characterID,
                characterWrapDict = new Dictionary<int, CharacterWrap>(){
                    { clothesIndex, character }   
                }
            });
        }
        character.Avatar.SetActive(false);
        onLoaded?.Invoke(character);

    }

    /// <summary>
    /// 目前是一换全换，一套衣服换掉所有衣服配置
    /// </summary>
    /// <param name="characterID"></param>
    /// <param name="json"></param>
    public void ReplaceCharacter(string characterID, string json){
        var characterData = CharacterData.DeserializeObject(json);
        var character = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, characterRoot);
        if(CharacterDict.ContainsKey(characterID)){
            foreach(var characterWrap in CharacterDict[characterID].characterWrapDict){
                characterWrap.Value.RefreshAvatar(characterData);
            }
        }
    }

    private void ArrangeEmote(OCTheatreEmote emote){
        Debug.Log($"[OCTheatre] ArrangeEmote: {JsonConvert.SerializeObject(emote)}");
        HideAllAvatars();
        var emoteId = emote.emoteID ?? string.Empty;
        if (string.IsNullOrEmpty(emoteId))
        {
            return;
        }

        var isUgcEmote = !int.TryParse(emoteId, out _);
        var emoteConfig = isUgcEmote ? null : DataTables.GetEmoUIConfig(emoteId);

        if (!isUgcEmote && emoteConfig == null)
        {
            Debug.LogWarning(
                $"[OCTheatre] PGC emote config not found id={emoteId}, falling back to player count"
            );
        }
        else if (!isUgcEmote)
        {
            Debug.Log($"[OCTheatre] ArrangeEmote pgcEmote id={emoteId} playerState={emoteConfig.playerState}");
        }
        else
        {
            Debug.Log($"[OCTheatre] ArrangeEmote ugcEmote id={emoteId}");
        }

        bool isSingleEmote = (isUgcEmote || emoteConfig == null)
            ? emote.emotePlayers.Count <= 1
            : emoteConfig.playerState.StartsWith("Single");
        bool isDoubleEmote = (isUgcEmote || emoteConfig == null)
            ? emote.emotePlayers.Count > 1
            : emoteConfig.playerState.StartsWith("Double");

        if(isSingleEmote){
            if(emote.emotePlayers.Count > 0){
                PlayEmote(
                    emote.emotePlayers[0].playerId, 
                    emote.emotePlayers[0].clothesIndex, 
                    emoteId
                );
                isPlayingEmote = true;
                TimerManager.Inst.Stop(protectTimer);
                protectTimer = TimerManager.Inst.RunOnce(protectTimerId, protectTimerDuration, OnProtectTimerFinish);
            }
        }else if(isDoubleEmote){
            if(emote.emotePlayers.Count > 1){
                PlayDoubleEmote(
                    emote.emotePlayers[0].playerId, 
                        emote.emotePlayers[1].playerId, 
                        emote.emotePlayers[0].clothesIndex, 
                        emote.emotePlayers[1].clothesIndex,
                        emoteId
                    );
                isPlayingEmote = true;
            }else if(emote.emotePlayers.Count > 0 && emote.emotePlayers.Count < 2){
                PlayDoubleEmote(
                    emote.emotePlayers[0].playerId, 
                        emote.emotePlayers[0].playerId, 
                        emote.emotePlayers[0].clothesIndex, 
                        emote.emotePlayers[0].clothesIndex,
                        emoteId
                    );
                isPlayingEmote = true;
            }
        }

        onEmoteTrigger?.Invoke(new OCTheatreGamePack<OCTheatreEmote>(){
            mainAvatar = emote.emotePlayers.Count > 0 ? emote.emotePlayers[0].playerId : string.Empty,
            otherAvatars = emote.emotePlayers.ConvertAll(player => player.playerId).ToArray(),
            data = emote
        });
    }

    private bool TryGetWrap(string characterID, int clothesIndex, out CharacterWrap wrap)
    {
        wrap = default;
        if (!CharacterDict.ContainsKey(characterID)) return false;
        var dict = CharacterDict[characterID].characterWrapDict;
        if (dict.TryGetValue(clothesIndex, out wrap)) return true;
        if (dict.Count == 0) return false;
        foreach (var kv in dict) { wrap = kv.Value; break; }
        Debug.LogWarning($"[OCTheatre] clothesIndex mismatch: id={characterID} need={clothesIndex} have=[{string.Join(",", dict.Keys)}], using fallback");
        return true;
    }

    public void PlayEmote(string characterID, int clothesIndex, string emoteID){
        if (!TryGetWrap(characterID, clothesIndex, out var wrap)) return;

        wrap.Avatar.SetActive(true);
        wrap.Avatar.transform.parent.parent.localPosition = new Vector3(0, -0.5f, 0);

        if (int.TryParse(emoteID, out _))
        {
            var animationCtrl = wrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            animationCtrl.PlaySingleEmoteForUICharacter(emoteID, OnCompleteSingleEmote: OnEmoteFinish);
            return;
        }

        UGCAnimAssetManager.Inst.GetAssetInfo<UGCAnimGetResponse>(emoteID, animInfo =>
        {
            if (animInfo == null) return;

            var animIkController = wrap.Avatar.GetComponent<AnimIKController>();
            if (animIkController == null) return;

            animIkController.Play(animInfo, null, OnEmoteFinish);
        });
    }

    public void PlayDoubleEmote(string characterID, string otherCharacterID, int clothesIndex, int otherClothesIndex, string emoteID){
        if (!TryGetWrap(characterID, clothesIndex, out var wrap)) return;
        if (!TryGetWrap(otherCharacterID, otherClothesIndex, out var otherWrap)) return;

        wrap.Avatar.SetActive(true);
        wrap.Avatar.transform.parent.parent.localPosition = new Vector3(-0.5f, -0.5f, 0);
        otherWrap.Avatar.transform.parent.parent.localPosition = new Vector3(-0.5f, -0.5f, 0);

        if (int.TryParse(emoteID, out _))
        {
            otherWrap.Avatar.SetActive(false);
            var animationCtrl = wrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            var otherAnimationCtrl = otherWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            animationCtrl.PlayDoubleEmoteForUICharacter(emoteID, otherAnimationCtrl, OnCompleteDoubleEmote: OnEmoteFinish);
            return;
        }

        otherWrap.Avatar.SetActive(true);

        UGCAnimAssetManager.Inst.GetAssetInfo<UGCAnimGetResponse>(emoteID, animInfo =>
        {
            if (animInfo == null) return;

            var animIkController = wrap.Avatar.GetComponent<AnimIKController>();
            var otherAnimIkController = otherWrap.Avatar.GetComponent<AnimIKController>();
            if (animIkController == null || otherAnimIkController == null) return;

            animIkController.Play(animInfo, otherAnimIkController, OnEmoteFinish);
        });
    }

    private void ArrangeAudio(OCTheatreAudio audio, GameObject node){
        onSoundTrigger?.Invoke(new OCTheatreGamePack<OCTheatreAudio>(){
            mainAvatar = curSection.avatarID,
            otherAvatars = new string[0],
            data = audio
        });
        if (node == theatreAudioNode && !string.IsNullOrEmpty(audio.audioURL))
        {
            curAudio = audio;
            PauseBgMusic();
        }
        PlayAudioOnNode(audio, node);
    }

    private void PlayAudioOnNode(OCTheatreAudio audio, GameObject node){
        StopAudioOnNode(node);
        AkSoundManager.Inst.PlayUGCAudioByUrl(audio.audioURL, audio.isLoop == 1, node);
    }

    private void StopAudioOnNode(GameObject node){
        AkSoundManager.Inst.StopAll(node);
        AkSoundManager.Inst.StopUGCAudio(node);
    }

    public void PlayAudio(OCTheatreAudio audio){
        curAudio = audio;
        PlayAudioOnNode(audio, theatreAudioNode);
    }

    public void EndAudio(){
        if(curAudio != null){
            StopAudioOnNode(theatreAudioNode);
            curAudio = null;
            ResumeBgMusic();
        }
    }

    #endregion

}