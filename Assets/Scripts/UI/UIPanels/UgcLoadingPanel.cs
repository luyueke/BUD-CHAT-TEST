using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using GameData.Base;
using Message;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public enum LoadingType
{
    Map = 0,
    Cloth = 1,
    Prop = 2,
    Material = 3,
    MusicalInstrument = 4,
    UGCAnim = 5,
    Vehicle = 6,
}

public class UgcLoadingPanel : BasePanel<UgcLoadingPanel>
{
    [SerializeField] private Text LoadingText;
    [SerializeField] private BUD_Text mapName;
    [SerializeField] private BUD_Text nickName;
    [SerializeField] private Slider progressBar;
    [SerializeField] private RemoteImageBehaviour coverLoader;
    [SerializeField] private RawImage cover;
    [SerializeField] private Image mask;
    [SerializeField] private RemoteImageBehaviour propCoverLoader;
    [SerializeField] private RawImage propCover;
    [SerializeField] private Image propTempCover;
    [SerializeField] private GameObject avatarSkinBg;
    [SerializeField] private GameObject instrumentBg;
    [SerializeField] private GameObject ugcAnimBg;
    [SerializeField] private GameObject vehicleBg;
    private BudTimer loadingTimer;
    private int loadingCount = 0;

    public class LoadStep
    {
        public string name;
        public int count = 100;
        public float speed = 0.05f;
        public float starValue = 0;//阶段开始数值
        public float endValue = 1;//结束阶段
    }
    
    private LoadStep currentLoadStep;
    private float curProgress = 0f;

    private const float DownloaStepMaxValue = 0.8f;
    protected override void Awake()
    {
        base.Awake();
        MessageHelper.AddListener(MessageName.StartLoadRemoteMetadata, OnStartLoadRemoteMetadata);
        MessageHelper.AddListener(MessageName.OverLoadRemoteMetadata, OnOverLoadRemoteMetadata);
        MessageHelper.AddListener<EnterGameModel, object>(MessageName.StartBuildMap, OnStartBuildMap);
    }
    private void OnStartBuildMap(EnterGameModel enterGameModel, object obj)
    {
        StartStep(new LoadStep() {name = "构建场景",starValue = 0.8f,endValue = 1f});
    }


    private void OnStartLoadRemoteMetadata()
    {
        StartStep(new LoadStep() {name = "下载资源",starValue = 0.4f,endValue = 0.6f});
    }

    private void OnOverLoadRemoteMetadata()
    {
        StartStep(new LoadStep() {name = "构建场景",starValue = 0.6f,endValue = 0.8f});
    }


    public void Init(UgcBaseInfo ugcBaseInfo, BaseCreator creatorInfo, LoadingType loadingType, Texture t = null, Sprite s = null) {
        if (t != null)
        {
            if (loadingType == LoadingType.Map)
            {
                propCover.gameObject.SetActive(false);
                cover.texture = t;
                mask.gameObject.SetActive(false);
            }
            else if (loadingType == LoadingType.Cloth||loadingType == LoadingType.Prop || loadingType == LoadingType.Material|| loadingType == LoadingType.MusicalInstrument || loadingType == LoadingType.UGCAnim || loadingType == LoadingType.Vehicle)
            {
                propCover.gameObject.SetActive(true);
                propCover.texture = t;
            }
        }
        if (s!=null)
        {
            propCover.gameObject.SetActive(false);
            propTempCover.gameObject.SetActive(true);
            propTempCover.sprite = s;
        }
        if (ugcBaseInfo != null)
        {
            mapName.SetText(ugcBaseInfo.name);
            nickName.SetText(creatorInfo?.nickname);
            if (loadingType == LoadingType.Map)
            {
                propCover.gameObject.SetActive(false);
                if (t == null)
                {
                    var tmpCover = ugcBaseInfo.cover;
                    if (string.IsNullOrEmpty(tmpCover)) {
                        tmpCover = MapAssetManager.Inst.GetTemplateCover(ugcBaseInfo.templateId);
                    }
                    coverLoader.Load(tmpCover, true,
                        (from, success) =>
                        {
                            mask.gameObject.SetActive(false);
                        });
                }
            }
            else if (loadingType == LoadingType.Cloth || loadingType == LoadingType.MusicalInstrument || loadingType == LoadingType.UGCAnim || loadingType == LoadingType.Vehicle)
            {
                if (t == null&&s==null&&!string.IsNullOrEmpty(ugcBaseInfo.cover))
                {
                    propCover.gameObject.SetActive(true);
                    propCoverLoader.Load(ugcBaseInfo.cover, true,
                        (from, success) =>
                        {
                        });
                }
            }
        }
        avatarSkinBg.SetActive(loadingType == LoadingType.Cloth);
        instrumentBg.SetActive(loadingType == LoadingType.MusicalInstrument);
        ugcAnimBg.SetActive(loadingType == LoadingType.UGCAnim);
        vehicleBg.SetActive(loadingType == LoadingType.Vehicle);
        StartStep(new LoadStep() {name = "加载中" , starValue = 0, endValue = 0.4f});

    }


    public void Init(UgcBaseInfo ugcBaseInfo, LoadingType loadingType, Texture t = null)
    {
        Init(ugcBaseInfo, null, loadingType, t);
    }

    protected override void OnDisable()
    {
        if (currentLoadStep != null)
        {
            loadingCount = currentLoadStep.count - 10;
        }
    }


    private void StopTimer()
    {
        if (loadingTimer != null && loadingTimer.IsDisposed == false)
        {
            TimerManager.Inst.Stop(loadingTimer);
            loadingTimer = null;
        }
    }

    public void StartStep(LoadStep loadStep)
    {
        StopTimer();
        currentLoadStep = loadStep;
        LoadingText.SetLocalText(loadStep.name);
        loadingCount = 0;
        SetProgress(currentLoadStep.starValue);
        loadingTimer = TimerManager.Inst.Run("LoadingTimer", 0f, currentLoadStep.speed, () =>
        {
            loadingCount++;
            UpdateLoadingValue();
            if (curProgress >= currentLoadStep.endValue)
            {
                StopTimer();
            }
        });
    }


    protected virtual void UpdateLoadingValue() ///DelayHide前刷新进度条
    {
        float progressValue = currentLoadStep.starValue + ((float)loadingCount / currentLoadStep.count);
        if (progressValue > 1)
        {
            progressValue = 1;
        }

        SetProgress(progressValue);
    }

    protected void SetProgress(float progressValue)
    {
        if (this == null || progressBar == null) return;
        progressBar.SetValueWithoutNotify(progressValue);
        curProgress = progressValue;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopTimer();
        MessageHelper.RemoveListener(MessageName.StartLoadRemoteMetadata, OnStartLoadRemoteMetadata);
        MessageHelper.RemoveListener(MessageName.OverLoadRemoteMetadata, OnOverLoadRemoteMetadata);
        MessageHelper.RemoveListener<EnterGameModel, object>(MessageName.StartBuildMap, OnStartBuildMap);
    }
}
