using System;
using Game.Avatar;
using Game.Store;
using GameData.BaseInfo;
using UnityEngine;

public abstract class AINpcBaseLobbyIdleView :MonoBehaviour
{
     protected AvatarBagSceneHandler dataHandler;
     protected GoodsDataClassifyList assetsDatas = new();
     protected Action resetAnimAction;

     public abstract void OnShow(AINpcInfo info, CharacterWrap wrap);
     public abstract void UpdateAssetList(AINpcAnimType npcType);

     public abstract void ResetAssetList();
     public virtual void OnCreate(Action resetAnim)
     {
          resetAnimAction = resetAnim;
          dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
          dataHandler.AddDataChange(gameObject, OnDataChange);
     }

     protected abstract void OnDataChange(AssetsData[] changes);


     public abstract void PlayMainAnim();
}