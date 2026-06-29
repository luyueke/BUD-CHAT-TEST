using Game.Database;
using UnityEngine;

public static class CabinSkinLockHelper
{
    public static void ApplyLock(GameObject lockObj, string ugcId, string creator)
    {
        if (lockObj == null) return;
        if (!string.IsNullOrEmpty(creator) && creator == AccountDataManager.Inst.Uid)
        {
            lockObj.SetActive(false);
            return;
        }
        var inv = !string.IsNullOrEmpty(ugcId) ? BagDatabase.Inst.Select(ugcId) : null;
        lockObj.SetActive(inv == null || inv.OwnedNum <= 0);
    }
}
