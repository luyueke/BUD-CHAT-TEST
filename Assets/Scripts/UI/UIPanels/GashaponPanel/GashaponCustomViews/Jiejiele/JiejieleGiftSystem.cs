using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiejieleGiftSystem : GlobalInstance<JiejieleGiftSystem>
{
    public List<string> productIds = new List<string>();

    public override void Initialize()
    {
        base.Initialize();
#if UNITY_ANDROID
        productIds.Add("android_bud2026yuanxiaonichengkuang30");
        productIds.Add("android_bud2026yuanxiaotouxiangkuang30");
#else
        productIds.Add("ios_bud2026yuanxiaonichengkuang30");
        productIds.Add("ios_bud2026yuanxiaotouxiangkuang30");
#endif
    }

}