using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.EventSystems;

public class GroupConsumeApplyAdapter: OSA<BaseParamsWithPrefab,GroupConsumeApplyItemHolder>,IPointerClickHandler {


    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<GroupConsumeBaseInfo> Data { get; private set; }
    private IPool texturePool;
    private Action<PointerEventData> mClickedHandler = null;
    private GroupConsumeApplyType applyType;

    protected override void Start() {
        base.Start();
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<GroupConsumeBaseInfo>(this);
        base.Start();
    }

    public void SetType(GroupConsumeApplyType type) {
        applyType = type;
    }

    protected override GroupConsumeApplyItemHolder CreateViewsHolder(int itemIndex) {
        var instance = new GroupConsumeApplyItemHolder();
        instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
        instance.avatarRemoteImageBehaviour.InitializeWithPool(texturePool);
        return instance;
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void UpdateViewsHolder(GroupConsumeApplyItemHolder newOrRecycled) {
        GroupConsumeBaseInfo model = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(model, applyType);
    }




    public void AddClickListener(Action<PointerEventData> clickAction)
    {
        mClickedHandler = clickAction;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            mClickedHandler?.Invoke(eventData);
        }
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }
}

public class GroupConsumeApplyItemHolder : BaseItemViewsHolder
{
    public RemoteImageBehaviour avatarRemoteImageBehaviour;
    public GroupConsumeApplyItem applyItem;
    public override void CollectViews()
    {
        base.CollectViews();
        applyItem = root.GetComponent<GroupConsumeApplyItem>();
        avatarRemoteImageBehaviour = applyItem.avatarImage;
    }

    public void UpdateViews(GroupConsumeBaseInfo model, GroupConsumeApplyType applyType)
    {
        applyItem.SetData(model, applyType);
    }
}
