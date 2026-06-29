using System.Collections;
using System.Collections.Generic;
using Game.Database;
using Game.Store;
using UnityEngine;

public class ConsumeEventRewardItem : MonoBehaviour
{
    public GameObject Go_Owned;
    public string PgcId;

    private void Start()
    {
        if(string.IsNullOrEmpty(PgcId))
            return;

        RefreshOwnedState();
    }

    public void RefreshOwnedState()
    {
        var isOwned =AssetsDataManager.IsOwned(PgcId);
        Go_Owned.SetActive(isOwned);
    }
}
