using AIGame.Base;
using DG.Tweening;
using Es;
using EventTracking;
using Game.Audio;
using Game.Avatar;
using GameData.PgcData;
using Message;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class ConnectingGamePanel : BasePanel<ConnectingGamePanel>
{
    public Button Close;

    public GameObject GameGroup;
    public GameObject Dumpling;
    public GameObject Lantern;
    public GameObject Bomb;
    public RectTransform _gameRoot;
    public AvatarCameraController avatarCameraController;
    private PlayerAnimationCtrl animationCtrl;
    public GameObject modelRoot;
    public RectTransform Player;
    int addScoreTxtIndex;
    public List<Text> _addScoreTxt;
    public Text _scoreTxt;
    public Text _timeTxt;
    public Text _comboTxt;
    public GameObject _comboBg;

    public GameObject StartGroup;
    public Button _startBtn;
    public Button _ruleBtn;

    public GameObject EndGroup;
    public Text _resultTxt;
    public Button _closeBtn;
    public Button _againBtn;

    public GameObject RuleGroup;
    public Button _closeRuleBtn;

    [Header("游戏参数")] float GameDuration = 30f;
    [Header("掉落速度")] float BaseSpeedPixelsPerFrame = 2.5f * 2.5f;
    float SpeedMultiplierMin = 1f, SpeedMultiplierMax = 3.5f;
    [Header("生成")] float SpawnIntervalMinMs = 1000f, SpawnIntervalMaxMs = 450f;
    int SpawnCountMin = 1, SpawnCountMax = 8;
    [Tooltip("队列里每间隔多少秒实际生成一个")]
    float SpawnQueueInterval = 0.1f;
    [Header("概率")] [Range(0, 1)] float LanternProbability = 0.05f;
    [Range(0, 1)] float BombProbabilityMin = 0.25f, BombProbabilityMax = 0.45f;
    [Header("玩家")] float PlayerHalfWidth = 100f, PlayerCatchY = 420f, ItemHalfWidth = 60f, OutOfScreenY = -100f;

    private enum GameState { NotStarted, Playing, Ended }
    private GameState _state = GameState.NotStarted;
    private int _score, _combo, _lastUiScore = -1, _lastUiCombo = -1, _lastUiTimeSec = -1;
    private float _timeLeft, _spawnTimer, _currentSpawnIntervalSec, _gameAreaMinX, _gameAreaMaxX, _spawnY;
    private float _spawnQueueTimer;
    private Queue<float> _spawnQueue = new Queue<float>();
    private List<ConnectingGameItem> _fallingItems = new List<ConnectingGameItem>();
    private CharacterWrap characterWrap;
    private float GameProgress => GameDuration > 0 ? 1f - _timeLeft / GameDuration : 0f;

    private class ConnectingGameItem
    {
        public RectTransform Rect; public ConnectingGameItemType Type; public float X, Y;
    }

    public override void OnCreate()
    {
        base.OnCreate();
        Close.onClick.AddListener(CloseSelf);
        _startBtn.onClick.AddListener(OnStartGame);
        _ruleBtn.onClick.AddListener(() => RuleGroup.SetActive(true));
        _closeRuleBtn.onClick.AddListener(() => RuleGroup.SetActive(false));
        _closeBtn.onClick.AddListener(CloseSelf);
        _againBtn.onClick.AddListener(OnStartGame);
        characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.avatarInfo.Clone(), modelRoot.transform);
        animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        AkSoundManager.Inst.StopBGSound();
        AkSoundManager.Inst.PostEventAsync("Play_Bgm_S13TY", gameObject);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        bo = true;
        ResetGame();
    }

    protected override void OnDestroy() { StopAllCoroutines(); _state = GameState.NotStarted; DestroyCharacter(); base.OnDestroy(); }

    private void DestroyCharacter()
    {
        AkSoundManager.Inst.PostEventAsync("Stop_Bgm_S13TY", gameObject);
        AkSoundManager.Inst.PlayBGSound();
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
        if (characterWrap == null) return;
        characterWrap.DestorySelf();
        characterWrap = null;
    }

    bool bo = false;
    public void SwitchEmo(bool move) {
        if (bo == move) 
        {
            return;
        }
        bo = move;
        if (move)
        {
            animationCtrl.PlaySingleEmoteForUICharacter("40200523", null);
        }
        else
        {
            animationCtrl.PlaySingleEmoteForUICharacter("40200522", null);
        }
    }

    private void ResetGame()
    {
        SwitchEmo(false);
        _state = GameState.NotStarted;
        _score = _combo = 0;
        _timeLeft = GameDuration;
        _spawnTimer = 0f;
        _spawnQueueTimer = 0f;
        _spawnQueue.Clear();
        lastx = 9999;
        _lastUiScore = _lastUiCombo = _lastUiTimeSec = -1;
        _gameAreaMinX = -Screen.width / 2f; _gameAreaMaxX = Screen.width / 2f; _spawnY = Screen.height;
        Player.anchoredPosition = new Vector2(0, Player.anchoredPosition.y);
        RuleGroup.SetActive(false); EndGroup.SetActive(false); StartGroup.SetActive(true);
        ClearAllItems();
        RefreshUI();
    }

    private void OnStartGame()
    {
        if (_state == GameState.Ended) ResetGame();
        _state = GameState.Playing;
        StartGroup.SetActive(false);
        StartCoroutine(GameLoop());
        LoadEvent.ReportTask(166, 1);
    }

    private IEnumerator GameLoop()
    {
        while (_state == GameState.Playing && _timeLeft > 0f)
        {
            float dt = Time.deltaTime;
            _timeLeft -= dt;
            _spawnTimer += dt;
            float progress = GameProgress;
            _currentSpawnIntervalSec = Mathf.Lerp(SpawnIntervalMinMs / 1000f, SpawnIntervalMaxMs / 1000f, progress);

            if (_spawnTimer >= _currentSpawnIntervalSec)
            {
                _spawnTimer -= _currentSpawnIntervalSec;
                int n = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(SpawnCountMin, SpawnCountMax, progress)), SpawnCountMin, SpawnCountMax);
                for (int i = 0; i < n; i++) SpawnItem(progress);
            }

            float interval = SpawnQueueInterval > 0.001f ? SpawnQueueInterval : 0.1f;
            _spawnQueueTimer += dt;
            while (_spawnQueue.Count > 0 && _spawnQueueTimer >= interval)
            {
                DoSpawnOne(_spawnQueue.Dequeue());
                _spawnQueueTimer -= interval;
            }

            // 跟随鼠标或触摸：屏幕坐标转游戏内 X（与 _gameAreaMinX/MaxX 同系）
            Vector2 ptr = Vector2.zero;
            bool input = false;
            float playerX = Player.anchoredPosition.x;
#if UNITY_EDITOR
            input = true;
            ptr = Input.mousePosition;
#else
            if (Input.touchCount > 0)
            {
                input = true;
                ptr = (Vector2)Input.GetTouch(0).position;
            }
#endif
            if (input)
            {
                playerX = Mathf.Clamp(ptr.x - Screen.width * 0.5f, _gameAreaMinX + PlayerHalfWidth, _gameAreaMaxX - PlayerHalfWidth);
                if (Player.anchoredPosition.x != playerX)
                {
                    Player.anchoredPosition = new Vector2(playerX, Player.anchoredPosition.y);
                    SwitchEmo(true);
                }
                else
                {
                    SwitchEmo(false);
                }
            }

            float move = BaseSpeedPixelsPerFrame * Mathf.Lerp(SpeedMultiplierMin, SpeedMultiplierMax, progress);
            float catchR = PlayerHalfWidth + ItemHalfWidth;
            for (int i = _fallingItems.Count - 1; i >= 0; i--)
            {
                var it = _fallingItems[i];
                it.Y -= move;
                it.Rect.anchoredPosition = new Vector2(it.X, it.Y);
                if (it.Y < PlayerCatchY && Mathf.Abs(it.X - playerX) < catchR) { OnCatchItem(it); RemoveItemAt(i); }
                else if (it.Y < OutOfScreenY) RemoveItemAt(i);
            }
            RefreshUI();
            yield return null;
        }
        EndGame();
    }

    float lastx = 9999;
    /// <summary> 只入队，不立即生成 </summary>
    private void SpawnItem(float progress)
    {
        _spawnQueue.Enqueue(progress);
    }

    /// <summary> 每 0.1s 从队列取一个，真正生成（含 lastx 重叠校验） </summary>
    private void DoSpawnOne(float progress)
    {
        float x = Random.Range(_gameAreaMinX, _gameAreaMaxX);
        float gap = ItemHalfWidth * 2f;
        if (lastx != 9999 && Mathf.Abs(x - lastx) < gap)
            return;
        lastx = x;
        float bombP = Mathf.Min(Mathf.Lerp(BombProbabilityMin, BombProbabilityMax, progress), 1f - LanternProbability);
        float r = Random.value;
        GameObject prefab; ConnectingGameItemType type;
        if (r < LanternProbability) { prefab = Lantern; type = ConnectingGameItemType.Lantern; }
        else if (r < LanternProbability + bombP) { prefab = Bomb; type = ConnectingGameItemType.Bomb; }
        else { prefab = Dumpling; type = ConnectingGameItemType.Dumpling; }
        var rect = Instantiate(prefab, _gameRoot).GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, _spawnY);
        rect.SetAsFirstSibling();
        _fallingItems.Add(new ConnectingGameItem { Rect = rect, Type = type, X = x, Y = _spawnY });
    }

    private void OnCatchItem(ConnectingGameItem item)
    {
        var (delta, isBomb) = ConnectingGameMgr.GetScoreWithCombo(item.Type, _combo);
        _score += delta;
        _combo = isBomb ? 0 : _combo + 1;
        if (delta > 0)
        {
            AkSoundManager.Inst.PostEventAsync("Play_TY_Get_Good", gameObject);
            addScoreTxtIndex++;
            if (addScoreTxtIndex >= _addScoreTxt.Count)
            {
                addScoreTxtIndex = 0;
            }
            var txt = _addScoreTxt[addScoreTxtIndex];
            txt.text = "+" + delta;
            var r = txt.transform as RectTransform;
            r.anchoredPosition = new Vector2(0,0);
            var t = r.DOAnchorPosY(100,0.5f);
            t.onComplete = () => { txt.text = ""; };
        }
        else
        {
            AkSoundManager.Inst.PostEventAsync("Play_TY_Get_Bad", gameObject);
        }
    }

    private void RemoveItemAt(int i)
    {
        if (_fallingItems[i].Rect != null && _fallingItems[i].Rect.gameObject != null) Destroy(_fallingItems[i].Rect.gameObject);
        _fallingItems.RemoveAt(i);
    }

    private void ClearAllItems()
    {
        foreach (var it in _fallingItems) { if (it.Rect != null && it.Rect.gameObject != null) Destroy(it.Rect.gameObject); }
        _fallingItems.Clear();
    }

    private void EndGame()
    {
        SwitchEmo(false);
        _state = GameState.Ended;
        EndGroup.SetActive(true);
        _resultTxt.text = $"最终得分 ：<size=66><color=#FFF1B8>{_score}</color></size>";
        ClearAllItems();
        LoadEvent.ReportTask(166, _score);
    }

    private void RefreshUI()
    {
        if (_score != _lastUiScore) 
        {
            _lastUiScore = _score; 
            _scoreTxt.text = $"分数：{_score}"; 
        }
        int sec = Mathf.CeilToInt(Mathf.Max(0, _timeLeft));
        if (sec != _lastUiTimeSec) { _lastUiTimeSec = sec; _timeTxt.text = $"倒计时：{sec}秒"; }
        if (_combo != _lastUiCombo) 
        {
            _lastUiCombo = _combo; 
            _comboTxt.text = $"连击x{_combo}";
            _comboBg.gameObject.SetActive(_combo >= 5);
        }
    }
}
